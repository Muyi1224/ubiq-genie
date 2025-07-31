// InfoPanelDataReceiver.cs
using UnityEngine;
using TMPro;

// 这个脚本挂载在 infoPanel Prefab 上
public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI文本组件的引用")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scaleText;
    //public TextMeshProUGUI idText;

    [Tooltip("用于显示Prompt的文本框")]
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