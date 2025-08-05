// TrackMuteSwitcher.cs
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;

/// <summary>
/// 负责把 Drums / Bass / Other 的静音状态通过 Ubiq 发给 Node → Python → MusicFX
/// Inspector 拖 3 个 Toggle，记得在 SpawnMenu 那边把 NetworkContext 注入进来：
///     trackMuteSwitcher.SetContext(context);
/// </summary>
public class TrackMuteSwitcher : MonoBehaviour
{
    [Header("Toggle References")]
    public Toggle drumsToggle;   // Drums  静音开关
    public Toggle bassToggle;    // Bass   静音开关
    public Toggle otherToggle;   // Other  静音开关

    private NetworkContext ctx;  // 由 SpawnMenu 注入
    public void SetContext(NetworkContext c) => ctx = c;

    // 发送到 Node 的 JSON 结构
    private struct TrackMuteMessage
    {
        public string type;   // "trackmute"
        public string track;  // "drums" | "bass" | "other"
        public bool mute;   // true = 静音, false = 取消静音
    }

    /* ─────────── 生命周期 ─────────── */
    private void Start()
    {
        if (drumsToggle)
            drumsToggle.onValueChanged.AddListener(on => Send("drums", on));

        if (bassToggle)
            bassToggle.onValueChanged.AddListener(on => Send("bass", on));

        if (otherToggle)
            otherToggle.onValueChanged.AddListener(on => Send("other", on));
    }

    /* ─────────── 发送 JSON ─────────── */
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
        Debug.Log($"[TrackMute] {track} → mute = {mute}");
    }
}
