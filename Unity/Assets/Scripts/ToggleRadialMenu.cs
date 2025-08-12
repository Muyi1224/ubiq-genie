using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("要显示/隐藏的菜单根")]
    public GameObject radialMenuRoot;

    [Header("（可选）引用现有 InputAction")]
    public InputActionReference toggleAction; // 不填则默认 LeftHand/X

    [Header("左手与摆放")]
    public Transform leftHand;                       // XR 左手控制器/锚点
    public Vector3 handLocalOffset = new Vector3(-0.05f, 0.07f, 0.12f); // 相对左手的本地方向偏移(左、上、前)
    public bool followLeftHand = true;               // 是否跟随
    public bool faceHmd = true;                      // 是否始终朝向HMD
    public float followLerp = 12f;                   // 平滑系数

    [Header("编辑器调试键")]
    public Key debugKey = Key.M;

    private InputAction _runtimeAction;
    private InputAction _activeAction;

    void Awake()
    {
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot 未赋值");
        else radialMenuRoot.SetActive(false);
    }

    void Start()
    {
        if (!radialMenuRoot) return;
        // 初次摆个大概位置，避免(0,0,0)
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
        // 编辑器键盘调试
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
            // 切换到显示时立刻摆放一次
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

        // 位置平滑到左手的偏移点
        Vector3 targetPos = leftHand.TransformPoint(handLocalOffset);
        radialMenuRoot.transform.position = Vector3.Lerp(
            radialMenuRoot.transform.position, targetPos, Time.deltaTime * followLerp);

        // 始终朝向头显，阅读更稳定
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
