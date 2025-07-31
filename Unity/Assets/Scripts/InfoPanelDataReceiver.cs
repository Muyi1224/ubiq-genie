// InfoPanelDataReceiver.cs (������ȷ��)
using UnityEngine;
using TMPro;

public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI�ı����������")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scaleText;
    public TextMeshProUGUI promptText;

    [Header("��ɫ������")]
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

        // ����һ������ɫѡ����ֱ�ӻ�ȡ (���Ƽ�)
        Color currentColor = colorPicker.color;

        // Unity�е�Color�ṹ�����r,g,b,a�ĸ�����������Χ 0.0f �� 1.0f��
        Debug.Log("--- Current Color Values ---");
        Debug.Log($"From Color Picker (0.0-1.0): R={currentColor.r}, G={currentColor.g}, B={currentColor.b}");

        // ��������ת��Ϊ 0-255 ��Χ������ֵ
        int r = Mathf.RoundToInt(currentColor.r * 255f);
        int g = Mathf.RoundToInt(currentColor.g * 255f);
        int b = Mathf.RoundToInt(currentColor.b * 255f);
        Debug.Log($"From Color Picker (0-255): R={r}, G={g}, B={b}");

        // ����ɫת��Ϊʮ�������ַ���
        string hexColor = ColorUtility.ToHtmlStringRGB(currentColor);
        Debug.Log($"From Color Picker (Hex): #{hexColor}");
        Debug.Log("--------------------------");
    }
}