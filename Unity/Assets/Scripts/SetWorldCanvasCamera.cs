using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Canvas))]
public class SetWorldCanvasCamera : MonoBehaviour
{
    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }
}
