using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;   // ������ϵͳ

[RequireComponent(typeof(XRGrabInteractable))]
public class ToggleSelectionOnGrab : MonoBehaviour
{
    XRGrabInteractable grab;
    Renderer rend;

    [Header("Desktop input (for PC testing)")]
    public bool enableMouse = true;
    public float maxRayDistance = 100f;
    public LayerMask mouseLayers = ~0; // ������߼���

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rend = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        // ��Ҫ���� selectExited�������ǿ�����ѡ�У��ƻ����ٴ�ץȡ=ȡ��ѡ�С����߼�
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
    }

    // VR �ֱ�ץȡ����
    void OnGrab(SelectEnterEventArgs args)
    {
        if (rend && VRSelectionManager.Instance)
        {
            // �ٴ�ץͬһ��������� ToggleSelection �� ȡ��ѡ��
            VRSelectionManager.Instance.ToggleSelection(rend);
        }
    }

    // ������������������ϵͳ��
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
                // �㵽�հ״������ѡ��
                VRSelectionManager.Instance?.ClearSelection();
            }
        }
    }
}
