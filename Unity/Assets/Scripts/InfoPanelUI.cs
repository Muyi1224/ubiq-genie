using UnityEngine;
using TMPro;

public class InfoPanelUI : MonoBehaviour
{
    [Header("显示名字的文本组件")]
    public TextMeshProUGUI nameText;

    public void SetInfo(string name)
    {
        if (nameText != null)
        {
            nameText.text = $"名字: {name}";
        }
        else
        {
            Debug.LogWarning("未绑定 nameText");
        }
    }
}
