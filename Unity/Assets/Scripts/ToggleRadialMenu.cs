using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("Ҫ��ʾ/���صĲ˵��� (X ��)")]
    public GameObject radialMenuRoot;

    [Header("����ѡ���������� InputAction (X ��)")]
    public InputActionReference toggleAction; // ������Ĭ�� LeftHand/X

    // ========== �������ڶ�����壬Y �� ==========
    [Header("Ҫ��ʾ/���صĵڶ����˵��� (Y ��)")]
    public GameObject radialMenuRootY;

    [Header("����ѡ���������� InputAction (Y ��)")]
    public InputActionReference toggleActionY; // ������Ĭ�� LeftHand/Y
    // =========================================

    [Header("������ڷ�")]
    public Transform leftHand;                       // XR ���ֿ�����/ê��
    //public Vector3 handLocalOffset = new Vector3(-0.05f, 0.07f, 0.12f); // ������ֵı��ط���ƫ��(���ϡ�ǰ)
    public Vector3 handLocalOffset = new Vector3(-0.03f, 0.08f, 0.20f);
    public bool followLeftHand = true;               // �Ƿ����
    public bool faceHmd = true;                      // �Ƿ�ʼ�ճ���HMD
    public float followLerp = 12f;                   // ƽ��ϵ��

    [Header("Y ���ר��ƫ�Ƶ������� handLocalOffset �������ټӣ�")]
    public Vector3 extraOffsetY = new Vector3(0.05f, 0f, 0f);


    [Header("�༭�����Լ����������� X ��壩")]
    public Key debugKey = Key.M;

    private InputAction _runtimeAction;
    private InputAction _activeAction;

    // ===== ������Y ����Ӧ������ =====
    private InputAction _runtimeActionY;
    private InputAction _activeActionY;
    // ==============================

    void Awake()
    {
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot δ��ֵ");
        else radialMenuRoot.SetActive(false);

        // �������ڶ�������ʼ����
        if (radialMenuRootY) radialMenuRootY.SetActive(false);
    }

    void Start()
    {
        if (radialMenuRoot)
        {
            var cam = Camera.main;
            if (cam)
            {
                radialMenuRoot.transform.position = cam.transform.position + cam.transform.forward * 0.6f;
                radialMenuRoot.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            }
        }

        // ���������ڶ������һ����ʼ����λ��
        if (radialMenuRootY)
        {
            var cam = Camera.main;
            if (cam)
            {
                radialMenuRootY.transform.position = cam.transform.position + cam.transform.forward * 0.6f;
                radialMenuRootY.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            }
        }
    }

    void OnEnable()
    {
        // X ����primaryButton��
        if (toggleAction != null && toggleAction.action != null)
            _activeAction = toggleAction.action;
        else
        {
            _runtimeAction = new InputAction("ToggleRadialMenu", InputActionType.Button);
            _runtimeAction.AddBinding("<XRController>{LeftHand}/primaryButton").WithInteraction("press(pressPoint=0.5)");
            _runtimeAction.Enable();
            _activeAction = _runtimeAction;
        }
        _activeAction.Enable();
        _activeAction.performed += OnToggle;

        // ������Y ����secondaryButton��
        if (radialMenuRootY != null) // ֻ�������˵ڶ�����������
        {
            if (toggleActionY != null && toggleActionY.action != null)
                _activeActionY = toggleActionY.action;
            else
            {
                _runtimeActionY = new InputAction("ToggleRadialMenuY", InputActionType.Button);
                _runtimeActionY.AddBinding("<XRController>{LeftHand}/secondaryButton").WithInteraction("press(pressPoint=0.5)");
                _runtimeActionY.Enable();
                _activeActionY = _runtimeActionY;
            }
            _activeActionY.Enable();
            _activeActionY.performed += OnToggleY;
        }
    }

    void OnDisable()
    {
        if (_activeAction != null)
        {
            _activeAction.performed -= OnToggle;
            if (_activeAction == _runtimeAction) _activeAction.Disable();
        }

        // ������Y �����
        if (_activeActionY != null)
        {
            _activeActionY.performed -= OnToggleY;
            if (_activeActionY == _runtimeActionY) _activeActionY.Disable();
        }
    }

    void Update()
    {
        // �༭�����̵��ԣ����� X ��壩
        if (debugKey != Key.None && Keyboard.current != null &&
            Keyboard.current[debugKey].wasPressedThisFrame)
            Toggle();

        if (radialMenuRoot && radialMenuRoot.activeSelf)
        {
            if (followLeftHand) FollowLeftHand();
            else if (faceHmd) FaceCamera();
        }

        // ������Y ������/����
        if (radialMenuRootY && radialMenuRootY.activeSelf)
        {
            if (followLeftHand) FollowLeftHandY();
            else if (faceHmd) FaceCameraY();
        }
    }

    void OnToggle(InputAction.CallbackContext _) => Toggle();
    void OnToggleY(InputAction.CallbackContext _) => ToggleY(); // ����

    void Toggle()
    {
        if (!radialMenuRoot) return;
        radialMenuRoot.SetActive(!radialMenuRoot.activeSelf);
        if (radialMenuRoot.activeSelf)
        {
            if (followLeftHand) SnapToLeftHand();
            if (faceHmd) FaceCamera();
        }
    }

    // ������Y �����л�
    void ToggleY()
    {
        if (!radialMenuRootY) return;
        radialMenuRootY.SetActive(!radialMenuRootY.activeSelf);
        if (radialMenuRootY.activeSelf)
        {
            if (followLeftHand) SnapToLeftHandY();
            if (faceHmd) FaceCameraY();
        }
    }

    void SnapToLeftHand()
    {
        if (!leftHand || !radialMenuRoot) return;
        radialMenuRoot.transform.position = leftHand.TransformPoint(handLocalOffset);
    }

    // ������Y ��������
    void SnapToLeftHandY()
    {
        if (!leftHand || !radialMenuRootY) return;
        radialMenuRootY.transform.position = leftHand.TransformPoint(handLocalOffset + extraOffsetY);
    }

    void FollowLeftHand()
    {
        if (!leftHand || !radialMenuRoot) return;

        Vector3 targetPos = leftHand.TransformPoint(handLocalOffset);
        radialMenuRoot.transform.position = Vector3.Lerp(
            radialMenuRoot.transform.position, targetPos, Time.deltaTime * followLerp);

        if (faceHmd) FaceCamera();
        else radialMenuRoot.transform.rotation =
                 Quaternion.LookRotation(leftHand.forward, Vector3.up);
    }

    // ������Y ���ĸ���
    void FollowLeftHandY()
    {
        if (!leftHand || !radialMenuRootY) return;

        Vector3 targetPos = leftHand.TransformPoint(handLocalOffset + extraOffsetY);
        radialMenuRootY.transform.position = Vector3.Lerp(
            radialMenuRootY.transform.position, targetPos, Time.deltaTime * followLerp);

        if (faceHmd) FaceCameraY();
        else radialMenuRootY.transform.rotation =
                 Quaternion.LookRotation(leftHand.forward, Vector3.up);
    }

    void FaceCamera()
    {
        var cam = Camera.main;
        if (!cam || !radialMenuRoot) return;
        radialMenuRoot.transform.rotation =
            Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }

    // ������Y ��峯�����
    void FaceCameraY()
    {
        var cam = Camera.main;
        if (!cam || !radialMenuRootY) return;
        radialMenuRootY.transform.rotation =
            Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }
}
