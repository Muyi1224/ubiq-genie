using UnityEngine;
using UnityEngine.UI;

public class MatListBuilder : MonoBehaviour
{
    [System.Serializable] public struct Slot { public Sprite icon; public Material mat; }
    public Slot[] slots;
    public GameObject matIconPrefab;   // point to MatIcon
    public RectTransform listParent;   // point to MaterialList

    void Start()
    {
        foreach (var s in slots)
        {
            var go = Instantiate(matIconPrefab, listParent);
            var img = go.GetComponent<Image>();
            img.sprite = s.icon;

            var drag = go.GetComponent<MaterialDragHandler>();
            drag.mat = s.mat;
        }
    }
}
