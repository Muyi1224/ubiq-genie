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
    [Tooltip("停止拖动多久后才发送 (秒)")]
    public float debounceSeconds = 0.35f;

    private NetworkContext ctx;          // 由 SpawnMenu 注入
    public void SetContext(NetworkContext c) => ctx = c;

    private struct BpmMessage { public string type; public int value; }

    // ---- 内部状态 ----
    private float lastChangeTime = 0f;   // 最近一次滑动的时间
    private int pendingBpm;            // 需发送但尚未发送的值
    private int lastSentBpm = -1;      // 上一次已发送的值
    private bool hasPending = false;

    private void Start()
    {
        if (!bpmSlider)
        {
            Debug.LogError("[BpmSender] Slider reference missing!");
            enabled = false;
            return;
        }

        // 初始 UI
        pendingBpm = Mathf.RoundToInt(bpmSlider.value);
        UpdateLabel(pendingBpm);

        bpmSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float v)
    {
        pendingBpm = Mathf.RoundToInt(v);
        lastChangeTime = Time.time;
        hasPending = true;

        UpdateLabel(pendingBpm);         // 即时 UI 更新（不发送）
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
        ctx.SendJson(msg);               // 默认 99 号信道
        Debug.Log($"[BpmSender] sent {bpm}");
    }
}
