using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("要显示/隐藏的菜单根")]
    public GameObject radialMenuRoot;        // 指向 Canvas 或 Ultimate Radial Menu

    [Header("（可选）引用现有 InputAction")]
    public InputActionReference toggleAction; // 可以留空：留空时用代码绑定左手X键

    [Header("编辑器调试键")]
    public Key debugKey = Key.M;

    private InputAction _runtimeAction;   // 代码里创建的 Action
    private InputAction _activeAction;    // 实际在用的 Action（引用或运行时创建）

    void Start()
    {
        Debug.Log($"[TRM] Start, root={(radialMenuRoot ? radialMenuRoot.name : "NULL")}");
        if (!radialMenuRoot) return;

        // 1) 强制显示
        radialMenuRoot.SetActive(true);

        // 2) 丢到相机正前方 0.6m，并面向相机
        var cam = Camera.main;
        if (cam)
        {
            radialMenuRoot.transform.position = cam.transform.position + cam.transform.forward * 0.6f;
            radialMenuRoot.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }
        Debug.Log($"[TRM] root active={radialMenuRoot.activeSelf}, pos={radialMenuRoot.transform.position}");
    }


    void Awake()
    {
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot 未赋值");
        else radialMenuRoot.SetActive(false);
    }

    void OnEnable()
    {
        Debug.Log("[TRM] OnEnable");
        // 1) 优先用 Inspector 里传入的引用
        if (toggleAction != null && toggleAction.action != null)
        {
            _activeAction = toggleAction.action;
        }
        else
        {
            // 2) 否则代码里创建：左手 X（primaryButton），带 Press 交互
            _runtimeAction = new InputAction(
                name: "ToggleRadialMenu",
                type: InputActionType.Button
            );
            _runtimeAction.AddBinding("<XRController>{LeftHand}/primaryButton")
                          .WithInteraction("press(pressPoint=0.5)");
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
        if (debugKey != Key.None && Keyboard.current != null &&
            Keyboard.current[debugKey].wasPressedThisFrame)
        {
            Toggle();
        }

        if (radialMenuRoot && radialMenuRoot.activeSelf) FaceCamera();
    }

    void OnToggle(InputAction.CallbackContext _)
    {
        Debug.Log("[ToggleRadialMenu] toggle by input");
        Toggle();
    }

    void Toggle()
    {
        if (!radialMenuRoot) return;
        radialMenuRoot.SetActive(!radialMenuRoot.activeSelf);
        Debug.Log($"[TRM] Toggle() => {radialMenuRoot.activeSelf}");
        if (radialMenuRoot.activeSelf) FaceCamera();
    }

    void FaceCamera()
    {
        var cam = Camera.main;
        if (!cam || !radialMenuRoot) return;
        radialMenuRoot.transform.rotation =
            Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }
}
