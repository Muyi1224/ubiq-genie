using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class TwoHandScale : MonoBehaviour
{
    public float minScale = 0.1f;         // 允许的最小缩放（世界空间）
    public float maxScale = 3.0f;         // 允许的最大缩放
    public float smooth = 12f;            // 插值平滑

    XRGrabInteractable grab;

    IXRSelectInteractor first;            // 第一只手
    IXRSelectInteractor second;           // 第二只手
    Vector3 initialScale;                 // 进入双手模式时的原始缩放
    float initialDistance;                // 进入双手模式时两手距离
    bool scaling;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        // 确保支持双手同时选中
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
        // 如果任意一只手松开，则退出双手缩放
        if (args.interactorObject == second) second = null;
        if (args.interactorObject == first) first = second; // 把第二只手提升为第一只
        scaling = (first != null && second != null);
        if (scaling) BeginTwoHandScale();     // 仍是两手则重新记录基线
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

        float factor = dist / initialDistance;              // 距离比例 → 缩放因子
        Vector3 target = initialScale * factor;             // 等比缩放

        // 夹紧到 [minScale, maxScale]
        target.x = Mathf.Clamp(target.x, minScale, maxScale);
        target.y = Mathf.Clamp(target.y, minScale, maxScale);
        target.z = Mathf.Clamp(target.z, minScale, maxScale);

        transform.localScale = Vector3.Lerp(transform.localScale, target,
                                            Time.deltaTime * smooth);
    }

    // 取“手”的空间位姿（优先用抓取的 Attach 点）
    Transform GetHandPose(IXRSelectInteractor interactor)
    {
        var attach = grab.GetAttachTransform(interactor);
        if (attach != null) return attach;
        return (interactor as Component).transform;
    }
}
