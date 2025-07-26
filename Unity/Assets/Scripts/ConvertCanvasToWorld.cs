using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ConvertCanvasToWorld : MonoBehaviour
{
    [ContextMenu("Convert To World-Space Canvas")]
    void Convert()
    {
        var c = GetComponent<Canvas>();
        if (!c) return;

        c.renderMode = RenderMode.WorldSpace;
        c.worldCamera = Camera.main;

        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(800, 600);
        rt.localScale = Vector3.one * 0.005f;   // Ëõ·Å
        rt.position = Camera.main.transform.position +
                        Camera.main.transform.forward * 2f +
                        Vector3.up * 1.5f;

        Debug.Log("Canvas converted to World Space.");
    }
}