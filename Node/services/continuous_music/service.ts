import { ServiceController } from '../../components/service';
import { NetworkScene } from 'ubiq';
import nconf from 'nconf';

class ContinuousMusicGenerationService extends ServiceController {
    constructor(scene: NetworkScene) {
        super(scene, 'ContinuousMusicGenerationService');
        this.registerRoomClientEvents();
    }

    // Register events to start the child process when the first peer joins the room, and to kill the child process when the last peer leaves the room.
    registerRoomClientEvents() {
        if (this.roomClient == undefined) {
            throw new Error('RoomClient must be added to the scene before ContinuousMusicGenerationService');
        }

        this.roomClient.addListener('OnPeerAdded', (peer: any) => {
            if (!('default' in this.childProcesses)) {
                // this.registerChildProcess('default', 'cmd.exe', [
                //     '/c', // Run a command in cmd
                //     'conda', 'activate', 'rgs', '&&', // Activate the 'rgs' environment
                //     'python', '-u',
                //     '../../services/continuous_music/text2music_continuous_wd.py',
                //     '--prompt_postfix',
                //     nconf.get('promptFromSystem') || '',
                // ]);
                this.registerChildProcess(
                    'default',
                    'conda',
                    [
                        'run', '-n', 'rgs',
                        'python', '-u',
                        '../../services/continuous_music/text2music_continuous_wd.py',
                        '--prompt_postfix', nconf.get('promptFromSystem') || ''
                    ]
                );
            }
        });
        
        this.roomClient.addListener('OnPeerRemoved', (peer: any) => {
            if (this.roomClient.peers.size == 0) {
                this.killChildProcess('default');
            }
        });
    }
}

export { ContinuousMusicGenerationService };
