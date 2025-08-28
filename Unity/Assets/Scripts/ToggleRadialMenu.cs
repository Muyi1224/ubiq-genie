using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("要显示/隐藏的菜单根 (X 键)")]
    public GameObject radialMenuRoot;

    [Header("（可选）引用现有 InputAction (X 键)")]
    public InputActionReference toggleAction; // 不填则默认 LeftHand/X

    // ========== 新增：第二个面板，Y 键 ==========
    [Header("要显示/隐藏的第二个菜单根 (Y 键)")]
    public GameObject radialMenuRootY;

    [Header("（可选）引用现有 InputAction (Y 键)")]
    public InputActionReference toggleActionY; // 不填则默认 LeftHand/Y
    // =========================================

    [Header("左手与摆放")]
    public Transform leftHand;                       // XR 左手控制器/锚点
    //public Vector3 handLocalOffset = new Vector3(-0.05f, 0.07f, 0.12f); // 相对左手的本地方向偏移(左、上、前)
    public Vector3 handLocalOffset = new Vector3(-0.03f, 0.08f, 0.20f);
    public bool followLeftHand = true;               // 是否跟随
    public bool faceHmd = true;                      // 是否始终朝向HMD
    public float followLerp = 12f;                   // 平滑系数

    [Header("Y 面板专属偏移调整（在 handLocalOffset 基础上再加）")]
    public Vector3 extraOffsetY = new Vector3(0.05f, 0f, 0f);


    [Header("编辑器调试键（仅作用于 X 面板）")]
    public Key debugKey = Key.M;

    private InputAction _runtimeAction;
    private InputAction _activeAction;

    // ===== 新增：Y 键对应的输入 =====
    private InputAction _runtimeActionY;
    private InputAction _activeActionY;
    // ==============================

    void Awake()
    {
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot 未赋值");
        else radialMenuRoot.SetActive(false);

        // 新增：第二个面板初始隐藏
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

        // 新增：给第二个面板一个初始大致位置
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
        // X 键（primaryButton）
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

        // 新增：Y 键（secondaryButton）
        if (radialMenuRootY != null) // 只有你拖了第二个面板才启用
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

        // 新增：Y 键解绑
        if (_activeActionY != null)
        {
            _activeActionY.performed -= OnToggleY;
            if (_activeActionY == _runtimeActionY) _activeActionY.Disable();
        }
    }

    void Update()
    {
        // 编辑器键盘调试（仅切 X 面板）
        if (debugKey != Key.None && Keyboard.current != null &&
            Keyboard.current[debugKey].wasPressedThisFrame)
            Toggle();

        if (radialMenuRoot && radialMenuRoot.activeSelf)
        {
            if (followLeftHand) FollowLeftHand();
            else if (faceHmd) FaceCamera();
        }

        // 新增：Y 面板跟随/朝向
        if (radialMenuRootY && radialMenuRootY.activeSelf)
        {
            if (followLeftHand) FollowLeftHandY();
            else if (faceHmd) FaceCameraY();
        }
    }

    void OnToggle(InputAction.CallbackContext _) => Toggle();
    void OnToggleY(InputAction.CallbackContext _) => ToggleY(); // 新增

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

    // 新增：Y 面板的切换
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

    // 新增：Y 面板的贴手
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

    // 新增：Y 面板的跟随
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

    // 新增：Y 面板朝向相机
    void FaceCameraY()
    {
        var cam = Camera.main;
        if (!cam || !radialMenuRootY) return;
        radialMenuRootY.transform.rotation =
            Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }
}
