using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class TwoHandScale : MonoBehaviour
{
    public float minScale = 0.1f;         // �������С���ţ�����ռ䣩
    public float maxScale = 3.0f;         // ������������
    public float smooth = 12f;            // ��ֵƽ��

    XRGrabInteractable grab;

    IXRSelectInteractor first;            // ��һֻ��
    IXRSelectInteractor second;           // �ڶ�ֻ��
    Vector3 initialScale;                 // ����˫��ģʽʱ��ԭʼ����
    float initialDistance;                // ����˫��ģʽʱ���־���
    bool scaling;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        // ȷ��֧��˫��ͬʱѡ��
        grab.selectMode = InteractableSelectMode.Multiple;
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (first == null)
        {
            first = args.interactorObject;
        }
        else if (second == null && args.interactorObject != first)
        {
            second = args.interactorObject;
            BeginTwoHandScale();
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // �������һֻ���ɿ������˳�˫������
        if (args.interactorObject == second) second = null;
        if (args.interactorObject == first) first = second; // �ѵڶ�ֻ������Ϊ��һֻ
        scaling = (first != null && second != null);
        if (scaling) BeginTwoHandScale();     // �������������¼�¼����
    }

    void BeginTwoHandScale()
    {
        initialScale = transform.localScale;
        initialDistance = Vector3.Distance(GetHandPose(first).position,
                                           GetHandPose(second).position);
        scaling = initialDistance > 1e-4f;
    }

    void Update()
    {
        if (!scaling || first == null || second == null) return;

        float dist = Vector3.Distance(GetHandPose(first).position,
                                      GetHandPose(second).position);
        if (dist <= 1e-4f) return;

        float factor = dist / initialDistance;              // ������� �� ��������
        Vector3 target = initialScale * factor;             // �ȱ�����

        // �н��� [minScale, maxScale]
        target.x = Mathf.Clamp(target.x, minScale, maxScale);
        target.y = Mathf.Clamp(target.y, minScale, maxScale);
        target.z = Mathf.Clamp(target.z, minScale, maxScale);

        transform.localScale = Vector3.Lerp(transform.localScale, target,
                                            Time.deltaTime * smooth);
    }

    // ȡ���֡��Ŀռ�λ�ˣ�������ץȡ�� Attach �㣩
    Transform GetHandPose(IXRSelectInteractor interactor)
    {
        var attach = grab.GetAttachTransform(interactor);
        if (attach != null) return attach;
        return (interactor as Component).transform;
    }
}
