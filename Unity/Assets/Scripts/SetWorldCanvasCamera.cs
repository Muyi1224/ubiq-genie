using UnityEngine;
using UnityEngine.XR;

/// 把它挂在 InfoPanel Prefab 顶层（含 Canvas）的 GameObject 上
[RequireComponent(typeof(Canvas))]
public class SetWorldCanvasCamera : MonoBehaviour
{
    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        // XR Rig 的相机通常带有 "MainCamera" tag
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }
}
