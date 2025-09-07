using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Networking;
using System;

/// <summary>
/// A circular buffer for float data, optimized for audio samples.
/// Its capacity must be a power of two for efficient bitwise operations.
/// </summary>
public class FloatRingBuffer
{
    private readonly float[] buf; // The underlying array for the buffer.
    private int w, r; // Write and read pointers.
    private readonly int mask; // Bitmask for wrapping pointers (capacity - 1).

    // The capacity is rounded up to the next power of two.
    public FloatRingBuffer(int capacityPow2 = 1 << 16) // 65536 samples ¡Ö 1.36s @48k
    {
        // Ensure capacity is a power of two.
        int cap = 1;
        while (cap < capacityPow2) cap <<= 1;
        buf = new float[cap];
        mask = cap - 1;
        w = r = 0; // Initialize pointers.
    }

    public int Count => (w - r) & mask; //The number of samples currently in the buffer.
    public int Capacity => buf.Length; // The total capacity of the buffer.

    // Writes a block of data. Overwrites the oldest data if the buffer is full.
    public void WriteBlock(float[] src, int count)
    {
        for (int i = 0; i < count; i++)
        {
            buf[w] = src[i];
            w = (w + 1) & mask; // Increment and wrap the write pointer.
            if (w == r) r = (r + 1) & mask; // If we've wrapped around, push the read pointer forward.
        }
    }

    // Reads a block of data. Returns the number of samples actually read.
    public int ReadBlock(float[] dst, int count)
    {
        int n = 0;
        while (n < count && r != w) // Read until destination is full or buffer is empty.
        {
            dst[n++] = buf[r];
            r = (r + 1) & mask; // Increment and wrap the read pointer.
        }
        return n;
    }
}

[RequireComponent(typeof(AudioSource))]
public class MessageReceiver : MonoBehaviour
{
    // --- Audio Constants ---
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int BytesPerSample = 2;
    private const int WavHeaderBytes = 44;

    // --- Buffering Targets ---
    // The ideal number of samples to keep in the buffer for smooth playback.
    private const int TargetBufferSamples = SampleRate / 5;   // 200ms
    // The number of samples required before playback starts, to prevent initial stutter.
    private const int PreBufferSamples = SampleRate * 3 / 10; // 300ms

    private AudioSource source;
    private NetworkContext ctx;

    // --- Threading and Buffers ---
    private readonly object rbLock = new object(); // Lock for thread-safe access to the ring buffer.
    private FloatRingBuffer rb = new FloatRingBuffer(1 << 17); // ~131072 samples ¡Ö 2.73s
    private float[] decodeScratch = new float[0]; // Scratch buffer for converting bytes to floats.
    private float[] temp = new float[0]; // Temporary buffer for reading from the ring buffer.

    // --- Playback State ---
    private bool hasPlaybackStarted = false;
    private float lastSample = 0f; // The last sample value played, used for underrun.
    private int totalSamplesPlayed = 0; // Counter for applying a fade-in at the start.

    void Start()
    {
        ctx = NetworkScene.Register(this, new NetworkId(95));
        source = GetComponent<AudioSource>();

        // Warn if the project's audio settings don't match the expected sample rate.
        if (AudioSettings.outputSampleRate != SampleRate)
        {
            Debug.LogWarning($"Audio output sample rate = {AudioSettings.outputSampleRate}, expected {SampleRate}.");
        }

        // Create a procedural audio clip that will be fed by our OnAudioRead callback.
        var clip = AudioClip.Create(
            "LiveMusic", // Clip name
            SampleRate * 2, // Length of Unity's internal buffer
            Channels,
            SampleRate,
            true, // True for streaming
            OnAudioRead // The callback that provides audio data.
        );

        source.clip = clip;
        source.loop = true;

        // Configure AudioSource for clean, direct playback.
        source.priority = 0;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
        source.spatialBlend = 0f; // Set to 2D audio. Change to 1 for 3D.
    }
    // This method is called by Ubiq when a message arrives on channel 95.
    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        var data = msg.data;

