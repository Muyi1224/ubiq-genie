// InfoPanelDataReceiver.cs
using UnityEngine;
using TMPro;

// ����ű������� infoPanel Prefab ��
public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI�ı����������")]
    [Tooltip("������ʾ���Ƶ��ı���")]
    public TextMeshProUGUI nameText;

    [Tooltip("������ʾ���ŵ��ı���")]
    public TextMeshProUGUI scaleText;

    // ��δ���������ɵ���������Ӹ����ı��򣬱��� descriptionText

    /// <summary>
    /// �������������ڸ����������ʾ��������Ϣ
    /// </summary>
    /// <param name="newName">Ҫ��ʾ����������</param>
    /// <param name="newScale">Ҫ��ʾ����������ֵ</param>
    public void UpdateInfo(string newName, Vector3 newScale)
    {
        if (nameText != null)
        {
            // Ϊ�����������Ǹ��ı����ϱ�ǩ
            nameText.text = "name: " + newName;
        }

        if (scaleText != null)
        {
            // �� Vector3 ��ʽ��Ϊ�����Ķ����ַ���
            scaleText.text = "scale: " + newScale.ToString();
        }
    }
}