using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRSelectionManager : MonoBehaviour
{
    public static VRSelectionManager Instance { get; private set; }

    [Header("Ray source (pick ONE)")]
    public XRRayInteractor rightRay;   // 有就拖这个
    public Transform rightAim;         // 没有就拖右手控制器 Transform

    [Header("Ray settings (for rightAim fallback)")]
    public float rayLength = 25f;
    public LayerMask layers = ~0;

    [Header("Selection visuals")]
    public Color highlightColor = new Color(0f, 1f, 1f, 0.75f);
    public bool usePropertyBlock = true;        // 用 MPB 高亮，避免实例化材质
    public GameObject selectionMarkerPrefab;    // 可选：选中标记

    // 新增字段
    [Header("Emission tweak")]
    [Min(0f)] public float emissionIntensity = 3f; // 高亮强度，试 3~8
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");    // URP/BiRP
    static readonly int EmissiveColorHDRPId = Shader.PropertyToID("_EmissiveColor"); // HDRP

    Renderer _current;
    GameObject _marker;
    MaterialPropertyBlock _mpb;

    // 加到字段区
    [Header("Outline")]
    public Material outlineMaterial;   // 这里拖你做好的“描边材质”
    public bool outlineAffectChildren = true; // 选中时连子Renderer一起描边（可选）

    // 缓存原始材质，便于还原
    readonly System.Collections.Generic.Dictionary<Renderer, Material[]> _originalMats
        = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

    // 如果模型由多个 Renderer 组成，统一处理
    System.Collections.Generic.List<Renderer> _activeGroup = new System.Collections.Generic.List<Renderer>();

    public Renderer Current => _current;
    public bool HasSelection => _current != null;

    void Awake()
    {
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    // —— 供“选中键”调用：从射线下取目标，命中则选中，否则清空
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

            // 取缓存，如果没缓存就用当前的 sharedMaterials 去掉 outline
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

            // 更新缓存 → 永远存「不带描边」的版本
            _originalMats[r] = baseMats;

            // 如果当前是高亮状态 → 要立刻加回 outline
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
        // 优先用 XRRayInteractor 的 3D 命中
        if (rightRay && rightRay.TryGetCurrent3DRaycastHit(out hit))
            return true;

        // 否则用 rightAim / Camera 的物理射线
        Transform t = rightAim ? rightAim : (Camera.main ? Camera.main.transform : null);
        if (!t) { hit = default; return false; }

        return Physics.Raycast(t.position, t.forward, out hit, rayLength, layers, QueryTriggerInteraction.Ignore);
    }

    void SetCurrent(Renderer r)
    {
        if (_current == r) return;

        // 先把上一个对象的所有Renderer还原
        if (_activeGroup.Count > 0)
        {
            foreach (var rr in _activeGroup) Unhighlight(rr);
            _activeGroup.Clear();
        }

        _current = r;

        if (_current)
        {
            var targets = outlineAffectChildren
                ? _current.GetComponentsInChildren<Renderer>()
                : new Renderer[] { _current };

            foreach (var rr in targets)
            {
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
            _originalMats[r] = r.sharedMaterials; // 记住原材质

        // 避免重复添加
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] == outlineMaterial) return;

        var newMats = new Material[mats.Length + 1];
        System.Array.Copy(mats, newMats, mats.Length);
        newMats[newMats.Length - 1] = outlineMaterial;
        r.sharedMaterials = newMats;
    }

    // —— 取消选中时恢复原材质（替换你原来的 Unhighlight）
    void Unhighlight(Renderer r)
    {
        if (_originalMats.TryGetValue(r, out var orig))
        {
            r.sharedMaterials = orig;
            _originalMats.Remove(r);
        }
        else
        {
            // 兜底：把描边材质剔除
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
    }

    // 启用发光关键字（等同在 Inspector 勾 Emission），只对 URP/BiRP 需要
    void EnsureEmissionKeyword(Renderer r)
    {
        var mats = r.sharedMaterials;
        foreach (var m in mats)
        {
            if (!m) continue;
            if (m.HasProperty(EmissionColorId))
                m.EnableKeyword("_EMISSION");
            // HDRP 一般不需要关键字，跳过
        }
    }
    public void ToggleSelection(Renderer r)
    {
        if (!r)
        {
            ClearSelection();
            return;
        }

        // 如果当前已经选中的是 r，再次抓就取消选中
        if (_current == r)
            ClearSelection();
        else
            SetCurrent(r);
    }

    public void SelectUnderMouse()
    {
        if (TryGetMouseHit(out var hit))
            SetCurrent(hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null);
    }

    // （可选）鼠标点击=切换选中（再次点同一个就取消）
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

    // 鼠标射线（从屏幕坐标发射）
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
        if (!selectionMarkerPrefab) return;

        if (!_marker) _marker = Instantiate(selectionMarkerPrefab);
        _marker.SetActive(true);

        var b = r.bounds;
        _marker.transform.SetParent(r.transform, true);
        _marker.transform.position = b.center + Vector3.up * b.extents.y * 1.05f;
        _marker.transform.rotation = Quaternion.identity;
    }
}
