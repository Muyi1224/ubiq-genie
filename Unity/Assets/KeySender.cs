// KeySender.cs
using UnityEngine;
using Ubiq.Messaging;

public class KeySender : MonoBehaviour
{
    private NetworkId networkId = new NetworkId(99); // Ҫ���˼�����IDһ��
    private NetworkContext context;

    [System.Serializable]
    public struct KeyMessage
    {
        public string type;   // �̶� "key"
        public string value;  // ���� "C maj / A min"
    }

    /// <summary>
    /// ��ѡ�����ⲿע��ͬһ�� NetworkContext���������е� BPM / TrackMute һ�£�
    /// </summary>
    void Start()
    {
        context = NetworkScene.Register(this, networkId);

    }

    void Awake()
    {
    }

    // ���� �� Radial SubButton �ã��� UnityEvent ��ֱ�Ӵ��ַ��� ���� 
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

    // ��ѡ��������ʱ��ʹ�� Inspector �����õ�Ĭ��ֵ
    public string defaultValue = "C maj / A min";
    public void SendKeyDefault()
    {
        SendKey(defaultValue);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // �������κ���Ϣ
    }
}
