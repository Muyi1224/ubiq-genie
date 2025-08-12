using UnityEngine;

public class URM_XRPointerBridge : MonoBehaviour
{
    public UltimateRadialMenu radialMenu; // 拖你的 Ultimate Radial Menu 组件
    public Transform rayOrigin;           // 右手控制器/射线的起点
    public float rayLength = 1.2f;        // 射线长度，按你UI距离调

    void Update()
    {
        if (!radialMenu || !rayOrigin) return;
        var basePos = rayOrigin.position;
        var tipPos = basePos + rayOrigin.forward * rayLength;
        // 关键：把射线两点送进 URM，让它计算 hover/选中
        radialMenu.inputManager.SendRaycastInput(tipPos, basePos);
    }
}
