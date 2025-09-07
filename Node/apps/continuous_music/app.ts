import { NetworkId } from 'ubiq';
import { ApplicationController } from '../../components/application';
import { SpeechToTextService } from '../../services/speech_to_text/service';
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
        musicReceiver?: MessageReader;
        deleteReceiver?: MessageReader;
        musicGenerationService?: ContinuousMusicGenerationService;
        textGenerationService?: TextGenerationService;
        writer?: fs.WriteStream;
        
        
    } = {};

    // Temporary state variables to link asynchronous events.
    lastVolumeFromScale: number = 50;
    lastTypeFromUnity: string='';
    currentSpeech:string = '';
    currentObjectId: string = '';

    byteArray?: any;
    targetPeer: string = '';
    
    constructor(configFile: string = 'config.json') {
        super(configFile);
    }

    // This is the core data structure. It maps a music prompt string (e.g., "[calm piano] {60}")
    // to a set of object IDs in the Unity scene that are associated with it.
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

        // Listens for messages from Unity on channel 99 (e.g., add, scale, mute).
        this.components.musicReceiver = new MessageReader(this.scene, 99);

        // Listens for delete messages from Unity on channel 100
        this.components.deleteReceiver = new MessageReader(this.scene, 100);

        // Manages the Python script that controls the music generation website.
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
            var threshold = 10; //  Filter out short or empty responses.
            if (response.length != 0 && response.length > threshold) {
                // Remove all newlines from the response
                response = response.replace(/(\r\n|\n|\r)/gm, '');
                // If the transcription is a command (starts with '>')...
                if (response.startsWith('>')) {
                    response = response.slice(1); // Slice off the leading '>' character
                    this.currentSpeech = response;

                    const ts = new Date().toISOString().replace(/:/g, '-');     // Timestamp format matching the transcription script.
                    const display = peerName || identifier;                      // Use nickname if available, otherwise UUID.
                    console.log(`[${ts}] ${display}: ${response}`);

                    // send it to the LLM to generate a music prompt.
                    this.components.textGenerationService?.sendToChildProcess('default', response + '\n');
                }
            }
        });

        

        // STEP 1 this service receive the image and send to LLM 
        this.components.musicReceiver?.on('data', (data: any) => {
            const selectionData = JSON.parse(data.message.toString());
            console.log(`Received from Unity → Type: ${selectionData.type}, ID: ${selectionData.objectId}, Description: ${selectionData.description}, Scale: ${JSON.stringify(selectionData.scale)}`);

            // Store the object ID and command type for later use in other asynchronous events.
            this.currentObjectId = selectionData.objectId ?? '';
            this.currentObjectId = selectionData.objectId ?? '';
            this.lastTypeFromUnity = selectionData.type ?? "add";

            // Convert the object's scale (size) from Unity into a volume level (0-100).
            let volumeFromScale = 50;
            if (
                selectionData.scale &&
                typeof selectionData.scale.x === 'number' &&
                !isNaN(selectionData.scale.x)
            ) {
                const MAX_SCALE = 2.0; // The maximum expected scale value from Unity.
                const MIN_SCALE = 0.0;
                const clamped = Math.min(Math.max(selectionData.scale.x, MIN_SCALE), MAX_SCALE);
                volumeFromScale = Math.round((clamped / MAX_SCALE) * 100);
            }
            this.lastVolumeFromScale = volumeFromScale;

            // If a new object is added, send its description to the LLM
            if(selectionData.type === "add"){
                this.components.textGenerationService?.sendToChildProcess('default', selectionData.description + '\n');
            }

            if (selectionData.type === "scale") {
                const objId = selectionData.objectId?.trim();
                if (!objId) return;

                // 1. Find the old prompt
                let oldLine: string | undefined;
                for (const [line, ids] of this.promptMap.entries()) {
                    if (ids.has(objId)) { oldLine = line; break; }
                }
                if (!oldLine) {
                    console.warn(`No prompt line found for objectId ${objId}`);
                    return;
                }

                // 2. Create the new prompt line with the updated volume.
                const kwMatch = oldLine.match(/\[([^\]]+)\]/);
                const keywords = kwMatch ? kwMatch[1].trim() : '';
                const newLine = `[${keywords}] {${volumeFromScale}}`;

                // 3. Update the gpt.txt file
                const __dirname = dirname(fileURLToPath(import.meta.url));
                const gptPath   = path.resolve(__dirname, '../../apps/continuous_music/data/gpt.txt');
                const newContent = fs.readFileSync(gptPath, 'utf-8')
                                    .split(/\r?\n/)
                                    .map(l => (l.trim() === oldLine.trim() ? newLine : l))
                                    .filter(l => l.trim())      // Remove empty lines.
                                    .join('\n') + '\n';
                fs.writeFileSync(gptPath, newContent);
                console.log(`◎ Updated gpt.txt → ${newLine}`);

                // 4. Update internal promptMap
                const ids = this.promptMap.get(oldLine)!;
                ids.delete(objId);
                if (ids.size === 0) this.promptMap.delete(oldLine);

                if (!this.promptMap.has(newLine)) this.promptMap.set(newLine, new Set());
                this.promptMap.get(newLine)!.add(objId);

                this.sendPromptMapToUnity("scale", 99);

                // 5. Sent to Python to adjust the volume
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

                // Find the prompt line and keywords associated with this object.
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

                // Construct and send the mute command
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

                console.log(`Sent mute status to Python → objectId: ${msg.objectId}, mute: ${msg.mute}, prompt: ${msg.prompt}`);
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
                const msg = { type: "bpm", value: selectionData.value };   // 60-180 or "auto"
                this.components.musicGenerationService?.sendToChildProcess(
                    "default",
                    JSON.stringify(msg) + "\n"
                );
                return;
            }

            if (selectionData.type === "trackmute") {
                // selectionData.track should be "drums" | "bass" | "other"
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

            if (selectionData.type === "key") {
                const msg = { type: "key", value: selectionData.value };  // e.g., "C maj / A min"
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

            //  Find the associated prompt in promptMap 
            let matchedPromptLine: string | undefined;
            for (const [promptLine, idSet] of this.promptMap.entries()) {
                if (idSet.has(objId)) {
                    matchedPromptLine = promptLine;        //  "[soothing tranquil piano] {50}"
                    idSet.delete(objId);                   // Remove the ID from the map
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


            // Extract the keywords and tell the Python script to remove the prompt.
            const kwMatch = matchedPromptLine.match(/\[([^\]]+)\]/);
            const keywords = kwMatch ? kwMatch[1].trim() : '';
            if (keywords) {
                const deleteMsg = { type: "DeletePrompt", prompt: keywords };
                this.components.musicGenerationService?.sendToChildProcess(
                    'default',
                    JSON.stringify(deleteMsg) + '\n'
                );
                console.log(`Sent DeletePrompt for '${keywords}'`);
            }

            // Remove the line from gpt.txt to keep the state file clean.
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

        
        // Listen to ChatGPT output and write to gpt.txt 
        this.components.textGenerationService?.on('data', (buf: Buffer) => {
            let line = buf.toString().trim();                    // Example:  >Calm emotional piano {60}.
            line = line.replace(/^>+/, '').replace(/\.+$/, '');  // Remove leading ">" and trailing "."

            // Extract keywords and volume
            const keywordsMatch = line.match(/[a-zA-Z0-9\/\- ]+/);      // Match letters and spaces
            const volumeMatch   = line.match(/\b(\d{1,3})\b/);   // Match number between 0–999

            if (!keywordsMatch) {
                this.log('Failed to extract keywords: ' + line, 'warning');
                return;
            }

            const keywords = keywordsMatch[0].trim().toLowerCase(); // e.g., calm emotional piano
            const words = keywords.split(/\s+/).filter(Boolean);
            const bannedSingles = new Set(['i','a','an','the']);
            if (keywords.length < 4 || words.length < 2 || (words.length === 1 && bannedSingles.has(words[0]))) {
                console.log(`Skip weak keywords: "${keywords}"`);
                return;
            }

            // const volume   = volumeMatch ? volumeMatch[1] : '60';   // Use default 60 if missing
            const volume   = this.lastVolumeFromScale;
            const objectId = this.currentObjectId || 'unknown-id';
            // Reformat and write to gpt.txt 
            const finalLine = `[${keywords}] {${volume}}`;

            // Add the new prompt and its associated objectID to promptMap
            if (!this.promptMap.has(finalLine)) {
                this.promptMap.set(finalLine, new Set<string>());
            }
            this.promptMap.get(finalLine)!.add(objectId);

            const __dirname = dirname(fileURLToPath(import.meta.url));
            const gptPath   = path.resolve(__dirname,
                            '../../apps/continuous_music/data/gpt.txt');

            fs.appendFileSync(gptPath, finalLine + '\n');
            console.log('Wrote to gpt.txt → ' + finalLine);

            // print promptMap
            // this.printPromptMap();
            this.sendPromptMapToUnity("add", 99);
        });


        // handle music stream send back from Python
        this.components.musicGenerationService?.on("data", (data: Buffer) => {
            const text = data.toString();

            // Ignore log messages
            if (text.startsWith(">*Ubiq*")) {
                console.log("Response:", text);
                return;
            }

            // Ignore empty chunk and data with only 2 bytes
            if (data.length === 2 && data[0] === 13 && data[1] === 10) {
                console.log("Skip lone CRLF chunk");
                return;
            }

            // find real PCM
            let rest = data;

            // Trim CRLF
            if (
                rest.length >= 2 &&
                rest[rest.length - 2] === 13 &&
                rest[rest.length - 1] === 10
            ) {
                rest = rest.subarray(0, rest.length - 2);
            }
            

            const MIN_CHUNK = 2048;
            const CHUNK = 16000;

            while (rest.length > 0) {
                const chunk = rest.subarray(0, CHUNK);
                rest = rest.subarray(CHUNK);

                if (chunk.length < MIN_CHUNK) {
                    console.log(`Skip small chunk: len=${chunk.length}`);
                    continue;
                }

                // console.log(`Sending chunk: len=${chunk.length}, head=[${chunk.subarray(0, 4).join(",")}]`);

                // Send the audio chunk on channel 95 for Unity to play.
                this.scene.send(new NetworkId(95), chunk);
            }
        });

    }
    /**
     * A helper function to send the current state of all prompts to Unity.
     * This is used to keep the frontend UI synchronized with the backend state.
     * @param updateType - The type of update ('add', 'scale', 'delete').
     * @param channel - The network channel to send the message on.
     * @param customData - Optional custom data for specific updates like deletion.
     */
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
        //     `PromptMap (${updateType}) → ch ${channel}\n` +
        //     JSON.stringify(payload, null, 2)
        // );
    }

    // debug function to log the current map
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
