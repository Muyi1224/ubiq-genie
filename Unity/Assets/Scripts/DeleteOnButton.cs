using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(SyncTransformOnChange))]
[RequireComponent(typeof(XRBaseInteractable))]      // �������ɱ�ѡ��/ץȡ
public class DeleteOnButton : MonoBehaviour
{
    // --- ԭ���������ã����ֲ��� -----------------
    private NetworkId networkId = new NetworkId(100);
    private NetworkContext context;
    private SyncTransformOnChange syncInfo;
    // ------------------------------------------

    private bool isSelected;                        // ��ǰ�Ƿ� XR ѡ��

    // �� ������������ֱ�Ӵ������� Quest3 ���� B �� InputAction
    private InputAction deleteAction;

    [System.Serializable]
    public struct DeleteMessage
    {
        public string type;
        public string objectId;
        public string description;
    }

    void Awake()
    {
        // OpenXR Generic Binding��RightHand/secondaryButton = B
        deleteAction = new InputAction("Delete",
            binding: "<XRController>{RightHand}/secondaryButton");
        // Oculus Compatible binding��button2 = B
        deleteAction.AddBinding("<OculusTouchController>{RightHand}/button2");
    }

    void OnEnable()
    {
        // listen on B
        deleteAction.Enable();
        deleteAction.performed += OnDeletePerformed;
    }

    void OnDisable()
    {
        deleteAction.performed -= OnDeletePerformed;
        deleteAction.Disable();
    }

    void Start()
    {
        context = NetworkScene.Register(this, networkId); 
        syncInfo = GetComponent<SyncTransformOnChange>();

        // Subscribe to XR check/uncheck events
        var ix = GetComponent<XRBaseInteractable>();
        ix.selectEntered.AddListener(_ => isSelected = true);
        ix.selectExited.AddListener(_ => isSelected = false);
    }

    void Update()
    {
        // Keep the original PC debugging logic: press F to delete only when it is selected
        if (isSelected &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            DeleteWithMessage();
        }
    }

    // Quest3 right controller B key trigger (only when currently selected)
    private void OnDeletePerformed(InputAction.CallbackContext _)
    {
        if (isSelected)
            DeleteWithMessage();
    }

    public void DeleteFromXR(ActivateEventArgs args)
    {
        DeleteWithMessage();
    }

    private void DeleteWithMessage()
    {
        if (!syncInfo)
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
    }
}
