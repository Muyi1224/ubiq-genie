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

    Renderer _current;
    GameObject _marker;
    MaterialPropertyBlock _mpb;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

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

    // —— 供子菜单按钮调用：把材质应用到“当前选中”对象
    public void ApplyMaterialToSelection(Material mat)
    {
        if (_current && mat)
        {
            _current.material = mat;

            // 你项目里的同步（可选）
            var sync = _current.GetComponent<SyncTransformOnChange>();
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

        if (_current) Unhighlight(_current);
        _current = r;

        if (_current)
        {
            Highlight(_current);
            EnsureMarker(_current);
        }
        else if (_marker)
        {
            _marker.SetActive(false);
        }
    }

    void Highlight(Renderer r)
    {
        if (!usePropertyBlock)
        {
            foreach (var m in r.materials)
                if (m.HasProperty(EmissionColorId)) { m.EnableKeyword("_EMISSION"); m.SetColor(EmissionColorId, highlightColor); }
            return;
        }

        _mpb.Clear();
        _mpb.SetColor(EmissionColorId, highlightColor);
        r.SetPropertyBlock(_mpb); // 作用于该 Renderer 的所有子网格
    }

    void Unhighlight(Renderer r)
    {
        if (!usePropertyBlock)
        {
            foreach (var m in r.materials)
                if (m.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, Color.black);
            return;
        }

        _mpb.Clear();
        _mpb.SetColor(EmissionColorId, Color.black);
        r.SetPropertyBlock(_mpb);
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
