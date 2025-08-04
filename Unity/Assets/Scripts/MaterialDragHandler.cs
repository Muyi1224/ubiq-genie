using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("assigned material")]
    public Material mat;

    private GameObject ghost;
    private RectTransform ghostRt;
    private Canvas rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        ghost = new GameObject("Ghost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghostRt = ghost.GetComponent<RectTransform>();
        ghostRt.sizeDelta = ((RectTransform)transform).sizeDelta;

        var srcImg = GetComponent<Image>();
        var dstImg = ghost.GetComponent<Image>();
        dstImg.sprite = srcImg.sprite;
        dstImg.color = srcImg.color;

        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.transform.SetAsLastSibling();
        ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
        FollowPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData) => FollowPointer(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost) Destroy(ghost);

        if (Physics.Raycast(Camera.main.ScreenPointToRay(eventData.position),
                            out RaycastHit hit, 100f))
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend && mat)
            {
                rend.material = mat;
                var sync = rend.GetComponent<SyncTransformOnChange>();
                if (sync) sync.UpdateDescription(null, mat);
            }
        }
    }

        private void FollowPointer(PointerEventData eventData)
    {
        if (!ghostRt) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rootCanvas.transform,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera,
            out Vector2 localPos);
        ghostRt.anchoredPosition = localPos;
    }
}
