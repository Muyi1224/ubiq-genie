using UnityEngine;
using TMPro;

public class InfoPanelDataReceiver : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scaleText;
    public TextMeshProUGUI promptText;
    public FlexibleColorPicker colorPicker;

    private Renderer targetRenderer;
    private SyncTransformOnChange sync;
    private Color lastSent;

    public void UpdateInfo(string newName, Vector3 newScale, string newPrompt)
    {
        if (nameText) nameText.text = "name: " + newName;
        if (scaleText) scaleText.text = "scale: " + newScale;
        if (promptText) promptText.text = "Prompt: " + newPrompt;
    }

    public void SetTargetObject(Renderer rendererToControl)
    {
        targetRenderer = rendererToControl;
        sync = targetRenderer ? targetRenderer.GetComponent<SyncTransformOnChange>() : null;

        if (targetRenderer && colorPicker)
        {
            colorPicker.color = targetRenderer.material.color;
            lastSent = colorPicker.color;
        }
    }

    void Update()
    {
        if (!targetRenderer || !colorPicker) return;

        Color now = colorPicker.color;
        if (now != lastSent)
        {
            targetRenderer.material.color = now;
            sync?.UpdateDescription(now, null);
            lastSent = now;
        }
    }
}
