using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleRadialMenu : MonoBehaviour
{
    [Header("Root of the menu to show/hide (X button)")]
    // The root GameObject of the radial menu toggled by the X button.
    public GameObject radialMenuRoot;

    [Header("(Optional) Reference an existing InputAction (X button)")]
    // An optional reference to a specific InputAction for the toggle behavior. Defaults to LeftHand/X if not set.
    public InputActionReference toggleAction;

    // ========== Added: Second panel for the Y button ==========
    [Header("Root of the second menu to show/hide (Y button)")]
    // The root GameObject of the second radial menu, toggled by the Y button.
    public GameObject radialMenuRootY;

    [Header("(Optional) Reference an existing InputAction (Y button)")]
    // An optional reference to a specific InputAction for the Y-button toggle. Defaults to LeftHand/Y if not set.
    public InputActionReference toggleActionY;

    [Header("Left Hand & Placement")]
    // A reference to the transform of the XR left hand controller/anchor.
    public Transform leftHand;
    // Local offset from the left hand (left/right, up/down, forward/back).
    //public Vector3 handLocalOffset = new Vector3(-0.05f, 0.07f, 0.12f);
    public Vector3 handLocalOffset = new Vector3(-0.03f, 0.08f, 0.20f);
    // Whether the menu should continuously follow the left hand.
    public bool followLeftHand = true;
    // Whether the menu should always orient itself to face the HMD.
    public bool faceHmd = true;
    // The smoothing coefficient for the follow interpolation.
    public float followLerp = 12f;

    [Header("Y-Panel Specific Offset Adjustment (added to handLocalOffset)")]
    // An extra offset applied only to the Y-panel, in addition to the base handLocalOffset.
    public Vector3 extraOffsetY = new Vector3(0.05f, 0f, 0f);


    [Header("Editor Debug Key (only affects the X panel)")]
    // A keyboard key for toggling the X panel during debugging in the Unity Editor.
    public Key debugKey = Key.M;

    
    private InputAction _runtimeAction;// A private field to hold a runtime-created input action if one is not provided.
    private InputAction _activeAction;// The currently active input action being used.

    // Input corresponding to the Y key
    private InputAction _runtimeActionY;
    private InputAction _activeActionY;

    void Awake()
    {
        if (!radialMenuRoot) Debug.LogWarning("[ToggleRadialMenu] radialMenuRoot Î´¸³Öµ");
        else radialMenuRoot.SetActive(false);

        // The second panel is initially hidden
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

        // Give the second panel an initial approximate position
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
        // X £¨primaryButton£©
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

        // Y £¨secondaryButton£©
        if (radialMenuRootY != null) 
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

        if (_activeActionY != null)
        {
            _activeActionY.performed -= OnToggleY;
            if (_activeActionY == _runtimeActionY) _activeActionY.Disable();
        }
    }

    void Update()
    {
        if (debugKey != Key.None && Keyboard.current != null &&
            Keyboard.current[debugKey].wasPressedThisFrame)
            Toggle();

        if (radialMenuRoot && radialMenuRoot.activeSelf)
        {
            if (followLeftHand) FollowLeftHand();
            else if (faceHmd) FaceCamera();
        }

        if (radialMenuRootY && radialMenuRootY.activeSelf)
        {
            if (followLeftHand) FollowLeftHandY();
            else if (faceHmd) FaceCameraY();
        }
    }

    void OnToggle(InputAction.CallbackContext _) => Toggle();
    void OnToggleY(InputAction.CallbackContext _) => ToggleY(); // ÐÂÔö

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

    // switch global parameters panel
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

    // panel follow
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

    // panel face to camera
    void FaceCameraY()
    {
        var cam = Camera.main;
        if (!cam || !radialMenuRootY) return;
        radialMenuRootY.transform.rotation =
            Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }
}
