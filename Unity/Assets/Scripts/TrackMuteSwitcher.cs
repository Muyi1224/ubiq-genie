// TrackMuteSwitcher.cs
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;

public class TrackMuteSwitcher : MonoBehaviour
{
    [Header("Toggle References")]
    public Toggle drumsToggle;   // Drums  
    public Toggle bassToggle;    // Bass   
    public Toggle otherToggle;   // Other  

    private NetworkContext ctx;  // �� SpawnMenu ע��
    public void SetContext(NetworkContext c) => ctx = c;

    // ���͵� Node �� JSON �ṹ
    private struct TrackMuteMessage
    {
        public string type;   // "trackmute"
        public string track;  // "drums" | "bass" | "other"
        public bool mute;   // true = mute, false = cancel mute
    }

    private void Start()
    {
        if (drumsToggle)
            drumsToggle.onValueChanged.AddListener(on => Send("drums", on));

        if (bassToggle)
            bassToggle.onValueChanged.AddListener(on => Send("bass", on));

        if (otherToggle)
            otherToggle.onValueChanged.AddListener(on => Send("other", on));
    }

    private void Send(string track, bool mute)
    {
        if (ctx.Scene == null)
        {
            Debug.LogWarning("[TrackMute] context not set !");
            return;
        }

        var msg = new TrackMuteMessage
        {
            type = "trackmute",
            track = track,
            mute = !mute
        };

        ctx.SendJson(msg); 
        Debug.Log($"[TrackMute] {track} �� mute = {mute}");
    }
}
