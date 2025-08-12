using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("Ҫ��ʾ/���صĲ˵���")]
    public GameObject radialMenuRoot;

    [Header("����ѡ���������� InputAction")]
    public InputActionReference toggleAction; // ������Ĭ�� LeftHand/X

    [Header("������ڷ�")]
    public Transform leftHand;                       // XR ���ֿ�����/ê��
    public Vector3 handLocalOffset = new Vector3(-0.05f, 0.07f, 0.12f); // ������ֵı��ط���ƫ��(���ϡ�ǰ)
    public bool followLeftHand = true;               // �Ƿ����
    public bool faceHmd = true;                      // �Ƿ�ʼ�ճ���HMD
    public float followLerp = 12f;                   // ƽ��ϵ��

    [Header("�༭�����Լ�")]
    public Key debugKey = Key.M;

    private InputAction _runtimeAction;
    private InputAction _activeAction;

    void Awake()
    {
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot δ��ֵ");
        else radialMenuRoot.SetActive(false);
    }

    void Start()
    {
        if (!radialMenuRoot) return;
        // ���ΰڸ����λ�ã�����(0,0,0)
        var cam = Camera.main;
        if (cam)
        {
            radialMenuRoot.transform.position = cam.transform.position + cam.transform.forward * 0.6f;
            radialMenuRoot.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }
    }

    void OnEnable()
    {
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
    }

    void OnDisable()
    {
        if (_activeAction != null)
        {
            _activeAction.performed -= OnToggle;
            if (_activeAction == _runtimeAction) _activeAction.Disable();
        }
    }

    void Update()
    {
        // �༭�����̵���
        if (debugKey != Key.None && Keyboard.current != null &&
            Keyboard.current[debugKey].wasPressedThisFrame)
            Toggle();

        if (radialMenuRoot && radialMenuRoot.activeSelf)
        {
            if (followLeftHand) FollowLeftHand();
            else if (faceHmd) FaceCamera();
        }
    }

    void OnToggle(InputAction.CallbackContext _) => Toggle();

    void Toggle()
    {
        if (!radialMenuRoot) return;
        radialMenuRoot.SetActive(!radialMenuRoot.activeSelf);
        if (radialMenuRoot.activeSelf)
        {
            // �л�����ʾʱ���̰ڷ�һ��
            if (followLeftHand) SnapToLeftHand();
            if (faceHmd) FaceCamera();
        }
    }

    void SnapToLeftHand()
    {
        if (!leftHand || !radialMenuRoot) return;
        radialMenuRoot.transform.position = leftHand.TransformPoint(handLocalOffset);
    }

    void FollowLeftHand()
    {
        if (!leftHand || !radialMenuRoot) return;

        // λ��ƽ�������ֵ�ƫ�Ƶ�
        Vector3 targetPos = leftHand.TransformPoint(handLocalOffset);
        radialMenuRoot.transform.position = Vector3.Lerp(
            radialMenuRoot.transform.position, targetPos, Time.deltaTime * followLerp);

        // ʼ�ճ���ͷ�ԣ��Ķ����ȶ�
        if (faceHmd) FaceCamera();
        else radialMenuRoot.transform.rotation =
                 Quaternion.LookRotation(leftHand.forward, Vector3.up);
    }

    void FaceCamera()
    {
        var cam = Camera.main;
        if (!cam || !radialMenuRoot) return;
        radialMenuRoot.transform.rotation =
            Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }
}
