using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRSelectionManager : MonoBehaviour
{
    public static VRSelectionManager Instance { get; private set; }

    [Header("Ray source (pick ONE)")]
    public XRRayInteractor rightRay;   // �о������
    public Transform rightAim;         // û�о������ֿ����� Transform

    [Header("Ray settings (for rightAim fallback)")]
    public float rayLength = 25f;
    public LayerMask layers = ~0;

    [Header("Selection visuals")]
    public Color highlightColor = new Color(0f, 1f, 1f, 0.75f);
    public bool usePropertyBlock = true;        // �� MPB ����������ʵ��������
    public GameObject selectionMarkerPrefab;    // ��ѡ��ѡ�б��

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

    // ���� ����ѡ�м������ã���������ȡĿ�꣬������ѡ�У��������
    public void SelectUnderRay()
    {
        if (TryGetHit(out var hit))
            SetCurrent(hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null);
        else
            ClearSelection();
    }

    public void ClearSelection() => SetCurrent(null);

    // ���� ���Ӳ˵���ť���ã��Ѳ���Ӧ�õ�����ǰѡ�С�����
    public void ApplyMaterialToSelection(Material mat)
    {
        if (_current && mat)
        {
            _current.material = mat;

            // ����Ŀ���ͬ������ѡ��
            var sync = _current.GetComponent<SyncTransformOnChange>();
            if (sync) sync.UpdateDescription(null, mat);
        }
    }

    // ---------- �ڲ� ----------

    bool TryGetHit(out RaycastHit hit)
    {
        // ������ XRRayInteractor �� 3D ����
        if (rightRay && rightRay.TryGetCurrent3DRaycastHit(out hit))
            return true;

        // ������ rightAim / Camera ����������
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
        r.SetPropertyBlock(_mpb); // �����ڸ� Renderer ������������
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
