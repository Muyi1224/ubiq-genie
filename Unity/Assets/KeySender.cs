// KeySender.cs
using UnityEngine;
using Ubiq.Messaging;

public class KeySender : MonoBehaviour
{
    private NetworkId networkId = new NetworkId(99); // 要与后端监听的ID一致
    private NetworkContext context;

    [System.Serializable]
    public struct KeyMessage
    {
        public string type;   // 固定 "key"
        public string value;  // 例如 "C maj / A min"
    }

    /// <summary>
    /// 可选：从外部注入同一个 NetworkContext（和你现有的 BPM / TrackMute 一致）
    /// </summary>
    void Start()
    {
        context = NetworkScene.Register(this, networkId);

    }

    void Awake()
    {
    }

    // —— 给 Radial SubButton 用：在 UnityEvent 里直接传字符串 —— 
    public void SendKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogWarning("[KeySender] value is empty.");
            return;
        }

        var msg = new KeyMessage
        {
            type = "key",
            value = value.Trim()
        };

        context.SendJson(msg);
        Debug.Log($"[KeySender] Sent -> {{ type:'{msg.type}', value:'{msg.value}' }}");
    }

    // 备选：不传参时，使用 Inspector 可配置的默认值
    public string defaultValue = "C maj / A min";
    public void SendKeyDefault()
    {
        SendKey(defaultValue);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // 不处理任何消息
    }
}
