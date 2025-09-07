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

// This component requires an XRGrabInteractable.
[RequireComponent(typeof(XRGrabInteractable))]
public class SyncTransformOnChange : MonoBehaviour
{
    /* ===== Public & Network Fields ===== */
    // Basic properties of the object for networking.
    public string objectName;
    public string description;
    public string objectId;
    public NetworkContext context;

    /* ===== Custom Parameters ===== */
    // How long to wait after an edit before sending a network message.
    public float debounceSeconds = 1.0f;

    /* ===== Private State ===== */
    // Variables for managing the object's description.
    private string baseNoun;
    private string tail;
    private Color lastColor;
    private string lastMatName;

    // Last known transform values to detect changes.
    private Vector3 lastPos;
    private Vector3 lastRot;
    private Vector3 lastScale;
    private bool hasSentTransform;

    // State for grab interaction and debouncing edits.
    private XRGrabInteractable grab;
    private float lastEditTime;
    private bool editPending;

    // Defines the structure for a color change message.
    public struct colorMessage
    {
        public string description;
        public string objectId;
        public string type;
    }

    /* ---------------- Lifecycle ---------------- */
    void Awake()
    {
        // Get initial color and material name from the renderer.
        var r = GetComponent<Renderer>();
        lastColor = r ? r.material.color : Color.white;
        lastMatName = r ? CleanMatName(r.material.name) : "material";
    }

    void Start()
    {
        // Parse the description and set up grab event listeners.
        ParseInitialDescription(description, out baseNoun, out tail);

        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(_ => CacheTransform());
        // When released by all hands, check if transform has changed.
        grab.selectExited.AddListener(args =>
        {
            if (!grab.isSelected) SyncIfChanged();
        });

        // Store the initial transform.
        CacheTransform();
    }

    void Update()
    {
        // If an edit is pending and the debounce time has passed, send it.
        if (editPending && Time.time - lastEditTime >= debounceSeconds)
        {
            SendEdit();
            editPending = false;
        }
    }

    /* ---------------- Description Update ---------------- */
    // Updates the description when color or material changes.
    public void UpdateDescription(Color? newColor = null, Material newMat = null)
    {
        if (newColor != null) lastColor = (Color)newColor;
        if (newMat != null) lastMatName = CleanMatName(newMat.name);

        // Reconstruct the description string.
        string clr = ColorName(lastColor);
        string hex = ColorUtility.ToHtmlStringRGB(lastColor);

        description =
            $"A {clr} (#{hex}) {baseNoun} made of {lastMatName} {tail}".Replace("  ", " ");

        Debug.Log($"[Description] {description}");

        // Mark that an edit is pending.
        lastEditTime = Time.time;
        editPending = true;
    }

    // Sends the edit by deleting and re-adding the object on the network.
    private void SendEdit()
    {
        //var edit = new colorMessage { type = "edit", objectId = objectId, description = description };
        //context.SendJson(edit);

        //string logMessage = $"type: {edit.type}, objectId: {edit.objectId}, description: \"{edit.description}\"";
        //Debug.Log($"[Edit] Sending data: {{ {logMessage} }}");
        StartCoroutine(DeleteThenAdd(0.03f));
    }

    // Coroutine to simulate an update by sending a delete message, then an add message.
    private IEnumerator DeleteThenAdd(float delaySeconds)
    {
        /* ---------- 1. delete ---------- */
        // Send a message to delete the object on other clients.
        var deleteMsg = new DeleteMessage
        {
            type = "delete",
            objectId = objectId
        };
        context.Scene.SendJson(new NetworkId(100), deleteMsg);
        Debug.Log($"[Edit¡úDelete] id:{objectId}");

        /* ---------- 2. wait then add ---------- */
        yield return new WaitForSeconds(delaySeconds);

        // Send a message to re-add the object with the updated description.
        var addMsg = new SpawnMessage
        {
            objectId = objectId,
            objectName = objectName,
            description = description,          // Use the new description
            //position = transform.position,
            //rotation = transform.eulerAngles,
            scale = transform.localScale,
            type = "add"
        };
        context.SendJson(addMsg);
        Debug.Log($"[Edit¡úAdd] id:{objectId} prompt:\"{description}\"");
    }

    /* ---------------- Transform Sync ---------------- */
    // Checks if the scale has changed and sends a network message.
    public void SyncIfChanged()
    {
        if (transform.localScale != lastScale)
        {
            // Create a message for the transform change.
            var msg = new SpawnMessage
            {
                objectId = objectId,
                objectName = objectName,
                description = description,
                //position = transform.position,
                //rotation = transform.eulerAngles,
                scale = transform.localScale,
                // Message type is 'add' for the first time, 'scale' for subsequent changes.
                type = hasSentTransform ? "scale" : "add"
            };
            context.SendJson(msg);

            Debug.Log($"[SyncTransform] Sent ({msg.type})  " +
                  $" des:{msg.description}  scale:{msg.scale}");

            hasSentTransform = true;
            CacheTransform(); // Update the cached transform.
        }
    }

    // Stores the current transform values.
    private void CacheTransform()
    {
        lastPos = transform.position;
        lastRot = transform.eulerAngles;
        lastScale = transform.localScale;
    }

    /* ---------------- Description Parsing ---------------- */
    // Extracts the base noun and tail from the initial description string.
    private static void ParseInitialDescription(string src,
                                                out string noun,
                                                out string tail)
    {
        int idx = src.IndexOf(" that ", StringComparison.OrdinalIgnoreCase);
        string head = idx >= 0 ? src[..idx].Trim() : src.Trim();
        tail = idx >= 0 ? src[idx..].Trim() : "that makes piano sounds";

        // Clean up the head string to find the noun.
        head = Regex.Replace(head, @"^(a|an|the)\s+", "", RegexOptions.IgnoreCase);
        head = Regex.Replace(head, @"made of .*$", "", RegexOptions.IgnoreCase).Trim();

        var words = head.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        noun = words.Length > 0 ? words[^1] : "object";
    }

    /* ---------------- Utility Functions ---------------- */
    // Cleans the material name string.
    private static string CleanMatName(string raw)
        => string.IsNullOrEmpty(raw) ? "material" : raw.Replace(" (Instance)", "").ToLower();

    // Converts a Color to a human-readable name.
    private static string ColorName(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        h *= 360f;

        // Handle grayscale colors.
        if (s < 0.12f)
        {
            if (v >= 0.96f) return "white";
            if (v >= 0.82f) return "very light grey";
            if (v >= 0.60f) return "light grey";
            if (v >= 0.38f) return "grey";
            if (v >= 0.20f) return "dark grey";
            return "black";
        }

        // Determine brightness prefix.
        string prefix =
            (v >= 0.92f) ? "pale " :
            (v >= 0.80f) ? "light " :
            (v >= 0.55f) ? "bright " :
            (v >= 0.35f) ? "" :
            (v >= 0.20f) ? "dark " :
                           "deep ";

        // Determine base color from hue.
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