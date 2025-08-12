using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RadialRayAdapter : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler
{
    [Header("URM 根（用于坐标转换）")]
    public RectTransform radialMenuRect;   // 拖 URM 的 RectTransform 进来
    [Header("扇区数量 & 起始角（度）")]
    public int buttonCount = 6;
    public float startAngle = 90f;         // 0号扇区的中心朝向（按你样式微调 0/90）

    [Header("点击回调：索引0..N-1")]
    public UnityEvent<int> onSelectIndex;  // 在 Inspector 里给每个索引配动作

    public void OnPointerMove(PointerEventData e) { /* 需要高亮可在这儿把 hover index 存起来 */ }

    public void OnPointerClick(PointerEventData e)
    {
        if (!radialMenuRect) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                radialMenuRect, e.position, e.pressEventCamera, out var local)) return;

        float ang = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        ang = (ang - startAngle + 360f) % 360f;
        int idx = Mathf.FloorToInt(ang / (360f / buttonCount));
        idx = Mathf.Clamp(idx, 0, buttonCount - 1);

        onSelectIndex?.Invoke(idx);
    }
}
