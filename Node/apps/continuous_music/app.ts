import { NetworkId } from 'ubiq';
import { ApplicationController } from '../../components/application';
import { SpeechToTextService } from '../../services/speech_to_text/service';
// import { ArtInterpretationService } from '../../services/art_interpretation/service';
// import { ArtGenerationService } from '../../services/art_generation/service';
import { ContinuousMusicGenerationService } from '../../services/continuous_music/service';
import { MediaReceiver } from '../../components/media_receiver';
import { MessageReader } from '../../components/message_reader';
import path from 'path';
import { RTCAudioData } from '@roamhq/wrtc/types/nonstandard';
import { fileURLToPath } from 'url';
import { AudioRecorder } from '../../services/audio_recorder/service';
import { TextGenerationService } from '../../services/text_generation/service';
import fs from 'fs';
import { dirname } from 'path';


export class ContinuousMusicAgent extends ApplicationController {
    components: {
        mediaReceiver?: MediaReceiver;
        speech2text?: SpeechToTextService;
        audioRecorder?: AudioRecorder;
        // artGenerationService?: ArtGenerationService;
        musicReceiver?: MessageReader;
        deleteReceiver?: MessageReader;
        // artInterpretation?: ArtInterpretationService;
        musicGenerationService?: ContinuousMusicGenerationService;
        textGenerationService?: TextGenerationService;
        writer?: fs.WriteStream;
        
        
    } = {};
    
    lastVolumeFromScale: number = 50;
    lastTypeFromUnity: string='';

    byteArray?: any;
    targetPeer: string = '';
    currentSpeech:string = '';
    currentObjectId: string = '';
    constructor(configFile: string = 'config.json') {
        super(configFile);
    }

    promptMap: Map<string, Set<string>> = new Map(); // description -> objectId 集合

    start(): void {
        // STEP 1: Register services (and any other components) used by the application
        this.registerComponents();
        this.log(`Services registered: ${Object.keys(this.components).join(', ')}`);

        // STEP 2: Define the application pipeline
        this.definePipeline();
        this.log('Pipeline defined');

        // STEP 3: Join a room based on the configuration (optionally creates a server)
        this.joinRoom();
    }

    registerComponents() {
        // An MediaReceiver to receive audio data from peers
        this.components.mediaReceiver = new MediaReceiver(this.scene);

        // A SpeechToTextService to transcribe audio coming from peers
        this.components.speech2text = new SpeechToTextService(this.scene);

        this.components.musicReceiver = new MessageReader(this.scene, 99);
        this.components.deleteReceiver = new MessageReader(this.scene, 100);
        // this.components.artInterpretation = new ArtInterpretationService(this.scene);
        //this.components.artGenerationService = new ArtGenerationService(this.scene);
        this.components.musicGenerationService = new ContinuousMusicGenerationService(this.scene);

        // An AudioRecorder to record audio data from peers
        this.components.audioRecorder = new AudioRecorder(this.scene);

        // A TextGenerationService to generate text based on text
        this.components.textGenerationService = new TextGenerationService(this.scene);

        // File path based on peer UUID and timestamp
        const timestamp = new Date().toISOString().replace(/:/g, '-');

        this.currentSpeech = "";

    }

