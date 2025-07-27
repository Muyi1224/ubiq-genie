using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(SyncTransformOnChange))]
[RequireComponent(typeof(XRBaseInteractable))]      // ★ 新增：确保有可选中的交互组件
public class DeleteOnButton : MonoBehaviour
{
    // --- 原有网络设置，保持不动 -----------------
    private NetworkId networkId = new NetworkId(100);
    private NetworkContext context;
    private SyncTransformOnChange syncInfo;
    // ------------------------------------------

    private bool isSelected;                        // ★ 当前是否被 XR 选中

    [System.Serializable]
    public struct DeleteMessage
    {
        public string type;
        public string objectId;
        public string description;
    }

    void Start()
    {
        context = NetworkScene.Register(this, networkId);   // 不改
        syncInfo = GetComponent<SyncTransformOnChange>();

        // ★ 订阅 XR 选中 / 取消选中事件
        var ix = GetComponent<XRBaseInteractable>();
        ix.selectEntered.AddListener(_ => isSelected = true);
        ix.selectExited.AddListener(_ => isSelected = false);
    }

    void Update()
    {
        // ★ 只有“自己正被选中”时才监听 F 键
        if (isSelected &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            DeleteWithMessage();
        }
    }

    // XR 按钮触发的删除（例如手柄按钮）
    public void DeleteFromXR(ActivateEventArgs args)
    {
        DeleteWithMessage();
    }

    // ------------------------------------------
    // 以下内容与原脚本相同，仅微调了 null 判定
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
        /* 仅发送，不接收；留空 */
    }
}
