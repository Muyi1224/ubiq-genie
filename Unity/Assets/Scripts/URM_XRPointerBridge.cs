using UnityEngine;

public class URM_XRPointerBridge : MonoBehaviour
{
    public UltimateRadialMenu radialMenu; // ����� Ultimate Radial Menu ���
    public Transform rayOrigin;           // ���ֿ�����/���ߵ����
    public float rayLength = 1.2f;        // ���߳��ȣ�����UI�����

    void Update()
    {
        if (!radialMenu || !rayOrigin) return;
        var basePos = rayOrigin.position;
        var tipPos = basePos + rayOrigin.forward * rayLength;
        // �ؼ��������������ͽ� URM���������� hover/ѡ��
        radialMenu.inputManager.SendRaycastInput(tipPos, basePos);
    }
}
