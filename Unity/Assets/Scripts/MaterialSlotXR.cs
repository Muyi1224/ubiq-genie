using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MaterialSlotXR : XRGrabInteractable
{
    public Material mat;  
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        // After releasing the hand, detect the last collision body of the ray
        if (args.interactorObject is XRRayInteractor ray &&
            ray.TryGetCurrent3DRaycastHit(out var hit))
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend) rend.material = mat;
        }

        // Instantly return the icon to the panel
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
