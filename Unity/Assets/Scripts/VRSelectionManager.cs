using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRSelectionManager : MonoBehaviour
{
    public static VRSelectionManager Instance { get; private set; }

    [Header("Ray source (pick ONE)")]
    public XRRayInteractor rightRay;
    public Transform rightAim;

    [Header("Ray settings (for rightAim fallback)")]
    public float rayLength = 25f;
    public LayerMask layers = ~0;

    [Header("Selection visuals")]
    public Color highlightColor = new Color(0f, 1f, 1f, 0.75f);
    public bool usePropertyBlock = true;
    public GameObject selectionMarkerPrefab;

    [Header("Emission tweak")]
    [Min(0f)] public float emissionIntensity = 3f;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int EmissiveColorHDRPId = Shader.PropertyToID("_EmissiveColor");

    Renderer _current;
    GameObject _marker;
    MaterialPropertyBlock _mpb;

    [Header("Outline")]
    public Material outlineMaterial;
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

    // —— 供“选中键”调用
    public void SelectUnderRay()
    {
        if (TryGetHit(out var hit))
            SetCurrent(hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null);
        else
            ClearSelection();
    }

    public void ClearSelection() => SetCurrent(null);

    public void ApplyMaterialToSelection(Material mat)
    {
        if (!_current || !mat) return;

        var targets = outlineAffectChildren
            ? _current.GetComponentsInChildren<Renderer>()
            : new Renderer[] { _current };

        foreach (var r in targets)
        {
            if (!r) continue;

            // 取缓存（不带描边）
            Material[] baseMats;
            if (_originalMats.TryGetValue(r, out var cached))
            {
                baseMats = (Material[])cached.Clone();
            }
            else
            {
                var mats = r.sharedMaterials;
                bool hasOutline = outlineMaterial && mats.Length > 0 && mats[mats.Length - 1] == outlineMaterial;
                int len = hasOutline ? mats.Length - 1 : mats.Length;

                baseMats = new Material[len];
                for (int i = 0; i < len; i++) baseMats[i] = mats[i];
            }

            // 替换材质
            for (int i = 0; i < baseMats.Length; i++)
                baseMats[i] = mat;

            // 缓存「不带描边」版本
            _originalMats[r] = baseMats;

            // 如果当前在高亮 -> 加回描边
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

    // ---------- 内部 ----------

    bool TryGetHit(out RaycastHit hit)
    {
        if (rightRay && rightRay.TryGetCurrent3DRaycastHit(out hit))
            return true;

        Transform t = rightAim ? rightAim : (Camera.main ? Camera.main.transform : null);
        if (!t) { hit = default; return false; }

        return Physics.Raycast(t.position, t.forward, out hit, rayLength, layers, QueryTriggerInteraction.Ignore);
    }

    void SetCurrent(Renderer r)
    {
        if (_current == r) return;

        // 先还原上一个对象的所有 Renderer（倒序，跳过已销毁）
        if (_activeGroup.Count > 0)
        {
            for (int i = _activeGroup.Count - 1; i >= 0; i--)
            {
                var rr = _activeGroup[i];
                if (!rr) { _activeGroup.RemoveAt(i); continue; } // 已销毁
                Unhighlight(rr);
                _activeGroup.RemoveAt(i);
            }
        }

        _current = r;

        if (_current)
        {
            var targets = outlineAffectChildren
                ? _current.GetComponentsInChildren<Renderer>()
                : new Renderer[] { _current };

            foreach (var rr in targets)
            {
                if (!rr) continue;
                Highlight(rr);
                _activeGroup.Add(rr);
            }
            EnsureMarker(_current);
        }
        else if (_marker)
        {
            _marker.SetActive(false);
        }
    }

    void Highlight(Renderer r)
    {
        if (!outlineMaterial) { Debug.LogWarning("[VRSelectionManager] 请在 Inspector 拖入 outlineMaterial"); return; }

        if (!_originalMats.ContainsKey(r))
            _originalMats[r] = r.sharedMaterials; // 记录原材质

        // 避免重复添加
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] == outlineMaterial) return;

        var newMats = new Material[mats.Length + 1];
        System.Array.Copy(mats, newMats, mats.Length);
        newMats[newMats.Length - 1] = outlineMaterial;
        r.sharedMaterials = newMats;
    }

    // —— 取消选中时恢复原材质（容错版）
    void Unhighlight(Renderer r)
    {
        if (!r)
        {
            // 已销毁，尝试清掉缓存条目
            _originalMats.Remove(r);
            return;
        }

        if (_originalMats.TryGetValue(r, out var orig) && orig != null)
        {
            r.sharedMaterials = orig;
            _originalMats.Remove(r);
            return;
        }

        // 兜底：剔除描边材质
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

    /// <summary>
    /// 如果当前选中包含 r，则清除选中（供删除前调用）
    /// </summary>
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

    // —— 自检：若对象被外部销毁而未通知，这里做兜底清理
    void LateUpdate()
    {
        if (!_current && (_activeGroup.Count > 0 || _marker))
        {
            _activeGroup.Clear();
            _marker?.SetActive(false);

            // 清理无效缓存项
            var dead = new System.Collections.Generic.List<Renderer>();
            foreach (var kv in _originalMats)
                if (!kv.Key) dead.Add(kv.Key);
            foreach (var d in dead) _originalMats.Remove(d);
        }
    }
}
