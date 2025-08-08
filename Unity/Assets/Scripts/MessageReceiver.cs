using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Networking;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MessageReceiver : MonoBehaviour
{
    private AudioSource source;
    private NetworkContext ctx;
    private readonly Queue<float> queue = new Queue<float>();

    const int SampleRate = 48000;
    const int Channels = 1;

    void Start()
    {
        ctx = NetworkScene.Register(this, new NetworkId(95));

        source = GetComponent<AudioSource>();
        var clip = AudioClip.Create("LiveMusic",
                                    SampleRate,
                                    Channels,
                                    SampleRate,
                                    true,
                                    OnAudioRead);
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        var span = msg.data;

        // —— 过滤 JSON 控制包 ——
        if (span.Length > 0 && span[0] == (byte)'{')
        {
            string json = System.Text.Encoding.UTF8.GetString(span);
            Debug.Log($"[AudioInfo] {json}");
            return;
        }

        // —— 解析 PCM ——
        for (int i = 0; i + 1 < span.Length; i += 2)
        {
            short s = (short)(span[i] | (span[i + 1] << 8));
            queue.Enqueue(s / 32768f);
        }
    }

    private void OnAudioRead(float[] buffer)
    {
        int i = 0;
        while (i < buffer.Length && queue.Count > 0)
        {
            buffer[i++] = queue.Dequeue();
        }
        for (; i < buffer.Length; i++) buffer[i] = 0f;
    }
}
