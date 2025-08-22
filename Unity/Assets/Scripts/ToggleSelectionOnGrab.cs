using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;   // 新输入系统

[RequireComponent(typeof(XRGrabInteractable))]
public class ToggleSelectionOnGrab : MonoBehaviour
{
    XRGrabInteractable grab;
    Renderer rend;

    [Header("Desktop input (for PC testing)")]
    public bool enableMouse = true;
    public float maxRayDistance = 100f;
    public LayerMask mouseLayers = ~0; // 鼠标射线检测层

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rend = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        // 不要监听 selectExited，否则会强制清空选中，破坏“再次抓取=取消选中”的逻辑
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
    }

    // VR 手柄抓取触发
    void OnGrab(SelectEnterEventArgs args)
    {
        if (rend && VRSelectionManager.Instance)
        {
            // 再次抓同一个物体会走 ToggleSelection → 取消选中
            VRSelectionManager.Instance.ToggleSelection(rend);
        }
    }

    // 键鼠点击触发（新输入系统）
    void Update()
    {
        if (!enableMouse) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            var cam = Camera.main;
            if (!cam) return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, mouseLayers))
            {
                var r = hit.collider ? hit.collider.GetComponentInParent<Renderer>() : null;
                if (r && VRSelectionManager.Instance)
                {
                    VRSelectionManager.Instance.ToggleSelection(r);
                }
            }
            else
            {
                // 点到空白处，清除选中
                VRSelectionManager.Instance?.ClearSelection();
            }
        }
    }
}
