using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MaterialSlotXR : XRGrabInteractable
{
    public Material mat;            // Inspector 里拖材质
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        // 松手后，检测射线最后一个碰撞体
        if (args.interactorObject is XRRayInteractor ray &&
            ray.TryGetCurrent3DRaycastHit(out var hit))
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend) rend.material = mat;
        }

        // 把图标瞬间回到面板（可选：Tween 回归）
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
