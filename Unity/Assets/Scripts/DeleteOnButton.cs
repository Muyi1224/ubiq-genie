using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using System;
using Unity.VisualScripting;
using static SpawnMenu;

[RequireComponent(typeof(SyncTransformOnChange))]
public class DeleteOnButton : MonoBehaviour
{
    private NetworkId networkId = new NetworkId(100);
    private SyncTransformOnChange syncInfo;
    public NetworkContext context;


    public struct DeleteMessage
    {
        public string type;
        public string objectId;
        public string description;
    }


    void Start()
    {
        context = NetworkScene.Register(this, networkId);
        syncInfo = GetComponent<SyncTransformOnChange>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            DeleteWithMessage();
        }
    }

    public void DeleteFromXR(ActivateEventArgs args)
    {
        DeleteWithMessage();

        Destroy(gameObject);
    }


    private void DeleteWithMessage()
    {
        if (syncInfo == null)
        {
            Debug.LogWarning("Missing SyncTransformOnChange for object metadata.");
            return;
        }

        var deleteMessage = new DeleteMessage
        {
            type = "delete",
            objectId = syncInfo.objectId,
            description = syncInfo.description
        };


        context.SendJson(deleteMessage);

        Debug.Log($"[Delete] Sent delete for {syncInfo.objectId} ({syncInfo.description})");

        Destroy(gameObject);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // No-op here (we only send, not receive), but method must exist
    }
}
