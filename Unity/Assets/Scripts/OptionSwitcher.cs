using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;

public class OptionSwitcher : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("���� Node �� type �ֶΣ����� density / brightness / chaos")]
    public string optionType = "density";           // Inspector ��д��"density" | "brightness" | "chaos"
                                                    // ***��Сд������ Node ��Լ��***

    [Header("Toggle References (auto / low / high)")]
    public Toggle btnAuto;
    public Toggle btnLow;
    public Toggle btnHigh;

    private NetworkContext ctx;                     // �� SpawnMenu ע��
    public void SetContext(NetworkContext c) => ctx = c;

    /* ---------- ��װ��Ϣ�ṹ ---------- */
    private struct OptionMsg
    {
        public string type;     // density / brightness / chaos
        public string level;    // auto   / low         / high
    }

    /* ---------- �������� ---------- */
    private void Start()
    {
        btnAuto.onValueChanged.AddListener(on => { if (on) Send("auto"); });
        btnLow.onValueChanged.AddListener(on => { if (on) Send("low"); });
        btnHigh.onValueChanged.AddListener(on => { if (on) Send("high"); });

        // Ĭ��ѡ�� auto����� Prefab �Լ��༭�� Is On �Ϳ�ʡ�ԣ�
        btnAuto.isOn = true;          // ���лᴥ��һ�� Send("auto")
    }

    /* ---------- ���� ---------- */
    private void Send(string level)
    {
        if (ctx.Scene == null)
        {
            Debug.LogWarning($"[OptionUI-{optionType}] NetworkContext δע��");
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
