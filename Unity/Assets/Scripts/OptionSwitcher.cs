using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;

public class OptionSwitcher : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("The type field sent to the Node; for example density / brightness / chaos")]
    public string optionType = "density";           // Inspector £º"density" | "brightness" | "chaos"
                                                   

    [Header("Toggle References (auto / low / high)")]
    public Toggle btnAuto;
    public Toggle btnLow;
    public Toggle btnHigh;

    private NetworkContext ctx;                     // injected by SpawnMenu 
    public void SetContext(NetworkContext c) => ctx = c;

    /* ---------- Encapsulated message structure ---------- */
    private struct OptionMsg
    {
        public string type;     // density / brightness / chaos
        public string level;    // auto   / low         / high
    }

    /* ---------- life cycle ---------- */
    private void Start()
    {
        btnAuto.onValueChanged.AddListener(on => { if (on) Send("auto"); });
        btnLow.onValueChanged.AddListener(on => { if (on) Send("low"); });
        btnHigh.onValueChanged.AddListener(on => { if (on) Send("high"); });

        // default auto
        btnAuto.isOn = true;          // trigger Send("auto") once
    }

    /* ---------- sent ---------- */
    private void Send(string level)
    {
        if (ctx.Scene == null)
        {
            Debug.LogWarning($"[OptionUI-{optionType}] NetworkContext Î´×¢Èë");
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
