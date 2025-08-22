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

    // �����ֶ�
    [Header("Emission tweak")]
    [Min(0f)] public float emissionIntensity = 3f; // ����ǿ�ȣ��� 3~8
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");    // URP/BiRP
    static readonly int EmissiveColorHDRPId = Shader.PropertyToID("_EmissiveColor"); // HDRP

    Renderer _current;
    GameObject _marker;
    MaterialPropertyBlock _mpb;

    // �ӵ��ֶ���
    [Header("Outline")]
    public Material outlineMaterial;   // �����������õġ���߲��ʡ�
    public bool outlineAffectChildren = true; // ѡ��ʱ����Rendererһ����ߣ���ѡ��

    // ����ԭʼ���ʣ����ڻ�ԭ
    readonly System.Collections.Generic.Dictionary<Renderer, Material[]> _originalMats
        = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

    // ���ģ���ɶ�� Renderer ��ɣ�ͳһ����
    System.Collections.Generic.List<Renderer> _activeGroup = new System.Collections.Generic.List<Renderer>();

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


    public void ApplyMaterialToSelection(Material mat)
    {
        if (!_current || !mat) return;

        var targets = outlineAffectChildren
            ? _current.GetComponentsInChildren<Renderer>()
            : new Renderer[] { _current };

        foreach (var r in targets)
        {
            if (!r) continue;

            // ȡ���棬���û������õ�ǰ�� sharedMaterials ȥ�� outline
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

            // �滻����
            for (int i = 0; i < baseMats.Length; i++)
                baseMats[i] = mat;

            // ���»��� �� ��Զ�桸������ߡ��İ汾
            _originalMats[r] = baseMats;

            // �����ǰ�Ǹ���״̬ �� Ҫ���̼ӻ� outline
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

        // �Ȱ���һ�����������Renderer��ԭ
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
        if (!outlineMaterial) { Debug.LogWarning("[VRSelectionManager] ���� Inspector ���� outlineMaterial"); return; }

        if (!_originalMats.ContainsKey(r))
            _originalMats[r] = r.sharedMaterials; // ��סԭ����

        // �����ظ����
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] == outlineMaterial) return;

        var newMats = new Material[mats.Length + 1];
        System.Array.Copy(mats, newMats, mats.Length);
        newMats[newMats.Length - 1] = outlineMaterial;
        r.sharedMaterials = newMats;
    }

    // ���� ȡ��ѡ��ʱ�ָ�ԭ���ʣ��滻��ԭ���� Unhighlight��
    void Unhighlight(Renderer r)
    {
        if (_originalMats.TryGetValue(r, out var orig))
        {
            r.sharedMaterials = orig;
            _originalMats.Remove(r);
        }
        else
        {
            // ���ף�����߲����޳�
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

    // ���÷���ؼ��֣���ͬ�� Inspector �� Emission����ֻ�� URP/BiRP ��Ҫ
    void EnsureEmissionKeyword(Renderer r)
    {
        var mats = r.sharedMaterials;
        foreach (var m in mats)
        {
            if (!m) continue;
            if (m.HasProperty(EmissionColorId))
                m.EnableKeyword("_EMISSION");
            // HDRP һ�㲻��Ҫ�ؼ��֣�����
        }
    }
    public void ToggleSelection(Renderer r)
    {
        if (!r)
        {
            ClearSelection();
            return;
        }

        // �����ǰ�Ѿ�ѡ�е��� r���ٴ�ץ��ȡ��ѡ��
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

    // ����ѡ�������=�л�ѡ�У��ٴε�ͬһ����ȡ����
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

    // ������ߣ�����Ļ���귢�䣩
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
