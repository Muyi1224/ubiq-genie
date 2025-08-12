using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("Ҫ��ʾ/���صĲ˵���")]
    public GameObject radialMenuRoot;        // ָ�� Canvas �� Ultimate Radial Menu

    [Header("����ѡ���������� InputAction")]
    public InputActionReference toggleAction; // �������գ�����ʱ�ô��������X��

    [Header("�༭�����Լ�")]
    public Key debugKey = Key.M;

    private InputAction _runtimeAction;   // �����ﴴ���� Action
    private InputAction _activeAction;    // ʵ�����õ� Action�����û�����ʱ������

    void Start()
    {
        Debug.Log($"[TRM] Start, root={(radialMenuRoot ? radialMenuRoot.name : "NULL")}");
        if (!radialMenuRoot) return;

        // 1) ǿ����ʾ
        radialMenuRoot.SetActive(true);

        // 2) ���������ǰ�� 0.6m�����������
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
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot δ��ֵ");
        else radialMenuRoot.SetActive(false);
    }

    void OnEnable()
    {
        Debug.Log("[TRM] OnEnable");
        // 1) ������ Inspector �ﴫ�������
        if (toggleAction != null && toggleAction.action != null)
        {
            _activeAction = toggleAction.action;
        }
        else
        {
            // 2) ��������ﴴ�������� X��primaryButton������ Press ����
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
