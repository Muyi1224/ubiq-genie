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

        // ��ʼ�����һ�μ�¼�������һ���ж�Ϊ��δ�仯�������
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
    /// �ⲿ�ɵ��ã�����ǰ Transform ����ϴλ����б仯����������ͬ����Ϣ��
    /// </summary>
    public void SyncIfChanged()
    {
        if (transform.position != lastPosition ||
            transform.eulerAngles != lastRotation ||
            transform.localScale != lastScale)
        {
            // �� �Ȱ�����Ŵ�
            transform.localScale += Vector3.one * 0.1f;
            var newScale = transform.localScale;       // ����һ��

            // �� �ٰѸ��º�����ݷ���ȥ
            var msg = new SpawnMessage
            {
                objectId = objectId,
                objectName = objectName,
                description = description,
                position = transform.position,
                rotation = transform.eulerAngles,
                scale = newScale,
                type = hasSent ? "scale" : "add"
            };

            context.SendJson(msg);
            Debug.Log($"[SyncTransform] Sent ({msg.type}) for {objectName}, id:{msg.objectId} | Pos:{msg.position}, Rot:{msg.rotation}, Scale:{msg.scale}");

            hasSent = true;

            // �� ���»�׼��������һ�ζԱ�����Ϊ��δ�仯��
            CacheCurrent();
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
        CacheCurrent(); // ͬ��Զ�����ݺ���»���
    }
}
