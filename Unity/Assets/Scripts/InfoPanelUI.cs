using UnityEngine;
using TMPro;

public class InfoPanelUI : MonoBehaviour
{
    [Header("A text component that displays a name")]
    public TextMeshProUGUI nameText;

    public void SetInfo(string name)
    {
        if (nameText != null)
        {
            nameText.text = $"name: {name}";
        }
        else
        {
            Debug.LogWarning("do not binding nameText");
        }
    }
}
