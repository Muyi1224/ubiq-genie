using UnityEngine;
using UnityEngine.EventSystems;                  // Required for mouse click interface (IPointerClickHandler).
using UnityEngine.XR.Interaction.Toolkit;        // Required for XR interaction events.
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// This component shows or hides an information panel when the object is clicked
/// by a mouse or selected by an XR controller.
/// </summary>
[RequireComponent(typeof(Collider))]             // Ensures the object has a Collider for interaction.
[RequireComponent(typeof(XRBaseInteractable))]   // Ensures the object can be interacted with via XR Toolkit.
public class ShowInfoOnClick : MonoBehaviour,
                               IPointerClickHandler // Implements the interface for handling pointer clicks.
{

    [Header("InfoPanel Prefab (World-Space Canvas)")]
    // The prefab for the UI panel to be instantiated.
    public InfoPanelUI infoPanelPrefab;

    [Header("Panel's Local Offset Relative to the Sphere")]
    // The local position offset of the panel relative to this object.
    public Vector3 localOffset = new Vector3(0.6f, 0.2f, 0f);

    [Header("Panel Orientation Fine-tuning (Euler Angles)")]
    // An additional rotation offset applied to the panel.
    public Vector3 rotationOffset = Vector3.zero;


    private InfoPanelUI currentPanel;            // A reference to the currently instantiated info panel.
    private XRBaseInteractable interactable;     // A reference to the XR interactable component on this object.


    void Awake()
    {
        // Get the XRBaseInteractable component (guaranteed to exist by RequireComponent).
        interactable = GetComponent<XRBaseInteractable>();

        // Listen for the 'selectEntered' event from XR controllers (e.g., grip or trigger press).
        interactable.selectEntered.AddListener(OnXRSelect);
        // To use the 'activated' event instead, you would use: interactable.activated.
    }

    void OnDestroy()
    {
        // Clean up the listener when the object is destroyed to prevent memory leaks.
        if (interactable)
            interactable.selectEntered.RemoveListener(OnXRSelect);
    }

    /* ========== Desktop: Left Mouse Click ========== */
    // This method is called when a mouse click is detected on this object's collider.
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if the click was from the left mouse button.
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            TogglePanel();
        }
    }

    /* ========== XR: Controller/Ray Selection ========== */
    // This method is called when an XR controller selects this object.
    private void OnXRSelect(SelectEnterEventArgs args) => TogglePanel();

    /* ========== Core Logic: Open / Close Panel ========== */
    private void TogglePanel()
    {
        // If a panel instance already exists, destroy it.
        if (currentPanel)
        {
            Destroy(currentPanel.gameObject);
            currentPanel = null;
        }
        // Otherwise, create a new panel.
        else
        {
            // Find a World-Space Canvas in the scene to be the parent; if none, use the scene root.
            var canvas = FindAnyObjectByType<Canvas>();
            string objName = gameObject.name;

            // Instantiate the panel prefab at the calculated position and rotation.
            currentPanel = Instantiate(
                infoPanelPrefab,
                transform.position + transform.TransformVector(localOffset), // Apply local offset in world space.
                transform.rotation * Quaternion.Euler(rotationOffset),       // Apply rotation offset.
                canvas ? canvas.transform : null                             // Set parent transform.
            );

            // Call a custom method on the panel to set its display text.
            currentPanel.SetInfo(objName);
        }
    }

    /* ========== Update Panel's Position / Orientation Every Frame ========== */
    private void LateUpdate()
    {
        // If there is no active panel, do nothing.
        if (!currentPanel) return;

        // 1. Position ---- Keep the panel attached to the moving sphere.
        currentPanel.transform.position =
            transform.position + transform.TransformVector(localOffset);

        // 2. Orientation ---- Match the sphere's rotation (with an optional offset).
        currentPanel.transform.rotation =
            transform.rotation * Quaternion.Euler(rotationOffset);
    }
}