    definePipeline() {
        // Step 1: When we receive audio data from a peer we send it to the transcription service and recording service
        this.components.mediaReceiver?.on('audio', (uuid: string, data: RTCAudioData) => {
            // Convert the Int16Array to a Buffer
            const sampleBuffer = Buffer.from(data.samples.buffer);
            
            // Send the audio data to the transcription service and the audio recording service
            if (this.roomClient.peers.get(uuid) !== undefined) {
                this.components.speech2text?.sendToChildProcess(uuid, sampleBuffer);
                this.components.audioRecorder?.sendToChildProcess(uuid, sampleBuffer);
            }
        });
        
        // Step 2: When we receive a response from the transcription service, we send it to the text generation service
        this.components.speech2text?.on('data', (data: Buffer, identifier: string) => {
            // We obtain the peer object from the room client using the identifier
            const peer = this.roomClient.peers.get(identifier);
            const peerName = peer?.properties.get('ubiq.displayname');

            let response = data.toString();
            var threshold = 10; //for filtering useless responses
            if (response.length != 0 && response.length > threshold) {
                // Remove all newlines from the response
                response = response.replace(/(\r\n|\n|\r)/gm, '');
                // console.log("Step 2 -> retrieve from speech to text...not used here");
                // console.log(response);
                if (response.startsWith('>')) {
                    response = response.slice(1); // Slice off the leading '>' character
                    this.currentSpeech = response;

                    const ts = new Date().toISOString().replace(/:/g, '-');     // 与 Transcription 脚本同格式
                    const display = peerName || identifier;                      // 有昵称用昵称，没有就用 UUID
                    console.log(`[${ts}] ${display}: ${response}`);
                    // this.log(`[${ts}] ${display}: ${response}`, 'info', '');

                    this.components.textGenerationService?.sendToChildProcess('default', response + '\n');
                    /*if (response.trim()) {
                        const message = (peerName + ' -> Agent:: ' + response).trim();
                        this.log(message);

                        this.components.codeGenerationService?.sendToChildProcess('default', message + '\n');
                    }*/
                }
            }
        });

        

        // STEP 1 this service receive the image and send to LLM 
        this.components.musicReceiver?.on('data', (data: any) => {
            //console.log("---- Step 1 -> send to create music prompt [...][...][...]");
            const selectionData = JSON.parse(data.message.toString());
            console.log(`Received from Unity → Type: ${selectionData.type}, ID: ${selectionData.objectId}, Description: ${selectionData.description}, Scale: ${JSON.stringify(selectionData.scale)}`);

            //const peerUUID = selectionData.peer;
            //this.byteArray = selectionData.image;
            //this.components.artInterpretation?.sendToChildProcess('default', data.message.toString() + '\n'); //@@from here how to deal with image to the service FIRST
            // this.components.musicGenerationService?.sendToChildProcess('default', data.message.toString() + '\n');
            this.currentObjectId = selectionData.objectId ?? '';
            this.lastTypeFromUnity = selectionData.type ?? "add";

            let volumeFromScale = 50;
            if (
                selectionData.scale &&
                typeof selectionData.scale.x === 'number' &&
                !isNaN(selectionData.scale.x)
            ) {
                const MAX_SCALE = 2.0; // 可根据实际需要修改上限
                const MIN_SCALE = 0.0;
                const clamped = Math.min(Math.max(selectionData.scale.x, MIN_SCALE), MAX_SCALE);
                volumeFromScale = Math.round((clamped / MAX_SCALE) * 100);
            }
            this.lastVolumeFromScale = volumeFromScale;

            if(selectionData.type === "add"){
                this.components.textGenerationService?.sendToChildProcess('default', selectionData.description + '\n');
            }

            if (selectionData.type === "scale") {
                const objId = selectionData.objectId?.trim();
                if (!objId) return;

                // 1. 在 promptMap 里找行
                let oldLine: string | undefined;
                for (const [line, ids] of this.promptMap.entries()) {
                    if (ids.has(objId)) { oldLine = line; break; }
                }
                if (!oldLine) {
                    console.warn(`No prompt line found for objectId ${objId}`);
                    return;
                }

                // 2. 提取关键字，拼新行
                const kwMatch = oldLine.match(/\[([^\]]+)\]/);
                const keywords = kwMatch ? kwMatch[1].trim() : '';
                const newLine = `[${keywords}] {${volumeFromScale}}`;

                // 3. 更新 gpt.txt
                const __dirname = dirname(fileURLToPath(import.meta.url));
                const gptPath   = path.resolve(__dirname, '../../apps/continuous_music/data/gpt.txt');
                const newContent = fs.readFileSync(gptPath, 'utf-8')
                                    .split(/\r?\n/)
                                    .map(l => (l.trim() === oldLine.trim() ? newLine : l))
                                    .filter(l => l.trim())      // 去掉空行
                                    .join('\n') + '\n';
                fs.writeFileSync(gptPath, newContent);
                console.log(`◎ Updated gpt.txt → ${newLine}`);

                // 4. 更新 promptMap
                const ids = this.promptMap.get(oldLine)!;
                ids.delete(objId);
                if (ids.size === 0) this.promptMap.delete(oldLine);

                if (!this.promptMap.has(newLine)) this.promptMap.set(newLine, new Set());
                this.promptMap.get(newLine)!.add(objId);

                this.sendPromptMapToUnity("scale", 99);

                // 5. 通知 Python 调整音量
                const msg = { type: "SetPromptVolume", prompt: keywords, volume: volumeFromScale };
                this.components.musicGenerationService?.sendToChildProcess(
                    'default',
                    JSON.stringify(msg) + '\n'
                );
                console.log(`◎ Sent SetPromptVolume for '${keywords}' → ${volumeFromScale}`);
                return; 
            }

            if (selectionData.type === "mute") {
                const objId = selectionData.objectId?.trim();
                if (!objId) return;

                let matchedPromptLine: string | undefined;
                let matchedKeywords: string | undefined;

                // 找到包含该 objectId 且关键词匹配的 promptLine
                for (const [promptLine, idSet] of this.promptMap.entries()) {
                    if (idSet.has(objId)) {
                        const match = promptLine.match(/\[([^\]]+)\]/);
                        if (match) {
                            matchedKeywords = match[1].trim();
                            matchedPromptLine = promptLine;
                            break;
                        }
                    }
                }

                if (!matchedPromptLine || !matchedKeywords) {
                    console.warn(`Mute: Cannot find prompt line or keywords for objectId ${objId}`);
                    return;
                }

                // 构造并发送 mute 消息
                const msg = {
                    type: "Mute",
                    objectId: objId,
                    mute: selectionData.mute,
                    prompt: matchedKeywords
                };

                this.components.musicGenerationService?.sendToChildProcess(
                    'default',
                    JSON.stringify(msg) + '\n'
                );

                console.log(`◎ Sent mute status to Python → objectId: ${msg.objectId}, mute: ${msg.mute}, prompt: ${msg.prompt}`);
                return;
            }

            const opt = selectionData.type?.toLowerCase();     // density / brightness / chaos
            const lv  = selectionData.level ?? selectionData.density;  // auto / low / high

            if (["density", "brightness", "chaos"].includes(opt) && lv) {
                const msg = {
                    type:  opt,   
                    level: lv    
                };

                this.components.musicGenerationService?.sendToChildProcess(
                    "default",
                    JSON.stringify(msg) + "\n"
                );
                return;
            }

            if (selectionData.type === "bpm") {
                const msg = { type: "bpm", value: selectionData.value };   // 60-180 或 "auto"
                this.components.musicGenerationService?.sendToChildProcess(
                    "default",
                    JSON.stringify(msg) + "\n"
                );
                return;
            }

            if (selectionData.type === "trackmute") {
                // selectionData.track 应为 "drums" | "bass" | "other"
                const msg = {
                    type: "TrackMute",
                    track: (selectionData.track || "").toLowerCase(),
                    mute : !!selectionData.mute        // true / false
                };
                this.components.musicGenerationService?.sendToChildProcess(
                    "default",
                    JSON.stringify(msg) + "\n"
                );
                return;
            }

        });

        // === Delete message from Unity =============================================
        this.components.deleteReceiver?.on('data', (data: any) => {
            const parsed = JSON.parse(data.message.toString());
            if (parsed.type !== 'delete') return;          // 只处理 delete
            const objId = parsed.objectId?.trim();
            if (!objId) return;

            console.log("delete object:", objId);

            // ---------- 在 promptMap 中查找对应的 prompt ---------------------------
            let matchedPromptLine: string | undefined;
            for (const [promptLine, idSet] of this.promptMap.entries()) {
                if (idSet.has(objId)) {
                    matchedPromptLine = promptLine;        // 形如 "[soothing tranquil piano] {50}"
                    idSet.delete(objId);                   // 从集合移除该 ID
                    if (idSet.size === 0) this.promptMap.delete(promptLine);
                    break;
                }
            }
            if (!matchedPromptLine) {
                console.warn(`No prompt found for objectId ${objId}`);
                return;
            }

            if (matchedPromptLine) {
                this.sendPromptMapToUnity("delete", 99, [
                    { prompt: matchedPromptLine, objectIds: [objId] }
                ]);
            }


            // ---------- 提取关键字并发送 DeletePrompt ------------------------------
            const kwMatch = matchedPromptLine.match(/\[([^\]]+)\]/);
            const keywords = kwMatch ? kwMatch[1].trim() : '';
            if (keywords) {
                const deleteMsg = { type: "DeletePrompt", prompt: keywords };
                this.components.musicGenerationService?.sendToChildProcess(
                    'default',
                    JSON.stringify(deleteMsg) + '\n'
                );
                console.log(`◎ Sent DeletePrompt for '${keywords}'`);
            }

            // ---------- 从 gpt.txt 中删除这一行 ------------------------------------
            const __dirname = dirname(fileURLToPath(import.meta.url));
            const gptPath   = path.resolve(
                __dirname,
                '../../apps/continuous_music/data/gpt.txt'
            );

            const newContent = fs.readFileSync(gptPath, 'utf-8')
                                .split(/\r?\n/)
                                .filter(line => line.trim() && line.trim() !== matchedPromptLine.trim())
                                .join('\n') + '\n';
            fs.writeFileSync(gptPath, newContent);

            console.log(`◎ Removed line from gpt.txt → ${matchedPromptLine}`);
        });

        

        // // STEP 2 this service retrieve information about object and functionalities
        // this.components.artInterpretation?.on('data', (data: Buffer, identifier: string) => {
        //     const response = data.toString();
        //     console.log('Received text generation response from child process ' + identifier + ': ' + response);
        //     console.log("---- Step 3 -> send to musicfx dj");
        //     if (response.startsWith(">")) {
        //         const cleaned_response = response.slice(1);

        //         /*const jsonObject = {
        //             prompt: response + "." + this.currentSpeech,
        //             output_file: "result.png",
        //             //image: this.byteArray
        //         };
                
        //         const jsonString = JSON.stringify(jsonObject);
        //         this.components.artGenerationService?.sendToChildProcess('default', jsonString+ '\n');*/

        //         const jsonObjectMusic = {
        //             prompt:  response, // +  this.currentSpeech,
        //             //image: this.byteArray
        //         };

        //         const jsonStringMusic = JSON.stringify(jsonObjectMusic);
        //         this.components.musicGenerationService?.sendToChildProcess('default', jsonStringMusic + '\n');
        //     }
        //     this.currentSpeech = "";
        // });
    
        // Step 3: When we receive a response from the text generation service, we send it to the text to speech service
        /*this.components.artGenerationService?.on('data', (data: Buffer, identifier: string) => {
            const response = data.toString();
            //console.log('Received text generation response from child process ' + identifier + ': ' + response);
            console.log("Step 3");
            // Parse target peer from the response (Agent -> TargetPeer: Message)
            if (response.startsWith(">")) {
                //console.log(" -> Send:: " + response);
                const cleaned_response = response.slice(1);
                
                this.scene.send(99, {
                        type: "ArtGenerated",
                        peer: identifier,
                        data: cleaned_response,
                    });
            }
        });*/

        // === Listen to ChatGPT output and write to gpt.txt =======================
        this.components.textGenerationService?.on('data', (buf: Buffer) => {
            let line = buf.toString().trim();                    // Example:  >Calm emotional piano {60}.
            line = line.replace(/^>+/, '').replace(/\.+$/, '');  // Remove leading ">" and trailing "."

            // -------- Extract keywords and volume --------------------------
            const keywordsMatch = line.match(/[a-zA-Z ]+/);      // Match letters and spaces
            const volumeMatch   = line.match(/\b(\d{1,3})\b/);   // Match number between 0–999

            if (!keywordsMatch) {
                this.log('Failed to extract keywords: ' + line, 'warning');
                return;
            }

            const keywords = keywordsMatch[0].trim().toLowerCase(); // e.g., calm emotional piano
            // const volume   = volumeMatch ? volumeMatch[1] : '60';   // Use default 60 if missing
            const volume   = this.lastVolumeFromScale;
            const objectId = this.currentObjectId || 'unknown-id';
            // -------- Reformat and write to gpt.txt ------------------------
            const finalLine = `[${keywords}] {${volume}}`;

            // 将 objectId 记录到 promptMap
            if (!this.promptMap.has(finalLine)) {
                this.promptMap.set(finalLine, new Set<string>());
            }
            this.promptMap.get(finalLine)!.add(objectId);

            const __dirname = dirname(fileURLToPath(import.meta.url));
            const gptPath   = path.resolve(__dirname,
                            '../../apps/continuous_music/data/gpt.txt');

            fs.appendFileSync(gptPath, finalLine + '\n');
            console.log('◎ Wrote to gpt.txt → ' + finalLine);

            // 打印 promptMap 状态
            // this.printPromptMap();
            this.sendPromptMapToUnity("add", 99);
        });



        // Background
        this.components.musicGenerationService?.on('data', (data: Buffer, identifier: string) => {
            let response = data;
            //console.log('Received TTS response from child process ' + identifier);
            
            const debug = data.toString();
            if (debug.startsWith(">*Ubiq*<")){
                console.log('Response: ' + debug);
                return;
            }

            this.scene.send(new NetworkId(99), {
                type: 'AudioInfo',
                // targetPeer: this.targetPeer,
                targetPeer: "Music Service",
                audioLength: data.length,
            });
            
            while (response.length > 0) {
                // console.log('Response length: ' + response.length + ' bytes');
                this.scene.send(new NetworkId(95), response.slice(0, 16000));
                response = response.slice(16000);
            }
        });
    }

    sendPromptMapToUnity(
        updateType: string,
        channel: number = 99,
        customData?: { prompt: string; objectIds: string[] }[]
    ) {
        const promptArray = customData ?? Array.from(this.promptMap.entries()).map(
            ([prompt, ids]) => ({ prompt, objectIds: Array.from(ids) })
        );

        const payload = { type: "PromptMapUpdate", updateType, data: promptArray, ts: Date.now() };
        this.scene.send(new NetworkId(channel), payload);

        // Log message that send to Unity
        // console.log(
        //     `◎ PromptMap (${updateType}) → ch ${channel}\n` +
        //     JSON.stringify(payload, null, 2)
        // );
    }


    printPromptMap() {
    console.log("Current promptMap status:");
    for (const [prompt, idSet] of this.promptMap.entries()) {
        console.log(`- Prompt: "${prompt}"`);
        console.log(`  Object IDs: ${Array.from(idSet).join(', ')}`);
    }

    
}

}

if (fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
    const configPath = './config.json';
    const __dirname = path.dirname(fileURLToPath(import.meta.url));
    const absConfigPath = path.resolve(__dirname, configPath);
    const app = new ContinuousMusicAgent(absConfigPath);
    app.start();
}