        // Ignore JSON control messages (which might start with '{').
        if (data.Length > 0 && data[0] == (byte)'{')
            return;

        // Check for and skip a WAV header if present.
        int offset = (data.Length >= WavHeaderBytes &&
                      data[0] == (byte)'R' && data[1] == (byte)'I') ? WavHeaderBytes : 0;

        int pcmBytes = data.Length - offset;
        if (pcmBytes <= 0) return;

        // Ensure we have an even number of bytes for 16-bit samples.
        if ((pcmBytes & 1) == 1) pcmBytes -= 1;
        int samples = pcmBytes / BytesPerSample;

        // Resize the scratch buffer if needed.
        if (decodeScratch.Length < samples)
            decodeScratch = new float[samples];

        // Convert the 16-bit little-endian PCM byte data to float samples.
        int j = 0;
        for (int i = 0; i < pcmBytes; i += 2)
        {
            short s = (short)((data[offset + i + 1] << 8) | data[offset + i]);
            decodeScratch[j++] = Mathf.Clamp(s / 32768f, -1f, 1f);
        }

        // Lock the ring buffer for thread-safe writing.
        lock (rbLock)
        {
            rb.WriteBlock(decodeScratch, samples);

            // If playback hasn't started and we've buffered enough audio, start the AudioSource.
            if (!hasPlaybackStarted && rb.Count >= PreBufferSamples)
            {
                source.Play();
                hasPlaybackStarted = true;
                // Debug.Log($"Start playback. RB={rb.Count}");
            }
        }
    }

    // This callback is called by Unity's audio system when it needs more audio data.
    private void OnAudioRead(float[] buffer)
    {
        int n = buffer.Length;

        // Get the number of available samples from the ring buffer.
        int available;
        lock (rbLock) { available = rb.Count; }

        // --- Dynamic Resampling (Time-stretching) ---
        // Calculate a resampling ratio to keep our buffer close to the target size.
        float ratio = 1.0f;
        int diff = available - TargetBufferSamples;

        // Speed up playback if we have too much buffered data, slow down if we have too little.
        ratio += Mathf.Clamp(diff / (float)SampleRate * 0.1f, -0.01f, 0.01f);

        // Determine how many samples we need to read from our buffer to generate 'n' output samples.
        int want = Mathf.CeilToInt(n / ratio);
        EnsureTempSize(want);

        // Read the required samples from the ring buffer.
        int got;
        lock (rbLock)
        {
            got = rb.ReadBlock(temp, want);
        }

        if (got > 1)
        {
            // Perform linear resampling to stretch/compress the 'got' samples into the 'n' sized output buffer.
            float step = (got - 1) / (float)(n - 1);
            float pos = 0f;
            for (int i = 0; i < n; i++)
            {
                int i0 = (int)pos;
                int i1 = (i0 + 1 < got) ? i0 + 1 : i0;
                float frac = pos - i0;
                float s = temp[i0] * (1f - frac) + temp[i1] * frac;

                // Apply a short fade-in at the very beginning of playback to prevent clicks.
                if (totalSamplesPlayed < 1000)
                {
                    float fade = totalSamplesPlayed / 1000f;
                    s *= fade;
                }

                buffer[i] = s;
                lastSample = s;
                totalSamplesPlayed++;
                pos += step;
            }
        }
        else
        {
            // Underrun: If we don't have enough data, fill the buffer with the last sample value
            // to avoid silence or a loud pop.
            for (int i = 0; i < n; i++) buffer[i] = lastSample;
        }
    }

    // Helper to ensure the temporary buffer is large enough.
    private void EnsureTempSize(int size)
    {
        if (temp.Length < size) temp = new float[size];
    }

    void OnDestroy()
    {
        // Stop the audio source when the object is destroyed.
        if (source != null && source.isPlaying) source.Stop();
    }
}
