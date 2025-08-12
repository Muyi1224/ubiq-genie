using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RadialRayAdapter : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler
{
    [Header("URM ������������ת����")]
    public RectTransform radialMenuRect;   // �� URM �� RectTransform ����
    [Header("�������� & ��ʼ�ǣ��ȣ�")]
    public int buttonCount = 6;
    public float startAngle = 90f;         // 0�����������ĳ��򣨰�����ʽ΢�� 0/90��

    [Header("����ص�������0..N-1")]
    public UnityEvent<int> onSelectIndex;  // �� Inspector ���ÿ�������䶯��

    public void OnPointerMove(PointerEventData e) { /* ��Ҫ������������� hover index ������ */ }

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
