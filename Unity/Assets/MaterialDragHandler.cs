using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 将预设材质拖到目标物体上。
/// 运行时会在根 Canvas 下创建一个“拖拽中的 ghost”，
/// 跟随鼠标 / 控制器指针移动，并在结束时销毁。
/// </summary>
public class MaterialDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("assigned material")]
    public Material mat;

    // 拖拽中的“ghost”
    private GameObject ghost;
    private RectTransform ghostRt;
    private Canvas rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 找离自己最近的根 Canvas（通常就是 World-Space Canvas）
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        // 克隆一个 icon 当作 ghost
        ghost = new GameObject("Ghost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghostRt = ghost.GetComponent<RectTransform>();
        ghostRt.sizeDelta = ((RectTransform)transform).sizeDelta;

        // 把自己的 sprite 复制过去
        var srcImg = GetComponent<Image>();
        var dstImg = ghost.GetComponent<Image>();
        dstImg.sprite = srcImg.sprite;
        dstImg.color = srcImg.color;

        // 让 ghost 显示在最顶层
        ghost.transform.SetParent(rootCanvas.transform, worldPositionStays: false);
        ghost.transform.SetAsLastSibling();
        ghost.GetComponent<CanvasGroup>().blocksRaycasts = false; // 不挡射线

        // 立即跟随一次，避免第一帧错位
        FollowPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        FollowPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost) Destroy(ghost);

        // ↓↓↓ 这里仍保留你原来把材质赋给目标物体的逻辑 ↓↓↓
        // （示例：射线检测指针指向的 GameObject，并更换其材质）
        if (Physics.Raycast(Camera.main.ScreenPointToRay(eventData.position),
                            out RaycastHit hit, 100f))
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend && mat)
            {
                rend.material = mat;
            }
        }
    }

    private void FollowPointer(PointerEventData eventData)
    {
        if (!ghostRt) return;

        // 将屏幕坐标转换为 rootCanvas 的局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rootCanvas.transform,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera,
            out Vector2 localPos);

        ghostRt.anchoredPosition = localPos;
        ghostRt.localScale = Vector3.one;  // 避免 1000 × scale 问题
    }
}

