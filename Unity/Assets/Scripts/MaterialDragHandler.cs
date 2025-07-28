using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ��Ԥ������ϵ�Ŀ�������ϡ�
/// ����ʱ���ڸ� Canvas �´���һ������ק�е� ghost����
/// ������� / ������ָ���ƶ������ڽ���ʱ���١�
/// </summary>
public class MaterialDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("assigned material")]
    public Material mat;

    // ��ק�еġ�ghost��
    private GameObject ghost;
    private RectTransform ghostRt;
    private Canvas rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // �����Լ�����ĸ� Canvas��ͨ������ World-Space Canvas��
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        // ��¡һ�� icon ���� ghost
        ghost = new GameObject("Ghost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghostRt = ghost.GetComponent<RectTransform>();
        ghostRt.sizeDelta = ((RectTransform)transform).sizeDelta;

        // ���Լ��� sprite ���ƹ�ȥ
        var srcImg = GetComponent<Image>();
        var dstImg = ghost.GetComponent<Image>();
        dstImg.sprite = srcImg.sprite;
        dstImg.color = srcImg.color;

        // �� ghost ��ʾ�����
        ghost.transform.SetParent(rootCanvas.transform, worldPositionStays: false);
        ghost.transform.SetAsLastSibling();
        ghost.GetComponent<CanvasGroup>().blocksRaycasts = false; // ��������

        // ��������һ�Σ������һ֡��λ
        FollowPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        FollowPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost) Destroy(ghost);

        // ������ �����Ա�����ԭ���Ѳ��ʸ���Ŀ��������߼� ������
        // ��ʾ�������߼��ָ��ָ��� GameObject������������ʣ�
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

        // ����Ļ����ת��Ϊ rootCanvas �ľֲ�����
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rootCanvas.transform,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera,
            out Vector2 localPos);

        ghostRt.anchoredPosition = localPos;
        ghostRt.localScale = Vector3.one;  // ���� 1000 �� scale ����
    }
}

