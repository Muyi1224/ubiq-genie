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
            // ���޸���FlexibleColorPicker��Bug�����д������ڿ�������������
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