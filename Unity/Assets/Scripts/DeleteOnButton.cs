using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(SyncTransformOnChange))]
[RequireComponent(typeof(XRBaseInteractable))]      // �� ������ȷ���п�ѡ�еĽ������
public class DeleteOnButton : MonoBehaviour
{
    // --- ԭ���������ã����ֲ��� -----------------
    private NetworkId networkId = new NetworkId(100);
    private NetworkContext context;
    private SyncTransformOnChange syncInfo;
    // ------------------------------------------

    private bool isSelected;                        // �� ��ǰ�Ƿ� XR ѡ��

    [System.Serializable]
    public struct DeleteMessage
    {
        public string type;
        public string objectId;
        public string description;
    }

    void Start()
    {
        context = NetworkScene.Register(this, networkId);   // ����
        syncInfo = GetComponent<SyncTransformOnChange>();

        // �� ���� XR ѡ�� / ȡ��ѡ���¼�
        var ix = GetComponent<XRBaseInteractable>();
        ix.selectEntered.AddListener(_ => isSelected = true);
        ix.selectExited.AddListener(_ => isSelected = false);
    }

    void Update()
    {
        // �� ֻ�С��Լ�����ѡ�С�ʱ�ż��� F ��
        if (isSelected &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            DeleteWithMessage();
        }
    }

    // XR ��ť������ɾ���������ֱ���ť��
    public void DeleteFromXR(ActivateEventArgs args)
    {
        DeleteWithMessage();
    }

    // ------------------------------------------
    // ����������ԭ�ű���ͬ����΢���� null �ж�
    // ------------------------------------------
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
        /* �����ͣ������գ����� */
    }
}
