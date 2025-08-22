using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Networking;
using System.Collections.Generic;
using System.Threading;

[RequireComponent(typeof(AudioSource))]
public class MessageReceiver : MonoBehaviour
{
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int BytesPerSample = 2;
    private const int WavHeaderBytes = 44;
    private const int PreBufferSamples = SampleRate / 4;  // 250ms����
    private const int MinBufferSamples = SampleRate / 10; // 100ms��С����

    private AudioSource source;
    private NetworkContext ctx;
    private readonly Queue<float> queue = new Queue<float>();
    private readonly object queueLock = new object();

    private bool hasPlaybackStarted = false;
    private int underrunCount = 0;
    private float lastSample = 0f;

    // ������Ϣ
    private float lastBufferCheckTime = 0f;
    private int totalSamplesReceived = 0;
    private int totalSamplesPlayed = 0;

    void Start()
    {
        ctx = NetworkScene.Register(this, new NetworkId(95));

        source = GetComponent<AudioSource>();

        // �����������Ƶ�������Լ����ӳ�
        var clip = AudioClip.Create("LiveMusic",
                                    SampleRate * 2,  // 2�뻺����
                                    Channels,
                                    SampleRate,
                                    true,
                                    OnAudioRead);
        source.clip = clip;
        source.loop = true;

        // �Ż���ƵԴ����
        source.priority = 0;  // ������ȼ�
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        var span = msg.data;

        if (span.Length > 0 && span[0] == (byte)'{')
            return;

        int offset = (span.Length >= WavHeaderBytes &&
                      span[0] == (byte)'R' && span[1] == (byte)'I') ? WavHeaderBytes : 0;

        lock (queueLock)
        {
            // ��Ӽ򵥵ĵ�ͨ�˲�������������
            float previousSample = lastSample;

            for (int i = offset; i + 1 < span.Length; i += BytesPerSample)
            {
                // �����ֽ������⣨С����
                short s = (short)((span[i + 1] << 8) | span[i]);
                float sample = s / 32768f;

                // �򵥵ĵ�ͨ�˲��������ٸ�Ƶ����
                float filteredSample = previousSample * 0.1f + sample * 0.9f;
                previousSample = filteredSample;

                // �޷�����ֹ����
                filteredSample = Mathf.Clamp(filteredSample, -1f, 1f);

                queue.Enqueue(filteredSample);
                totalSamplesReceived++;
            }

            lastSample = previousSample;
        }

        // ����Ӧ����������
        if (!hasPlaybackStarted && queue.Count > PreBufferSamples)
        {
            source.Play();
            hasPlaybackStarted = true;
            Debug.Log($"��ʼ���ţ���������С: {queue.Count}");
        }
    }

    private void OnAudioRead(float[] buffer)
    {
        lock (queueLock)
        {
            int samplesAvailable = queue.Count;

            // ������������ͣ���¼Ƿ��
            if (samplesAvailable < buffer.Length && hasPlaybackStarted)
            {
                underrunCount++;
                if (underrunCount % 100 == 0)  // ÿ100��Ƿ�����һ�ξ���
                {
                    Debug.LogWarning($"��Ƶ������Ƿ��! ��������: {samplesAvailable}, ��Ҫ: {buffer.Length}");
                }
            }

            int i = 0;
            float fadeInFactor = 1f;

            // �Ӷ����ж�ȡ����
            while (i < buffer.Length && queue.Count > 0)
            {
                float sample = queue.Dequeue();

                // �ڿ�ʼ����ʱ��ӵ���Ч��
                if (totalSamplesPlayed < 1000)
                {
                    fadeInFactor = totalSamplesPlayed / 1000f;
                    sample *= fadeInFactor;
                }

                buffer[i++] = sample;
                totalSamplesPlayed++;
            }

            // ���������Ϊ�գ�ʹ�ò�ֵ���ʣ�ಿ��
            if (i < buffer.Length)
            {
                float lastValidSample = (i > 0) ? buffer[i - 1] : lastSample;

                // ���Բ�ֵ��0������ͻȻ�ľ���
                for (; i < buffer.Length; i++)
                {
                    float t = (float)(i - (buffer.Length - queue.Count)) / queue.Count;
                    t = Mathf.Clamp01(t);
                    buffer[i] = Mathf.Lerp(lastValidSample, 0f, t);
                }
            }
        }
    }

    void Update()
    {
        // �������������Ϣ
        if (Time.time - lastBufferCheckTime > 10f)
        {
            lastBufferCheckTime = Time.time;
            lock (queueLock)
            {
                if (hasPlaybackStarted)
                {
                    //Debug.Log($"������״̬ - ���д�С: {queue.Count}, " +
                    //         $"��������: {totalSamplesReceived}, " +
                    //         $"��������: {totalSamplesPlayed}, " +
                    //         $"Ƿ�ش���: {underrunCount}");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }
}