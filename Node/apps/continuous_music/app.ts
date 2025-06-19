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

export class ContinuousMusicAgent extends ApplicationController {
    components: {
        mediaReceiver?: MediaReceiver;
        speech2text?: SpeechToTextService;
        //artGenerationService?: ArtGenerationService;
        // artReceiver?: MessageReader;
        // artInterpretation?: ArtInterpretationService;
        musicGenerationService?: ContinuousMusicGenerationService;
    } = {};
    
    //byteArray?: any;
    targetPeer: string = '';
    currentSpeech:string = '';
    constructor(configFile: string = 'config.json') {
        super(configFile);
    }

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

        
        // this.components.artReceiver = new MessageReader(this.scene, 99);
        // this.components.artInterpretation = new ArtInterpretationService(this.scene);
        //this.components.artGenerationService = new ArtGenerationService(this.scene);
        this.components.musicGenerationService = new ContinuousMusicGenerationService(this.scene);

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
                console.log("Step 2 -> retrieve from speech to text...not used here");
                console.log(response);
                if (response.startsWith('>')) {
                    response = response.slice(1); // Slice off the leading '>' character
                    this.currentSpeech = response;
                    /*if (response.trim()) {
                        const message = (peerName + ' -> Agent:: ' + response).trim();
                        this.log(message);

                        this.components.codeGenerationService?.sendToChildProcess('default', message + '\n');
                    }*/
                }
            }
        });

        // // STEP 1 this service receive the image and send to LLM 
        // this.components.artReceiver?.on('data', (data: any) => {
        //     console.log("---- Step 1 -> send to create music prompt [...][...][...]");
        //     const selectionData = JSON.parse(data.message.toString());
        //     const peerUUID = selectionData.peer;
        //     //this.byteArray = selectionData.image;
        //     this.components.artInterpretation?.sendToChildProcess('default', data.message.toString() + '\n'); //@@from here how to deal with image to the service FIRST
        // });
        
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


        this.components.musicGenerationService?.on('data', (data: Buffer, identifier: string) => {
            let response = data;
            //console.log('Received TTS response from child process ' + identifier);
            
            const debug = data.toString();
            if (debug.startsWith(">*Ubiq*<")){
                console.log('Response: ' + debug);
                return;
            }

            this.scene.send(new NetworkId(95), {
                type: 'AudioInfo',
                targetPeer: "Music Service",
                audioLength: data.length,
            });
            
            while (response.length > 0) {
                //console.log('Response length: ' + response.length + ' bytes');
                this.scene.send(new NetworkId(95), response.slice(0, 16000));
                response = response.slice(16000);
            }
        });
    }
}

if (fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
    const configPath = './config.json';
    const __dirname = path.dirname(fileURLToPath(import.meta.url));
    const absConfigPath = path.resolve(__dirname, configPath);
    const app = new ContinuousMusicAgent(absConfigPath);
    app.start();
}
