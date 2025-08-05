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
    [Tooltip("ֹͣ�϶���ú�ŷ��� (��)")]
    public float debounceSeconds = 0.35f;

    private NetworkContext ctx;          // �� SpawnMenu ע��
    public void SetContext(NetworkContext c) => ctx = c;

    private struct BpmMessage { public string type; public int value; }

    // ---- �ڲ�״̬ ----
    private float lastChangeTime = 0f;   // ���һ�λ�����ʱ��
    private int pendingBpm;            // �跢�͵���δ���͵�ֵ
    private int lastSentBpm = -1;      // ��һ���ѷ��͵�ֵ
    private bool hasPending = false;

    private void Start()
    {
        if (!bpmSlider)
        {
            Debug.LogError("[BpmSender] Slider reference missing!");
            enabled = false;
            return;
        }

        // ��ʼ UI
        pendingBpm = Mathf.RoundToInt(bpmSlider.value);
        UpdateLabel(pendingBpm);

        bpmSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float v)
    {
        pendingBpm = Mathf.RoundToInt(v);
        lastChangeTime = Time.time;
        hasPending = true;

        UpdateLabel(pendingBpm);         // ��ʱ UI ���£������ͣ�
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
        ctx.SendJson(msg);               // Ĭ�� 99 ���ŵ�
        Debug.Log($"[BpmSender] sent {bpm}");
    }
}
