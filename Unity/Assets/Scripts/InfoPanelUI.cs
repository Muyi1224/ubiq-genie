using UnityEngine;
using TMPro;

public class InfoPanelUI : MonoBehaviour
{
    [Header("��ʾ���ֵ��ı����")]
    public TextMeshProUGUI nameText;

    public void SetInfo(string name)
    {
        if (nameText != null)
        {
            nameText.text = $"����: {name}";
        }
        else
        {
            Debug.LogWarning("δ�� nameText");
        }
    }
}
