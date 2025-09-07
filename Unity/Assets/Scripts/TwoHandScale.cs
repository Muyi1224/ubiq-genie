using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// This component requires an XRGrabInteractable to function.
[RequireComponent(typeof(XRGrabInteractable))]
public class TwoHandScale : MonoBehaviour
{
    // Public properties for scaling limits and smoothing.
    public float minScale = 0.1f;         // The minimum allowed scale (in world space).
    public float maxScale = 3.0f;         // The maximum allowed scale.
    public float smooth = 12f;            // The smoothing factor for interpolation.

    // Reference to the XRGrabInteractable component.
    XRGrabInteractable grab;

    // --- Private state variables ---
    IXRSelectInteractor first;            // The first hand (interactor) grabbing the object.
    IXRSelectInteractor second;           // The second hand (interactor) grabbing the object.
    Vector3 initialScale;                 // The object's original scale when two-hand scaling begins.
    float initialDistance;                // The initial distance between the two hands.
    bool scaling;                         // A flag to indicate if two-hand scaling is active.

    void Awake()
    {
        // Get the XRGrabInteractable component.
        grab = GetComponent<XRGrabInteractable>();
        // Ensure the interactable can be selected by multiple interactors at the same time.
        grab.selectMode = InteractableSelectMode.Multiple;
    }

    void OnEnable()
    {
        // Subscribe to the select events.
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        // Unsubscribe from the select events to prevent memory leaks.
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    // Called when an interactor starts selecting (grabbing) this object.
    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // If this is the first hand grabbing the object.
        if (first == null)
        {
            first = args.interactorObject;
        }
        // If a second hand grabs the object.
        else if (second == null && args.interactorObject != first)
        {
            second = args.interactorObject;
            // Start the two-hand scaling process.
            BeginTwoHandScale();
        }
    }

    // Called when an interactor stops selecting (releasing) this object.
    void OnSelectExited(SelectExitEventArgs args)
    {
        // If either hand is released, exit the two-hand scaling mode.
        if (args.interactorObject == second) second = null;
        // If the first hand is released, promote the second hand to be the first.
        if (args.interactorObject == first) first = second;
        // Update the scaling flag.
        scaling = (first != null && second != null);
        // If two hands are still grabbing, re-initialize the scaling baseline.
        if (scaling) BeginTwoHandScale();
    }

    // Initializes the state for two-hand scaling.
    void BeginTwoHandScale()
    {
        // Record the object's current scale.
        initialScale = transform.localScale;
        // Record the initial distance between the two hands.
        initialDistance = Vector3.Distance(GetHandPose(first).position,
                                           GetHandPose(second).position);
        // Activate scaling only if the hands are a small distance apart.
        scaling = initialDistance > 1e-4f;
    }

    void Update()
    {
        // Only run the logic if scaling is active and both hands are valid.
        if (!scaling || first == null || second == null) return;

        // Get the current distance between the hands.
        float dist = Vector3.Distance(GetHandPose(first).position,
                                      GetHandPose(second).position);
        // Avoid division by zero or tiny values.
        if (dist <= 1e-4f) return;

        // The ratio of current distance to initial distance gives the scaling factor.
        float factor = dist / initialDistance;
        // Calculate the target scale by applying the factor.
        Vector3 target = initialScale * factor;

        // Clamp the target scale to the defined min/max range.
        target.x = Mathf.Clamp(target.x, minScale, maxScale);
        target.y = Mathf.Clamp(target.y, minScale, maxScale);
        target.z = Mathf.Clamp(target.z, minScale, maxScale);

        // Smoothly interpolate from the current scale to the target scale.
        transform.localScale = Vector3.Lerp(transform.localScale, target,
                                            Time.deltaTime * smooth);
    }

    // Gets the spatial pose of the "hand" (interactor).
    // It prioritizes the grab's attach transform for better accuracy.
    Transform GetHandPose(IXRSelectInteractor interactor)
    {
        // Try to get the specific attach point for this interactor.
        var attach = grab.GetAttachTransform(interactor);
        if (attach != null) return attach;
        // As a fallback, use the interactor's own transform.
        return (interactor as Component).transform;
    }
}