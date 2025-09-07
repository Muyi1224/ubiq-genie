using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ubiq.Messaging;

public class BpmSender : MonoBehaviour
{
    [Header("UI References")]
    public Slider bpmSlider;
    public TextMeshProUGUI bpmLabel;

    [Header("Send Settings")]
    [Tooltip("How long to wait after the user stops dragging before sending the value (in seconds).")]
    public float debounceSeconds = 0.35f;

    private NetworkContext ctx;          // injected by SpawnMenu
    public void SetContext(NetworkContext c) => ctx = c;

    private struct BpmMessage { public string type; public int value; }

    // ---- Internal State ----
    private float lastChangeTime = 0f;   // The time when the slider was last moved.
    private int pendingBpm;            // The BPM value that is waiting to be sent.
    private int lastSentBpm = -1;      // The last BPM value that was successfully sent.
    private bool hasPending = false;   // Flag indicating if there is a value waiting to be sent.

    private void Start()
    {
        if (!bpmSlider)
        {
            Debug.LogError("[BpmSender] Slider reference missing!");
            enabled = false;
            return;
        }

        // initial UI
        pendingBpm = Mathf.RoundToInt(bpmSlider.value);
        UpdateLabel(pendingBpm);

        bpmSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float v)
    {
        pendingBpm = Mathf.RoundToInt(v);
        lastChangeTime = Time.time;
        hasPending = true;

        UpdateLabel(pendingBpm);         // Update the UI label immediately for responsive feedback.
    }

    private void Update()
    {
        if (hasPending &&
            Time.time - lastChangeTime >= debounceSeconds &&
            pendingBpm != lastSentBpm)
        {
            SendBpm(pendingBpm);
            lastSentBpm = pendingBpm;
            hasPending = false;
        }
    }

    private void UpdateLabel(int bpm)
    {
        if (bpmLabel) bpmLabel.text = $"BPM: {bpm}";
    }

    private void SendBpm(int bpm)
    {
        if (ctx.Scene == null)
        {
            Debug.LogWarning("[BpmSender] context not set");
            return;
        }

        var msg = new BpmMessage { type = "bpm", value = bpm };
        ctx.SendJson(msg);               // 99 channel
        Debug.Log($"[BpmSender] sent {bpm}");
    }
}
