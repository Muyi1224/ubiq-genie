using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(SyncTransformOnChange))]
[RequireComponent(typeof(XRBaseInteractable))]      // 保留：可被选中/抓取
public class DeleteOnButton : MonoBehaviour
{
    // --- 原有网络设置，保持不动 -----------------
    private NetworkId networkId = new NetworkId(100);
    private NetworkContext context;
    private SyncTransformOnChange syncInfo;
    // ------------------------------------------

    private bool isSelected;                        // 当前是否被 XR 选中

    // ★ 新增：代码里直接创建并绑定 Quest3 右手 B 的 InputAction
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
        // OpenXR 通用绑定：RightHand/secondaryButton = B
        deleteAction = new InputAction("Delete",
            binding: "<XRController>{RightHand}/secondaryButton");
        // Oculus 兼容绑定：button2 = B
        deleteAction.AddBinding("<OculusTouchController>{RightHand}/button2");
    }

    void OnEnable()
    {
        // 监听 B 键
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
        context = NetworkScene.Register(this, networkId);   // 不改
        syncInfo = GetComponent<SyncTransformOnChange>();

        // 订阅 XR 选中 / 取消选中事件（保持原状）
        var ix = GetComponent<XRBaseInteractable>();
        ix.selectEntered.AddListener(_ => isSelected = true);
        ix.selectExited.AddListener(_ => isSelected = false);
    }

    void Update()
    {
        // 保留原 PC 调试逻辑：只有被选中时按 F 才删
        if (isSelected &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            DeleteWithMessage();
        }
    }

    // ★ 新增：Quest3 右手 B 键触发（仅当当前被选中时）
    private void OnDeletePerformed(InputAction.CallbackContext _)
    {
        if (isSelected)
            DeleteWithMessage();
    }

    // 如果你在 XRI 的事件里绑定了 Activate，也沿用原先逻辑
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
        /* 仅发送，不接收；留空 */
    }
}
