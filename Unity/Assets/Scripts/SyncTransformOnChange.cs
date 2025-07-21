using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using static SpawnMenu;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class SyncTransformOnChange : MonoBehaviour
{
    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private Vector3 lastScale;

    private NetworkId networkId = new NetworkId(100);
    public NetworkContext context;

    public string objectName;
    public string description;
    public string objectId;
    private bool hasSent = false;

    private XRGrabInteractable grabInteractable;

    void Start()
    {

        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        // 初始化最后一次记录，避免第一次判断为“未变化”或出错
        CacheCurrent();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        CacheCurrent();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        SyncIfChanged();
    }

    /// <summary>
    /// 外部可调用：若当前 Transform 相对上次缓存有变化，则发送网络同步消息。
    /// </summary>
    public void SyncIfChanged()
    {
        if (transform.position != lastPosition ||
            transform.eulerAngles != lastRotation ||
            transform.localScale != lastScale)
        {
            var msg = new SpawnMessage
            {
                objectId = objectId,
                objectName = objectName,
                description = description,
                position = transform.position,
                rotation = transform.eulerAngles,
                scale = transform.localScale,
                type = hasSent ? "scale" : "add" // 加入类型字段
            };

            context.SendJson(msg);

            Debug.Log($"[SyncTransform] Sent ({msg.type}) for {objectName}, id:{msg.objectId} | Pos:{msg.position}, Rot:{msg.rotation}, Scale:{msg.scale}");

            hasSent = true; // 标记为已发送
            CacheCurrent(); // 更新基准
        }
    }

    private void CacheCurrent()
    {
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
        lastScale = transform.localScale;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<SpawnMessage>();
        transform.position = msg.position;
        transform.eulerAngles = msg.rotation;
        transform.localScale = msg.scale;
        CacheCurrent(); // 同步远端数据后更新缓存
    }
}
