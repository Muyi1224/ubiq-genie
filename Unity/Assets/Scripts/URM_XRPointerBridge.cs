using UnityEngine;

public class URM_XRPointerBridge : MonoBehaviour
{
    public UltimateRadialMenu radialMenu; // Drag your Ultimate Radial Menu component
    public Transform rayOrigin;           // Right controller/ray origin
    public float rayLength = 1.2f;        // Ray length, adjust based on your UI distance

    void Update()
    {
        if (!radialMenu || !rayOrigin) return;
        var basePos = rayOrigin.position;
        var tipPos = basePos + rayOrigin.forward * rayLength;
        // Send the two points of the ray to the URM and let it calculate hover/select
        radialMenu.inputManager.SendRaycastInput(tipPos, basePos);
    }
}
