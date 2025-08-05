using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Ubiq.Messaging;

public class InfoPanelDataReceiver : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scaleText;
    public TextMeshProUGUI promptText;
    public FlexibleColorPicker colorPicker;
    public Toggle muteToggle;

    private Renderer targetRenderer;
    private SyncTransformOnChange sync;
    private Color lastSent;
    private bool suppressCallback = false; // ·ÀÖ¹ÇÐ»»Ä¿±êÊ±´¥·¢ Toggle

    [System.Serializable]
    private struct MuteMessage
    {
        public string type;
        public string objectId;
        public bool mute;
    }

    void Start()
    {
        if (muteToggle)
        {
            muteToggle.onValueChanged.AddListener(OnMuteToggle);
        }
    }

    public void UpdateInfo(string newName, Vector3 newScale, string newPrompt)
    {
        if (nameText) nameText.text = $"Name:  {newName}";
        if (scaleText) scaleText.text = $"Scale: {newScale}";
        if (promptText) promptText.text = $"Prompt: {newPrompt}";
    }

    public void SetTargetObject(Renderer rendererToControl)
    {
        targetRenderer = rendererToControl;
        sync = targetRenderer ? targetRenderer.GetComponent<SyncTransformOnChange>() : null;

        if (targetRenderer && colorPicker)
        {
            colorPicker.color = targetRenderer.material.color;
            lastSent = colorPicker.color;
        }

        if (muteToggle)
        {
            suppressCallback = true;
            muteToggle.isOn = false; // Ä¬ÈÏÎ´¾²Òô
            suppressCallback = false;
        }
    }

    void Update()
    {
        if (!targetRenderer || !colorPicker) return;

        Color now = colorPicker.color;
        if (now != lastSent)
        {
            targetRenderer.material.color = now;
            sync?.UpdateDescription(now, null);
            lastSent = now;
        }
    }

    public void OnMuteToggle(bool isOn)
    {
        if (suppressCallback || sync == null) return;

        var msg = new MuteMessage
        {
            type = "mute",
            objectId = sync.objectId,
            mute = isOn
        };

        sync.context.SendJson(msg);
        Debug.Log($"[Mute] Sent: objectId={msg.objectId}, mute={msg.mute}");
    }
}
