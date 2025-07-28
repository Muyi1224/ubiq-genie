using UnityEngine;
using UnityEngine.XR;

/// �������� InfoPanel Prefab ���㣨�� Canvas���� GameObject ��
[RequireComponent(typeof(Canvas))]
public class SetWorldCanvasCamera : MonoBehaviour
{
    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        // XR Rig �����ͨ������ "MainCamera" tag
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }
}
