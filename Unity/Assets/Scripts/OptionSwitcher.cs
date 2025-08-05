using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;

public class OptionSwitcher : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("发往 Node 的 type 字段；例如 density / brightness / chaos")]
    public string optionType = "density";           // Inspector 填写："density" | "brightness" | "chaos"
                                                    // ***大小写随你们 Node 端约定***

    [Header("Toggle References (auto / low / high)")]
    public Toggle btnAuto;
    public Toggle btnLow;
    public Toggle btnHigh;

    private NetworkContext ctx;                     // 由 SpawnMenu 注入
    public void SetContext(NetworkContext c) => ctx = c;

    /* ---------- 封装消息结构 ---------- */
    private struct OptionMsg
    {
        public string type;     // density / brightness / chaos
        public string level;    // auto   / low         / high
    }

    /* ---------- 生命周期 ---------- */
    private void Start()
    {
        btnAuto.onValueChanged.AddListener(on => { if (on) Send("auto"); });
        btnLow.onValueChanged.AddListener(on => { if (on) Send("low"); });
        btnHigh.onValueChanged.AddListener(on => { if (on) Send("high"); });

        // 默认选中 auto（如果 Prefab 自己编辑好 Is On 就可省略）
        btnAuto.isOn = true;          // 这行会触发一次 Send("auto")
    }

    /* ---------- 发送 ---------- */
    private void Send(string level)
    {
        if (ctx.Scene == null)
        {
            Debug.LogWarning($"[OptionUI-{optionType}] NetworkContext 未注入");
            return;
        }

        ctx.SendJson(new OptionMsg
        {
            type = optionType,
            level = level
        });

        Debug.Log($"[OptionUI-{optionType}] sent {level}");
    }
}
