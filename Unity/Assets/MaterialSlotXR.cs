using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MaterialSlotXR : XRGrabInteractable
{
    public Material mat;            // Inspector ���ϲ���
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        // ���ֺ󣬼���������һ����ײ��
        if (args.interactorObject is XRRayInteractor ray &&
            ray.TryGetCurrent3DRaycastHit(out var hit))
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend) rend.material = mat;
        }

        // ��ͼ��˲��ص���壨��ѡ��Tween �ع飩
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
