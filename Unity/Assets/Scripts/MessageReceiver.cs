//using UnityEngine;
//using Ubiq.Messaging;
//using Ubiq.Networking;

//// ���λ��壺�������� 2 ���ݣ�����λ���㣩������� ~1 ���������ɰ������
//public class FloatRingBuffer
//{
//    private readonly float[] buf;
//    private int w, r;
//    private readonly int mask;

//    public FloatRingBuffer(int capacityPow2 = 1 << 16) // 65536 samples �� 1.36s @48k
//    {
//        // ȷ��Ϊ 2 ����
//        int cap = 1;
//        while (cap < capacityPow2) cap <<= 1;
//        buf = new float[cap];
//        mask = cap - 1;
//        w = r = 0;
//    }

//    public int Count => (w - r) & mask;
//    public int Capacity => buf.Length;

//    // д�����飨����������ݣ���������
//    public void WriteBlock(float[] src, int count)
//    {
//        for (int i = 0; i < count; i++)
//        {
//            buf[w] = src[i];
//            w = (w + 1) & mask;
//            if (w == r) r = (r + 1) & mask; // ���Ǿ�����
//        }
//    }

//    // ��ȡ��� count ������������ʵ�ʶ�ȡ��
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

//    // Ŀ��/�𲥻��壨��������
//    private const int TargetBufferSamples = SampleRate / 5;   // 200ms
//    private const int PreBufferSamples = SampleRate * 3 / 10; // 300ms

//    private AudioSource source;
//    private NetworkContext ctx;

//    private readonly object rbLock = new object();
//    private FloatRingBuffer rb = new FloatRingBuffer(1 << 17); // ~131072 samples �� 2.73s
//    private float[] decodeScratch = new float[0];
//    private float[] temp = new float[0];

//    private bool hasPlaybackStarted = false;
//    private float lastSample = 0f;
//    private int totalSamplesPlayed = 0;

//    void Start()
//    {
//        ctx = NetworkScene.Register(this, new NetworkId(95));
//        source = GetComponent<AudioSource>();

//        // ������ Project Settings/Audio ���� 48k�������������ʱ��ʾ
//        if (AudioSettings.outputSampleRate != SampleRate)
//        {
//            Debug.LogWarning($"Audio output sample rate = {AudioSettings.outputSampleRate}, expected {SampleRate}.");
//        }

//        var clip = AudioClip.Create(
//            "LiveMusic",
//            SampleRate * 2, // Unity �ڲ������С���뻷�λ�����룩
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
//        source.spatialBlend = 0f; // ���� 3D���ɸ�Ϊ 1
//    }

//    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
//    {
//        // Ubiq �ṩ���ǿɶ����Σ��˴�תΪ byte[]/Span ����
//        var data = msg.data;

//        // ���� JSON ������Ϣ����ĺ�˿���ż�����ı���
//        if (data.Length > 0 && data[0] == (byte)'{')
//            return;

//        int offset = (data.Length >= WavHeaderBytes &&
//                      data[0] == (byte)'R' && data[1] == (byte)'I') ? WavHeaderBytes : 0;

//        int pcmBytes = data.Length - offset;
//        if (pcmBytes <= 0) return;

//        // ������ż���ֽڣ�16-bit ���룩
//        if ((pcmBytes & 1) == 1) pcmBytes -= 1;
//        int samples = pcmBytes / BytesPerSample;

//        // ׼�����뻺��
//        if (decodeScratch.Length < samples)
//            decodeScratch = new float[samples];

//        // һ�����ֽڡ�float��С�� 16-bit��
//        int j = 0;
//        for (int i = 0; i < pcmBytes; i += 2)
//        {
//            short s = (short)((data[offset + i + 1] << 8) | data[offset + i]);
//            decodeScratch[j++] = Mathf.Clamp(s / 32768f, -1f, 1f);
//        }

//        // д�뻷�λ��壨��һ�μ�����
//        lock (rbLock)
//        {
//            rb.WriteBlock(decodeScratch, samples);

//            // �𲥣�����ﵽ��ֵ������������һ��ʼ��
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

//        // ��֮ǰ��ȡ������
//        int available;
//        lock (rbLock) { available = rb.Count; }

//        // ����΢�ز������ʣ�����ƫ�� ��1% ��΢����
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
//            // �����ز������� temp[0..got) ����/ѹ���� buffer[0..n)
//            float step = (got - 1) / (float)(n - 1);
//            float pos = 0f;
//            for (int i = 0; i < n; i++)
//            {
//                int i0 = (int)pos;
//                int i1 = (i0 + 1 < got) ? i0 + 1 : i0;
//                float frac = pos - i0;
//                float s = temp[i0] * (1f - frac) + temp[i1] * frac;

//                // �� 1k �������루�������գ�
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
//            // Ƿ�أ��� lastSample ����ֱ��������˲�ϣ��ɸ�Ϊ���̵�����
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
