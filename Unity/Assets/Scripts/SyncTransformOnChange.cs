using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Ubiq.Messaging;
using static SpawnMenu;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static DeleteOnButton;
using System.Collections;

[RequireComponent(typeof(XRGrabInteractable))]
public class SyncTransformOnChange : MonoBehaviour
{
    /* ===== ���� & �����ֶ� ===== */
    public string objectName;
    public string description;
    public string objectId;
    public NetworkContext context;

    /* ===== �Զ������ ===== */
    public float debounceSeconds = 1.0f;       // ���޲�������ú�ŷ��� edit

    /* ===== ˽��״̬ ===== */
    private string baseNoun;
    private string tail;
    private Color lastColor;
    private string lastMatName;

    private Vector3 lastPos;
    private Vector3 lastRot;
    private Vector3 lastScale;
    private bool hasSentTransform;

    private XRGrabInteractable grab;
    private float lastEditTime;
    private bool editPending;

    public struct colorMessage
    {
        public string description;
        public string objectId;
        public string type;

    }

    /* ---------------- �������� ---------------- */
    void Awake()
    {
        var r = GetComponent<Renderer>();
        lastColor = r ? r.material.color : Color.white;
        lastMatName = r ? CleanMatName(r.material.name) : "material";
    }

    void Start()
    {
        ParseInitialDescription(description, out baseNoun, out tail);

        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(_ => CacheTransform());
        grab.selectExited.AddListener(_ => SyncIfChanged());

        CacheTransform();
    }

    void Update()
    {
        // ���д����͵� edit ���Ѿ�ֹ debounceSeconds������
        if (editPending && Time.time - lastEditTime >= debounceSeconds)
        {
            SendEdit();
            editPending = false;
        }
    }

    /* ---------------- �������� ---------------- */
    public void UpdateDescription(Color? newColor = null, Material newMat = null)
    {
        if (newColor != null) lastColor = (Color)newColor;
        if (newMat != null) lastMatName = CleanMatName(newMat.name);

        string clr = ColorName(lastColor);
        string hex = ColorUtility.ToHtmlStringRGB(lastColor);

        description =
            $"A {clr} (#{hex}) {baseNoun} made of {lastMatName} {tail}".Replace("  ", " ");

        Debug.Log($"[Description] {description}");

        lastEditTime = Time.time;
        editPending = true;
    }

    private void SendEdit()
    {
        //var edit = new colorMessage { type = "edit", objectId = objectId, description = description };
        //context.SendJson(edit);

        //string logMessage = $"type: {edit.type}, objectId: {edit.objectId}, description: \"{edit.description}\"";
        //Debug.Log($"[Edit] Sending data: {{ {logMessage} }}");
        StartCoroutine(DeleteThenAdd(0.03f));
    }

    private IEnumerator DeleteThenAdd(float delaySeconds)
    {
        /* ---------- 1. delete ---------- */
        var deleteMsg = new DeleteMessage
        {
            type = "delete",
            objectId = objectId
        };
        context.Scene.SendJson(new NetworkId(100), deleteMsg);
        Debug.Log($"[Edit��Delete] id:{objectId}");

        /* ---------- 2. �ȴ��� add ---------- */
        yield return new WaitForSeconds(delaySeconds);

        var addMsg = new SpawnMessage
        {
            objectId = objectId,
            objectName = objectName,
            description = description,          // ����������
            position = transform.position,
            rotation = transform.eulerAngles,
            scale = transform.localScale,
            type = "add"
        };
        context.SendJson(addMsg);
        Debug.Log($"[Edit��Add] id:{objectId} prompt:\"{description}\"");
    }

    /* ---------------- ͬ�� transform ---------------- */
    public void SyncIfChanged()
    {
        if (transform.position != lastPos ||
            transform.eulerAngles != lastRot ||
            transform.localScale != lastScale)
        {
            transform.localScale += Vector3.one * 0.1f;

            var msg = new SpawnMessage
            {
                objectId = objectId,
                objectName = objectName,
                description = description,
                position = transform.position,
                rotation = transform.eulerAngles,
                scale = transform.localScale,
                type = hasSentTransform ? "scale" : "add"
            };
            context.SendJson(msg);

            Debug.Log($"[SyncTransform] Sent ({msg.type})  " +
                  $"id:{msg.objectId}  pos:{msg.position}  " +
                  $"rot:{msg.rotation}  scale:{msg.scale}");

            hasSentTransform = true;
            CacheTransform();
        }
    }

    private void CacheTransform()
    {
        lastPos = transform.position;
        lastRot = transform.eulerAngles;
        lastScale = transform.localScale;
    }

    /* ---------------- �������� ---------------- */
    private static void ParseInitialDescription(string src,
                                                out string noun,
                                                out string tail)
    {
        int idx = src.IndexOf(" that ", StringComparison.OrdinalIgnoreCase);
        string head = idx >= 0 ? src[..idx].Trim() : src.Trim();
        tail = idx >= 0 ? src[idx..].Trim() : "that makes piano sounds";

        head = Regex.Replace(head, @"^(a|an|the)\s+", "", RegexOptions.IgnoreCase);
        head = Regex.Replace(head, @"made of .*$", "", RegexOptions.IgnoreCase).Trim();

        var words = head.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        noun = words.Length > 0 ? words[^1] : "object";
    }

    /* ---------------- ���ߺ��� ---------------- */
    private static string CleanMatName(string raw)
        => string.IsNullOrEmpty(raw) ? "material" : raw.Replace(" (Instance)", "").ToLower();

    private static string ColorName(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        h *= 360f;

        if (s < 0.12f) // �ҽ�
        {
            if (v >= 0.96f) return "white";
            if (v >= 0.82f) return "very light grey";
            if (v >= 0.60f) return "light grey";
            if (v >= 0.38f) return "grey";
            if (v >= 0.20f) return "dark grey";
            return "black";
        }

        string prefix =
            (v >= 0.92f) ? "pale " :
            (v >= 0.80f) ? "light " :
            (v >= 0.55f) ? "bright " :
            (v >= 0.35f) ? "" :
            (v >= 0.20f) ? "dark " :
                           "deep ";

        string baseColor;
        if (h < 8f) baseColor = "red";
        else if (h < 14f) baseColor = "vermillion";
        else if (h < 26f) baseColor = "orange";
        else if (h < 38f) baseColor = "amber";
        else if (h < 52f) baseColor = "yellow";
        else if (h < 62f) baseColor = "lemon";
        else if (h < 75f) baseColor = "chartreuse";
        else if (h < 90f) baseColor = "lime";
        else if (h < 105f) baseColor = "spring green";
        else if (h < 120f) baseColor = "emerald";
        else if (h < 135f) baseColor = "green";
        else if (h < 150f) baseColor = "sea green";
        else if (h < 165f) baseColor = "turquoise";
        else if (h < 180f) baseColor = "cyan";
        else if (h < 195f) baseColor = "aquamarine";
        else if (h < 210f) baseColor = "azure";
        else if (h < 225f) baseColor = "cerulean";
        else if (h < 240f) baseColor = "blue";
        else if (h < 255f) baseColor = "royal blue";
        else if (h < 270f) baseColor = "indigo";
        else if (h < 285f) baseColor = "violet";
        else if (h < 300f) baseColor = "purple";
        else if (h < 315f) baseColor = "magenta";
        else if (h < 330f) baseColor = "pink";
        else if (h < 345f) baseColor = "rose";
        else baseColor = "red";

        return (prefix + baseColor).TrimEnd();
    }
}