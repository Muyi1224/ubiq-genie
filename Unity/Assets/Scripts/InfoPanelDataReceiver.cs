// InfoPanelDataReceiver.cs
using UnityEngine;
using TMPro;

// ����ű������� infoPanel Prefab ��
public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI�ı����������")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scaleText;
    //public TextMeshProUGUI idText;

    [Tooltip("������ʾPrompt���ı���")]
    public TextMeshProUGUI promptText;

    public void UpdateInfo(string newName, Vector3 newScale, string newPrompt) //, string newId)
    {
        if (nameText != null)
        {
            nameText.text = "name: " + newName;
        }

        if (scaleText != null)
        {
            scaleText.text = "scale: " + newScale.ToString();
        }

        //if (idText != null)
        //{
        //    idText.text = "ID: " + newId;
        //}

        if (promptText != null)
        {
            promptText.text = "Prompt: " + newPrompt;
        }
    }
}