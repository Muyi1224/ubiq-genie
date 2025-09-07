using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// A singleton manager for handling object selection in VR via raycasting.
/// It provides visual feedback (outline, marker) and allows material changes on the selected object.
/// </summary>
public class VRSelectionManager : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static VRSelectionManager Instance { get; private set; }

    [Header("Ray source (pick ONE)")]
    // The XR Ray Interactor from the XR Interaction Toolkit.
    public XRRayInteractor rightRay;
    // A fallback transform to use for raycasting if the XRRayInteractor is not assigned.
    public Transform rightAim;

    [Header("Ray settings (for rightAim fallback)")]
    public float rayLength = 25f;
    public LayerMask layers = ~0; // Raycast against all layers by default.

    [Header("Selection visuals")]
    // Note: These properties are declared but not used in the current code.
    public Color highlightColor = new Color(0f, 1f, 1f, 0.75f);
    public bool usePropertyBlock = true;
    // A prefab to instantiate as a visual marker above the selected object.
    public GameObject selectionMarkerPrefab;

    [Header("Emission tweak")]
    // Note: These properties are declared but not used in the current code.
    [Min(0f)] public float emissionIntensity = 3f;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int EmissiveColorHDRPId = Shader.PropertyToID("_EmissiveColor");

    // internal state
    Renderer _current; // The primary renderer of the currently selected object.
    GameObject _marker; // The instantiated selection marker object.
    MaterialPropertyBlock _mpb; // For performance, if changing material properties without creating new instances.

    [Header("Outline")]
    // The material used to draw the selection outline.
    public Material outlineMaterial;
    // If true, the outline will be applied to the selected object and all its children.
    public bool outlineAffectChildren = true;

    readonly System.Collections.Generic.Dictionary<Renderer, Material[]> _originalMats
        = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

    System.Collections.Generic.List<Renderer> _activeGroup
        = new System.Collections.Generic.List<Renderer>();

    public Renderer Current => _current;
    public bool HasSelection => _current != null;

    void Awake()
    {
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    // Called by an input action (e.g., a button press) to select the object under the ray.
    public void SelectUnderRay()
    {
        if (TryGetHit(out var hit))
            SetCurrent(hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null);
        else
            ClearSelection();
    }

    // Clears the current selection and removes any highlighting.
    public void ClearSelection() => SetCurrent(null);

    // Applies a new material to the currently selected object and its children.
    public void ApplyMaterialToSelection(Material mat)
    {
        if (!_current || !mat) return;

        // Determine the target renderers (either just the selected one or its children too).
        var targets = outlineAffectChildren
            ? _current.GetComponentsInChildren<Renderer>()
            : new Renderer[] { _current };

        foreach (var r in targets)
        {
            if (!r) continue;

            // Get the base materials (without the outline).
            Material[] baseMats;
            if (_originalMats.TryGetValue(r, out var cached))
            {
                baseMats = (Material[])cached.Clone(); // Use the cached original materials
            }
            else
            {
                // If not cached, get the current shared materials and strip the outline if present.
                var mats = r.sharedMaterials;
                bool hasOutline = outlineMaterial && mats.Length > 0 && mats[mats.Length - 1] == outlineMaterial;
                int len = hasOutline ? mats.Length - 1 : mats.Length;

                baseMats = new Material[len];
                for (int i = 0; i < len; i++) baseMats[i] = mats[i];
            }

            // replace texture
            for (int i = 0; i < baseMats.Length; i++)
                baseMats[i] = mat;

            // Cache the new set of materials (without the outline).
            _originalMats[r] = baseMats;

            // If the object is currently highlighted, re-apply the materials with the outline.
            if (_activeGroup.Contains(r))
            {
                var matsWithOutline = new Material[baseMats.Length + 1];
                System.Array.Copy(baseMats, matsWithOutline, baseMats.Length);
                matsWithOutline[matsWithOutline.Length - 1] = outlineMaterial;
                r.sharedMaterials = matsWithOutline;
            }
            else
            {
                r.sharedMaterials = baseMats;
            }

            var sync = r.GetComponent<SyncTransformOnChange>();
            if (sync) sync.UpdateDescription(null, mat);
        }
    }

    // Tries to get a raycast hit from the primary XR Ray or the fallback transform.
    bool TryGetHit(out RaycastHit hit)
    {
        if (rightRay && rightRay.TryGetCurrent3DRaycastHit(out hit))
            return true;

        Transform t = rightAim ? rightAim : (Camera.main ? Camera.main.transform : null);
        if (!t) { hit = default; return false; }

        return Physics.Raycast(t.position, t.forward, out hit, rayLength, layers, QueryTriggerInteraction.Ignore);
    }


    // Sets the currently selected renderer, handling the un-highlighting of the old and highlighting of the new.
    void SetCurrent(Renderer r)
    {
        if (_current == r) return;

        // First, unhighlight all renderers in the previously active group
        if (_activeGroup.Count > 0)
        {
            for (int i = _activeGroup.Count - 1; i >= 0; i--)
            {
                var rr = _activeGroup[i];
                if (!rr) { _activeGroup.RemoveAt(i); continue; } // Handle destroyed objects.
                Unhighlight(rr);
                _activeGroup.RemoveAt(i);
            }
        }

        _current = r;

        if (_current)
        {
            // If a new object is selected, highlight it (and its children if applicable).
            var targets = outlineAffectChildren
                ? _current.GetComponentsInChildren<Renderer>()
                : new Renderer[] { _current };

            foreach (var rr in targets)
            {
                if (!rr) continue;
                Highlight(rr);
                _activeGroup.Add(rr);
            }
            EnsureMarker(_current);  // Show the selection marker.
        }
        else if (_marker)
        {
            _marker.SetActive(false); // Hide the marker if nothing is selected
        }
    }

    void Highlight(Renderer r)
    {
        if (!outlineMaterial) { Debug.LogWarning("[VRSelectionManager] «Î‘⁄ Inspector Õœ»Î outlineMaterial"); return; }

        if (!_originalMats.ContainsKey(r))
            _originalMats[r] = r.sharedMaterials; // store original texture

        // Avoid adding the outline material if it's already there
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] == outlineMaterial) return;

        // Create a new material array with the outline material appended.
        var newMats = new Material[mats.Length + 1];
        System.Array.Copy(mats, newMats, mats.Length);
        newMats[newMats.Length - 1] = outlineMaterial;
        r.sharedMaterials = newMats;
    }

    void Unhighlight(Renderer r)
    {
        if (!r)
        {
            _originalMats.Remove(r);
            return;
        }

        if (_originalMats.TryGetValue(r, out var orig) && orig != null)
        {
            r.sharedMaterials = orig;
            _originalMats.Remove(r);
            return;
        }

        var mats = r.sharedMaterials;
        int count = 0;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] != outlineMaterial) count++;

        if (count != mats.Length)
        {
            var newMats = new Material[count];
            int k = 0;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != outlineMaterial) newMats[k++] = mats[i];
            r.sharedMaterials = newMats;
        }
    }

    public void ToggleSelection(Renderer r)
    {
        if (!r) { ClearSelection(); return; }

        if (_current == r)
            ClearSelection();
        else
            SetCurrent(r);
    }

    public bool IsSelected(Renderer r)
    {
        if (r == null) return false;
        if (r == _current) return true;
        return _activeGroup != null && _activeGroup.Contains(r);
    }


    public void ClearIfContains(Renderer r)
    {
        if (!r) return;
        if (r == _current || (_activeGroup != null && _activeGroup.Contains(r)))
            ClearSelection();
    }

    public void SelectUnderMouse()
    {
        if (TryGetMouseHit(out var hit))
            SetCurrent(hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null);
    }

    public void ToggleUnderMouse()
    {
        if (TryGetMouseHit(out var hit))
        {
            var r = hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null;
            if (!r) { ClearSelection(); return; }
            if (_current == r) ClearSelection();
            else SetCurrent(r);
        }
    }

    bool TryGetMouseHit(out RaycastHit hit)
    {
        hit = default;
        var cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, rayLength, layers, QueryTriggerInteraction.Ignore);
    }

    void EnsureMarker(Renderer r)
    {
        if (!selectionMarkerPrefab || !r) return;

        if (!_marker) _marker = Instantiate(selectionMarkerPrefab);
        _marker.SetActive(true);

        var b = r.bounds;
        _marker.transform.SetParent(r.transform, true);
        _marker.transform.position = b.center + Vector3.up * b.extents.y * 1.05f;
        _marker.transform.rotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        if (!_current && (_activeGroup.Count > 0 || _marker))
        {
            _activeGroup.Clear();
            _marker?.SetActive(false);
            var dead = new System.Collections.Generic.List<Renderer>();
            foreach (var kv in _originalMats)
                if (!kv.Key) dead.Add(kv.Key);
            foreach (var d in dead) _originalMats.Remove(d);
        }
    }
}
