using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script dynamically binds materials and icons to the buttons of an Ultimate Radial Submenu.
/// It waits for the submenu to generate its buttons and then populates them based on a predefined list.
/// </summary>
[RequireComponent(typeof(UltimateRadialSubmenu))]
public class RadialMenuMaterialBinder : MonoBehaviour
{
    [System.Serializable]
    public struct Slot { public Sprite icon; public Material mat; }
    public Slot[] slots;

    [Header("Mapping")]
    public int childOffset = 0;     // If there are extra nodes in front of the child object, can offset
    public bool autoBindOnEnable = true;
    public float readyTimeout = 2f; // wait for URM 
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
        // Wait until the GameObject is active in the scene hierarchy.
        float t = 0f;
        while (!gameObject.activeInHierarchy && (t += Time.unscaledDeltaTime) < readyTimeout)
            yield return null;

        // Wait until the Ultimate Radial Menu has generated its child button objects.
        while (transform.childCount == 0 && (t += Time.unscaledDeltaTime) < readyTimeout)
            yield return null;

        // If the submenu is still not ready after the timeout, log a warning and exit.
        if (!gameObject.activeInHierarchy || transform.childCount == 0)
        {
            Debug.LogWarning(
                $"[RadialMenuMaterialBinder] Submenu not ready. active={gameObject.activeInHierarchy}, children={transform.childCount}",
                this);
            yield break; 
        }

        // If enabled, log all child object names for debugging.
        if (logChildren)
        {
            for (int i = 0; i < transform.childCount; i++)
                Debug.Log($"[Submenu] child {i}: {transform.GetChild(i).name}", transform.GetChild(i));
        }

        // Determine how many buttons to bind, taking the smaller of the slots count and the child count.
        int count = Mathf.Min(slots?.Length ?? 0, Mathf.Max(0, transform.childCount - childOffset));
        for (int i = 0; i < count; i++)
        {
            var btn = transform.GetChild(i + childOffset);

            // Try to find an existing Image component on the button to reuse for the icon.
            Image img = null;
            var images = btn.GetComponentsInChildren<Image>(true);
            if (images != null && images.Length > 0)
                img = images[images.Length - 1]; // Assume the deepest Image in the hierarchy is the icon.

            if (img == null)
            {
                // If no suitable Image component is found, create a new one.
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(btn, false);
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(64, 64);
                img = go.GetComponent<Image>();
            }

            // Assign the icon from the corresponding slot.
            img.sprite = slots[i].icon;
            img.raycastTarget = true;

            // Find or add the MaterialDragHandler component to the button.
            var drag = btn.GetComponent<MaterialDragHandler>();
            if (!drag) drag = btn.gameObject.AddComponent<MaterialDragHandler>();
            // Assign the material from the corresponding slot to the drag handler.
            drag.mat = slots[i].mat;
        }

        // Mark the binding as complete to prevent it from running again.
        _bound = true;
    }
}
