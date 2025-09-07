using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Ubiq.Networking;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Logging.Utf8Json;
using Ubiq.Rooms;
using System.Text;

public class MusicManager : MonoBehaviour
{
    private NetworkId networkId = new NetworkId(99);
    private NetworkContext context;

    
    private int remainingBytes;       
    private MemoryStream pcmStream;    

    [Serializable]
    private struct Message
    {
        public string data;
    }

    private struct AudioInfoMsg      
    {
        public string type;         
        public int byteLength;       
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        context = NetworkScene.Register(this, networkId);
        pcmStream = new MemoryStream();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
    {
        Message message = data.FromJson<Message>();
        Debug.Log("Message received: " + message.data);
    }
}
