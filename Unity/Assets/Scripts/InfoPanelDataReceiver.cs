// InfoPanelDataReceiver.cs
using UnityEngine;
using TMPro;

// 这个脚本挂载在 infoPanel Prefab 上
public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI文本组件的引用")]
    [Tooltip("用于显示名称的文本框")]
    public TextMeshProUGUI nameText;

    [Tooltip("用于显示缩放的文本框")]
    public TextMeshProUGUI scaleText;

    public void UpdateInfo(string newName, Vector3 newScale)
    {
        if (nameText != null)
        {
            // 为了清晰，我们给文本加上标签
            nameText.text = "name: " + newName;
        }

        if (scaleText != null)
        {
            // 将 Vector3 格式化为易于阅读的字符串
            scaleText.text = "scale: " + newScale.ToString();
        }
    }
}