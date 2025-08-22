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
    private const int PreBufferSamples = SampleRate / 4;  // 250ms缓冲
    private const int MinBufferSamples = SampleRate / 10; // 100ms最小缓冲

    private AudioSource source;
    private NetworkContext ctx;
    private readonly Queue<float> queue = new Queue<float>();
    private readonly object queueLock = new object();

    private bool hasPlaybackStarted = false;
    private int underrunCount = 0;
    private float lastSample = 0f;

    // 调试信息
    private float lastBufferCheckTime = 0f;
    private int totalSamplesReceived = 0;
    private int totalSamplesPlayed = 0;

    void Start()
    {
        ctx = NetworkScene.Register(this, new NetworkId(95));

        source = GetComponent<AudioSource>();

        // 创建更大的音频缓冲区以减少延迟
        var clip = AudioClip.Create("LiveMusic",
                                    SampleRate * 2,  // 2秒缓冲区
                                    Channels,
                                    SampleRate,
                                    true,
                                    OnAudioRead);
        source.clip = clip;
        source.loop = true;

        // 优化音频源设置
        source.priority = 0;  // 最高优先级
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
            // 添加简单的低通滤波器来减少噪音
            float previousSample = lastSample;

            for (int i = offset; i + 1 < span.Length; i += BytesPerSample)
            {
                // 修正字节序问题（小端序）
                short s = (short)((span[i + 1] << 8) | span[i]);
                float sample = s / 32768f;

                // 简单的低通滤波器，减少高频噪音
                float filteredSample = previousSample * 0.1f + sample * 0.9f;
                previousSample = filteredSample;

                // 限幅，防止爆音
                filteredSample = Mathf.Clamp(filteredSample, -1f, 1f);

                queue.Enqueue(filteredSample);
                totalSamplesReceived++;
            }

            lastSample = previousSample;
        }

        // 自适应缓冲区管理
        if (!hasPlaybackStarted && queue.Count > PreBufferSamples)
        {
            source.Play();
            hasPlaybackStarted = true;
            Debug.Log($"开始播放，缓冲区大小: {queue.Count}");
        }
    }

    private void OnAudioRead(float[] buffer)
    {
        lock (queueLock)
        {
            int samplesAvailable = queue.Count;

            // 如果缓冲区过低，记录欠载
            if (samplesAvailable < buffer.Length && hasPlaybackStarted)
            {
                underrunCount++;
                if (underrunCount % 100 == 0)  // 每100次欠载输出一次警告
                {
                    Debug.LogWarning($"音频缓冲区欠载! 可用样本: {samplesAvailable}, 需要: {buffer.Length}");
                }
            }

            int i = 0;
            float fadeInFactor = 1f;

            // 从队列中读取样本
            while (i < buffer.Length && queue.Count > 0)
            {
                float sample = queue.Dequeue();

                // 在开始播放时添加淡入效果
                if (totalSamplesPlayed < 1000)
                {
                    fadeInFactor = totalSamplesPlayed / 1000f;
                    sample *= fadeInFactor;
                }

                buffer[i++] = sample;
                totalSamplesPlayed++;
            }

            // 如果缓冲区为空，使用插值填充剩余部分
            if (i < buffer.Length)
            {
                float lastValidSample = (i > 0) ? buffer[i - 1] : lastSample;

                // 线性插值到0，避免突然的静音
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
        // 定期输出调试信息
        if (Time.time - lastBufferCheckTime > 10f)
        {
            lastBufferCheckTime = Time.time;
            lock (queueLock)
            {
                if (hasPlaybackStarted)
                {
                    //Debug.Log($"缓冲区状态 - 队列大小: {queue.Count}, " +
                    //         $"接收样本: {totalSamplesReceived}, " +
                    //         $"播放样本: {totalSamplesPlayed}, " +
                    //         $"欠载次数: {underrunCount}");
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