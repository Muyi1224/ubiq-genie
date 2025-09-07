using UnityEngine;
using Ubiq.Messaging;

public class VolumeSender : MonoBehaviour
{
    private NetworkId networkId = new NetworkId(99); 
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
