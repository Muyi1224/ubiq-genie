using System;
using Ubiq.Messaging;
using UnityEngine;

public class MusicReceiver : MonoBehaviour
{
    private readonly NetworkId netId = new NetworkId(99);
    private NetworkContext ctx;

    private int expectedBytes = 0;
    private int receivedBytes = 0;

    private InjectableAudioSource injector;
    private bool hasStartedPlaying = false;

    void Start()
    {
        ctx = NetworkScene.Register(this, netId);
        injector = GetComponent<InjectableAudioSource>();
        if (!injector)
        {
            Debug.LogError("[MusicRx] InjectableAudioSource missing!");
        }
        else
        {
            Debug.Log("[MusicRx] Script started");
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        if (msg.bytes.Length > 0 && msg.bytes[0] == (byte)'{')
        {
            var json = System.Text.Encoding.UTF8.GetString(msg.bytes);
            var info = JsonUtility.FromJson<AudioInfo>(json);
            expectedBytes = info.audioLength;
            receivedBytes = 0;
            hasStartedPlaying = false;
            Debug.Log($"[MusicRx] expect {expectedBytes} bytes");
        }
        else
        {
            if (injector != null)
            {
                var chunk = new Span<byte>(msg.bytes, 0, msg.bytes.Length);
                injector.InjectPcm(chunk);
                receivedBytes += chunk.Length;

                Debug.Log($"[MusicRx] chunk {chunk.Length} B  total {receivedBytes}/{expectedBytes}");

                if (!hasStartedPlaying && receivedBytes > 0)
                {
                    Debug.Log($"[MusicRx] is playing music£¡");
                    hasStartedPlaying = true;
                }

                if (receivedBytes >= expectedBytes)
                {
                    Debug.Log($"[MusicRx] done injecting {receivedBytes} bytes");
                    expectedBytes = 0;
                }
            }
        }
    }

    [Serializable]
    struct AudioInfo
    {
        public string type;
        public string targetPeer;
        public int audioLength;
    }
}
