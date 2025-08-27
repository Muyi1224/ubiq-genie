//using UnityEngine;
//using Ubiq.Messaging;
//using Ubiq.Networking;

//// 环形缓冲：容量需是 2 的幂（便于位运算）；这里给 ~1 秒容量（可按需调大）
//public class FloatRingBuffer
//{
//    private readonly float[] buf;
//    private int w, r;
//    private readonly int mask;

//    public FloatRingBuffer(int capacityPow2 = 1 << 16) // 65536 samples ≈ 1.36s @48k
//    {
//        // 确保为 2 的幂
//        int cap = 1;
//        while (cap < capacityPow2) cap <<= 1;
//        buf = new float[cap];
//        mask = cap - 1;
//        w = r = 0;
//    }

//    public int Count => (w - r) & mask;
//    public int Capacity => buf.Length;

//    // 写入整块（覆盖最旧数据，不阻塞）
//    public void WriteBlock(float[] src, int count)
//    {
//        for (int i = 0; i < count; i++)
//        {
//            buf[w] = src[i];
//            w = (w + 1) & mask;
//            if (w == r) r = (r + 1) & mask; // 覆盖旧数据
//        }
//    }

//    // 读取最多 count 个样本，返回实际读取数
//    public int ReadBlock(float[] dst, int count)
//    {
//        int n = 0;
//        while (n < count && r != w)
//        {
//            dst[n++] = buf[r];
//            r = (r + 1) & mask;
//        }
//        return n;
//    }
//}

//[RequireComponent(typeof(AudioSource))]
//public class MessageReceiver : MonoBehaviour
//{
//    private const int SampleRate = 48000;
//    private const int Channels = 1;
//    private const int BytesPerSample = 2;
//    private const int WavHeaderBytes = 44;

//    // 目标/起播缓冲（样本数）
//    private const int TargetBufferSamples = SampleRate / 5;   // 200ms
//    private const int PreBufferSamples = SampleRate * 3 / 10; // 300ms

//    private AudioSource source;
//    private NetworkContext ctx;

//    private readonly object rbLock = new object();
//    private FloatRingBuffer rb = new FloatRingBuffer(1 << 17); // ~131072 samples ≈ 2.73s
//    private float[] decodeScratch = new float[0];
//    private float[] temp = new float[0];

//    private bool hasPlaybackStarted = false;
//    private float lastSample = 0f;
//    private int totalSamplesPlayed = 0;

//    void Start()
//    {
//        ctx = NetworkScene.Register(this, new NetworkId(95));
//        source = GetComponent<AudioSource>();

//        // 建议在 Project Settings/Audio 设置 48k；这里仅做运行时提示
//        if (AudioSettings.outputSampleRate != SampleRate)
//        {
//            Debug.LogWarning($"Audio output sample rate = {AudioSettings.outputSampleRate}, expected {SampleRate}.");
//        }

//        var clip = AudioClip.Create(
//            "LiveMusic",
//            SampleRate * 2, // Unity 内部缓冲大小（与环形缓冲分离）
//            Channels,
//            SampleRate,
//            true,
//            OnAudioRead
//        );

//        source.clip = clip;
//        source.loop = true;

//        source.priority = 0;
//        source.bypassEffects = true;
//        source.bypassListenerEffects = true;
//        source.bypassReverbZones = true;
//        source.spatialBlend = 0f; // 如需 3D，可改为 1
//    }

//    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
//    {
//        // Ubiq 提供的是可读区段；此处转为 byte[]/Span 处理
//        var data = msg.data;

//        // 忽略 JSON 控制消息（你的后端可能偶尔发文本）
//        if (data.Length > 0 && data[0] == (byte)'{')
//            return;

//        int offset = (data.Length >= WavHeaderBytes &&
//                      data[0] == (byte)'R' && data[1] == (byte)'I') ? WavHeaderBytes : 0;

//        int pcmBytes = data.Length - offset;
//        if (pcmBytes <= 0) return;

//        // 必须是偶数字节（16-bit 对齐）
//        if ((pcmBytes & 1) == 1) pcmBytes -= 1;
//        int samples = pcmBytes / BytesPerSample;

//        // 准备解码缓存
//        if (decodeScratch.Length < samples)
//            decodeScratch = new float[samples];

//        // 一次性字节→float（小端 16-bit）
//        int j = 0;
//        for (int i = 0; i < pcmBytes; i += 2)
//        {
//            short s = (short)((data[offset + i + 1] << 8) | data[offset + i]);
//            decodeScratch[j++] = Mathf.Clamp(s / 32768f, -1f, 1f);
//        }

//        // 写入环形缓冲（仅一次加锁）
//        lock (rbLock)
//        {
//            rb.WriteBlock(decodeScratch, samples);

//            // 起播：缓冲达到阈值再启动，避免一开始抖
//            if (!hasPlaybackStarted && rb.Count >= PreBufferSamples)
//            {
//                source.Play();
//                hasPlaybackStarted = true;
//                // Debug.Log($"Start playback. RB={rb.Count}");
//            }
//        }
//    }

//    private void OnAudioRead(float[] buffer)
//    {
//        int n = buffer.Length;

//        // 读之前获取可用量
//        int available;
//        lock (rbLock) { available = rb.Count; }

//        // 计算微重采样比率（根据偏差 ±1% 内微调）
//        float ratio = 1.0f;
//        int diff = available - TargetBufferSamples;
//        ratio += Mathf.Clamp(diff / (float)SampleRate * 0.1f, -0.01f, 0.01f);

//        int want = Mathf.CeilToInt(n / ratio);
//        EnsureTempSize(want);

//        int got;
//        lock (rbLock)
//        {
//            got = rb.ReadBlock(temp, want);
//        }

//        if (got > 1)
//        {
//            // 线性重采样，把 temp[0..got) 拉伸/压缩到 buffer[0..n)
//            float step = (got - 1) / (float)(n - 1);
//            float pos = 0f;
//            for (int i = 0; i < n; i++)
//            {
//                int i0 = (int)pos;
//                int i1 = (i0 + 1 < got) ? i0 + 1 : i0;
//                float frac = pos - i0;
//                float s = temp[i0] * (1f - frac) + temp[i1] * frac;

//                // 起播 1k 样本淡入（避免咔哒）
//                if (totalSamplesPlayed < 1000)
//                {
//                    float fade = totalSamplesPlayed / 1000f;
//                    s *= fade;
//                }

//                buffer[i] = s;
//                lastSample = s;
//                totalSamplesPlayed++;
//                pos += step;
//            }
//        }
//        else
//        {
//            // 欠载：用 lastSample 保持直流，避免瞬断（可改为极短淡出）
//            for (int i = 0; i < n; i++) buffer[i] = lastSample;
//        }
//    }

//    private void EnsureTempSize(int size)
//    {
//        if (temp.Length < size) temp = new float[size];
//    }

//    void OnDestroy()
//    {
//        if (source != null && source.isPlaying) source.Stop();
//    }
//}
