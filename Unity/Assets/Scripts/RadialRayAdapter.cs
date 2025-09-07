using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RadialRayAdapter : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler
{
    public RectTransform radialMenuRect;   
    public int buttonCount = 6;
    public float startAngle = 90f;        
    public UnityEvent<int> onSelectIndex;  

    public void OnPointerMove(PointerEventData e) { }

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
