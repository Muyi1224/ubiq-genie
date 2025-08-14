using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UltimateRadialSubmenu))]
public class RadialMenuMaterialBinder : MonoBehaviour
{
    [System.Serializable]
    public struct Slot { public Sprite icon; public Material mat; }
    public Slot[] slots;

    [Header("Mapping")]
    public int childOffset = 0;     // 如果子物体前面有额外节点，可以偏移
    public bool autoBindOnEnable = true;
    public float readyTimeout = 2f; // 最多等几秒让 URM 生成子按钮
    public bool logChildren = false;

    bool _bound;

    void OnEnable()
    {
        if (autoBindOnEnable && !_bound)
            StartCoroutine(BindWhenReady());
    }

    public void BindNow()
    {
        StopAllCoroutines();
        StartCoroutine(BindWhenReady());
    }

    IEnumerator BindWhenReady()
    {
        // 1) 等到对象真的在层级中激活
        float t = 0f;
        while (!gameObject.activeInHierarchy && (t += Time.unscaledDeltaTime) < readyTimeout)
            yield return null;

        // 2) 等到 URM 生成出子按钮（Sub Button 00/01/02……）
        while (transform.childCount == 0 && (t += Time.unscaledDeltaTime) < readyTimeout)
            yield return null;

        if (!gameObject.activeInHierarchy || transform.childCount == 0)
        {
            Debug.LogWarning(
                $"[RadialMenuMaterialBinder] Submenu not ready. active={gameObject.activeInHierarchy}, children={transform.childCount}",
                this);
            yield break; // 不做任何行为，避免触发 URM 错误
        }

        if (logChildren)
        {
            for (int i = 0; i < transform.childCount; i++)
                Debug.Log($"[Submenu] child {i}: {transform.GetChild(i).name}", transform.GetChild(i));
        }

        int count = Mathf.Min(slots?.Length ?? 0, Mathf.Max(0, transform.childCount - childOffset));
        for (int i = 0; i < count; i++)
        {
            var btn = transform.GetChild(i + childOffset);

            // 优先复用按钮里现有的 Image（一般是子物体上的图标）
            Image img = null;
            var images = btn.GetComponentsInChildren<Image>(true);
            if (images != null && images.Length > 0)
                img = images[images.Length - 1]; // 取层级最深的作为图标，避免改到底座

            if (img == null)
            {
                // 没有的话就创建一个“Icon”节点
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(btn, false);
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(64, 64);
                img = go.GetComponent<Image>();
            }

            img.sprite = slots[i].icon;
            img.raycastTarget = true;

            var drag = btn.GetComponent<MaterialDragHandler>();
            if (!drag) drag = btn.gameObject.AddComponent<MaterialDragHandler>();
            drag.mat = slots[i].mat;
        }

        _bound = true;
    }
}
