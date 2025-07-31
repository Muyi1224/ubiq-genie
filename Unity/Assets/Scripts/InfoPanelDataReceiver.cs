// InfoPanelDataReceiver.cs (最终正确版)
using UnityEngine;
using TMPro;

public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI文本组件的引用")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scaleText;
    public TextMeshProUGUI promptText;

    [Header("颜色控制器")]
    public FlexibleColorPicker colorPicker;

    private Renderer targetRenderer;

    public void UpdateInfo(string newName, Vector3 newScale, string newPrompt)
    {
        if (nameText != null) nameText.text = "name: " + newName;
        if (scaleText != null) scaleText.text = "scale: " + newScale.ToString();
        if (promptText != null) promptText.text = "Prompt: " + newPrompt;
    }

    public void SetTargetObject(Renderer rendererToControl)
    {
        targetRenderer = rendererToControl;

        if (targetRenderer != null && colorPicker != null)
        {
            // 在修复了FlexibleColorPicker的Bug后，这行代码现在可以完美工作了
            colorPicker.color = targetRenderer.material.color;
        }
    }

    void Update()
    {
        if (targetRenderer != null && colorPicker != null)
        {
            targetRenderer.material.color = colorPicker.color;
        }
    }
}