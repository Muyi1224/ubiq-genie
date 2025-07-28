using System.Collections;
using UnityEngine;

/// Attach this to the root object that holds the World-Space Canvas.
[RequireComponent(typeof(Canvas))]
public sealed class CanvasCameraBinder : MonoBehaviour
{
    [Tooltip("Leave empty to use Camera.main automatically")]
    public UnityEngine.Camera explicitCamera;

    [Tooltip("Try to (re)bind every time this object is enabled")]
    public bool rebindOnEnable = true;

    [Tooltip("Extra frames to keep retrying on startup")]
    public int retryFrames = 30;     // ¡Ö0.5 s @ 60 fps

    Canvas canvas;

    /* -------- Unity Messages -------- */
    void Awake() { canvas = GetComponent<Canvas>(); }
    void OnEnable() { if (rebindOnEnable) StartCoroutine(BindRoutine()); }
    void Start() { StartCoroutine(BindRoutine()); }

    /* -------- Binding Logic -------- */
    IEnumerator BindRoutine()
    {
        if (canvas.renderMode != RenderMode.WorldSpace) yield break;

        for (int i = 0; i <= retryFrames; i++)
        {
            var cam = explicitCamera ?? Camera.main ?? FindFirstCamera();
            if (cam != null)
            {
                canvas.worldCamera = cam;
#if UNITY_EDITOR
                Debug.Log($"{name}: bound to <b>{cam.name}</b> on try {i}", this);
#endif
                yield break;
            }
            yield return null;      // wait a frame, then retry
        }
        Debug.LogWarning($"{name}: no camera found for World-Space Canvas", this);
    }

    /* -------- Helpers -------- */
    static UnityEngine.Camera FindFirstCamera()
    {
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<UnityEngine.Camera>();
#else
        return FindObjectOfType<UnityEngine.Camera>();
#endif
    }
}