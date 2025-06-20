using UnityEngine;
using Ubiq.Messaging;

public class VolumeSender : MonoBehaviour
{
    private NetworkId networkId = new NetworkId(99); // 要与后端监听的ID一致
    private NetworkContext context;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        context = NetworkScene.Register(this, networkId);

        // Hardcode volume
        var message = new
        {
            type = "setVolume",
            volume = 80
        };

        context.SendJson(message);
        Debug.Log("Sent volume control message");

    }
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // 不处理任何消息
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
