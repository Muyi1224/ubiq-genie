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

    public void LogCurrentColorValues()
    {
        if (colorPicker == null)
        {
            Debug.LogWarning("Color Picker is not assigned!");
            return;
        }

        // 方法一：从颜色选择器直接获取 (最推荐)
        Color currentColor = colorPicker.color;

        // Unity中的Color结构体包含r,g,b,a四个浮点数（范围 0.0f 到 1.0f）
        Debug.Log("--- Current Color Values ---");
        Debug.Log($"From Color Picker (0.0-1.0): R={currentColor.r}, G={currentColor.g}, B={currentColor.b}");

        // 将浮点数转换为 0-255 范围的整数值
        int r = Mathf.RoundToInt(currentColor.r * 255f);
        int g = Mathf.RoundToInt(currentColor.g * 255f);
        int b = Mathf.RoundToInt(currentColor.b * 255f);
        Debug.Log($"From Color Picker (0-255): R={r}, G={g}, B={b}");

        // 将颜色转换为十六进制字符串
        string hexColor = ColorUtility.ToHtmlStringRGB(currentColor);
        Debug.Log($"From Color Picker (Hex): #{hexColor}");
        Debug.Log("--------------------------");
    }
}