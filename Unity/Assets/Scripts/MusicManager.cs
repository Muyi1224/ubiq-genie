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

    // ≡≡ 用于重组音频 ========================================================
    private int remainingBytes;        // 还差多少字节没收到
    private MemoryStream pcmStream;    // 暂存音频

    [Serializable]
    private struct Message
    {
        public string data;
    }

    private struct AudioInfoMsg      // Node 首先发的 JSON
    {
        public string type;          // "AudioInfo"
        public int byteLength;       // 后续总字节
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
