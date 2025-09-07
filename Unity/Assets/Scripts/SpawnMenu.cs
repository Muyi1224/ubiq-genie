using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Ubiq.Messaging;
using TMPro;
using System;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.VisualScripting;
using UnityEditor;
using System.Linq;

public class SpawnMenu : MonoBehaviour
{
    // Singleton instance for easy access.
    public static SpawnMenu Instance { get; private set; }
    // Prefab for the buttons in the spawn menu.
    public GameObject buttonPrefab;
    // The parent transform where buttons will be created.
    public Transform buttonContainer;
    // Caches prompts and their associated object IDs.
    private Dictionary<string, PromptData> promptDictionary = new Dictionary<string, PromptData>();

    [System.Serializable]
    public class SpawnableItem
    {
        public string name;
        public GameObject prefab;
        [TextArea] public string description;
    }

    [System.Serializable]
    public class MessageType
    {
        // Used to parse the 'type' field from a generic JSON message.
        public string type;
    }

    // Class specifically for parsing PromptMapUpdate messages.
    [System.Serializable]
    public class PromptData
    {
        public string prompt;
        public string[] objectIds;
    }

    [System.Serializable]
    public class PromptMapUpdateMessage
    {
        public string type;
        public string updateType;
        public PromptData[] data;
        public long ts;
    }

    // The list of items that can be spawned, configured in the Inspector.
    public List<SpawnableItem> spawnableItems;

    public class SpawnedObjectData
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    public struct SpawnMessage
    {
        public string objectName;
        public string description;
        public Vector3 position;
        public Vector3 scale;
        public Vector3 rotation;
        public string objectId;
        public string type;
    }

    // Ubiq networking variables.
    private NetworkId networkId = new NetworkId(99);
    private NetworkContext context;
    // A shared context to ensure all instances use the same one.
    private static NetworkContext sharedContext;
    private static bool hasSharedContext = false;

    [Header("UI References")]
    public OptionSwitcher[] optionSwitchers;
    public TrackMuteSwitcher trackMuteSwitcher;
    public BpmSender bpmSender;

    void Awake()
    {
        // Set up the singleton pattern.
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        // Register with the Ubiq network, creating or using a shared context.
        if (!hasSharedContext)
        {
            context = NetworkScene.Register(this, networkId);
            sharedContext = context;
            hasSharedContext = true;
        }
        else
        {
            context = sharedContext;
        }

        // Inject the network context into other components.
        if (trackMuteSwitcher) trackMuteSwitcher.SetContext(context);
        if (bpmSender) bpmSender.SetContext(context);

        foreach (var sw in optionSwitchers)
        {
            if (sw) sw.SetContext(context);
        }

        // Create the menu buttons.
        PopulateMenu();
    }

    void PopulateMenu()
    {
        // Create a button for each item in the spawnableItems list.
        foreach (var item in spawnableItems)
        {
            var buttonGO = Instantiate(buttonPrefab, buttonContainer);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = item.name;

            var prefab = item.prefab;
            var objectName = item.name;
            var description = item.description;

            // Add a listener to the button's click event.
            buttonGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                // Spawn the object locally when the button is clicked.
                var go = SpawnObject(prefab, objectName, description);
                var idComponent = go.GetComponent<UniqueObjectId>();
                string objectId = idComponent != null ? idComponent.objectId : Guid.NewGuid().ToString();
                var msg = new SpawnMessage
                {
                    objectId = objectId,
                    objectName = objectName,
                    description = description,
                    position = go.transform.position,
                    rotation = go.transform.eulerAngles,
                    scale = go.transform.localScale
                };
                // Note: Sending spawn message is currently commented out.
                //context.SendJson(msg);
                //Debug.Log($"[SpawnMenu] Sent - Name: {msg.objectName}, Pos: {msg.position}, Scale: {msg.scale}");
            });
        }
    }

    // Main function to instantiate and configure a spawned object.
    GameObject SpawnObject(GameObject prefab, string objectName, string description, Vector3? position = null)
    {
        if (prefab)
        {
            Debug.Log("Instantiating prefab: " + prefab.name);
            var go = Instantiate(prefab);
            go.name = objectName;
            // Set position in front of the camera if not specified.
            go.transform.position = position ?? (Camera.main.transform.position + Camera.main.transform.forward * 1.5f);

            // Ensure necessary components exist on the spawned object.
            if (go.GetComponent<XRGrabInteractable>() == null)
            {
                go.AddComponent<XRGrabInteractable>();
            }

            if (go.GetComponent<DeleteOnButton>() == null)
            {
                go.AddComponent<DeleteOnButton>();
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.isKinematic = true;

            // Add or get the unique ID component.
            var idComponent = go.GetComponent<UniqueObjectId>();
            if (idComponent == null)
            {
                idComponent = go.AddComponent<UniqueObjectId>();
            }
            string objectId = idComponent.objectId; // Get the GUID.

            // Configure the network synchronization script.
            SyncTransformOnChange sync = go.GetComponent<SyncTransformOnChange>();
            if (sync == null)
            {
                sync = go.AddComponent<SyncTransformOnChange>();
            }
            sync.objectId = objectId;
            sync.objectName = objectName;
            sync.description = description;
            sync.context = context;

            var delete = go.GetComponent<DeleteOnButton>();

            // Immediately try to sync its state across the network.
            sync.SyncIfChanged();

            return go;
        }
        else
        {
            Debug.LogError("Prefab is null.");
            return null;
        }
    }

    // Entry point for processing messages received from the network.
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // First, determine the message type.
        var genericMessage = message.FromJson<MessageType>();

        // Route the message to the correct handler.
        switch (genericMessage.type)
        {
            case "PromptMapUpdate":
                var promptMsg = message.FromJson<PromptMapUpdateMessage>();
                HandlePromptMapUpdate(promptMsg);
                break;

            case "SpawnObject":
                var spawnMsg = message.FromJson<SpawnMessage>();
                HandleSpawnObject(spawnMsg);
                break;

            default:
                Debug.LogWarning("Received unknown message type: " + genericMessage.type);
                break;
        }
    }

    // Handles a network message to spawn an object.
    private void HandleSpawnObject(SpawnMessage msg)
    {
        var item = spawnableItems.Find(i => i.name == msg.objectName);
        if (item != null)
        {
            SpawnObject(item.prefab, item.name, item.description);
        }
        else
        {
            Debug.LogError("No prefab found for: " + msg.objectName);
        }
    }

    // Handles updates to the prompt-to-object-ID mapping.
    private void HandlePromptMapUpdate(PromptMapUpdateMessage msg)
    {
        Debug.Log($"Received prompt-map ({msg.updateType}) with {msg.data.Length} items");

        switch (msg.updateType)
        {
            // Add a new prompt.
            case "add":
                foreach (var pd in msg.data)
                {
                    if (!promptDictionary.ContainsKey(pd.prompt))
                    {
                        promptDictionary[pd.prompt] = pd;
                        Debug.Log($"[ADD]   + {pd.prompt}");
                    }
                }
                break;

            // Update an existing prompt (e.g., after scaling).
            case "scale":
                foreach (var pd in msg.data)
                {
                    if (pd.objectIds == null) continue;

                    foreach (var objId in pd.objectIds)
                    {
                        var kvOld = promptDictionary
                                    .FirstOrDefault(kv =>
                                        kv.Value.objectIds != null &&
                                        kv.Value.objectIds.Contains(objId));

                        if (!string.IsNullOrEmpty(kvOld.Key))
                        {
                            promptDictionary.Remove(kvOld.Key);
                            Debug.Log($"[SCALE] - {kvOld.Key}");
                        }
                    }

                    promptDictionary[pd.prompt] = pd;
                    Debug.Log($"[SCALE] + {pd.prompt}");
                }
                break;

            // Delete a prompt or object IDs from a prompt.
            case "delete":
                foreach (var pd in msg.data)
                {
                    if (!promptDictionary.TryGetValue(pd.prompt, out var stored))
                    {
                        Debug.LogWarning($"[DEL] prompt not found: {pd.prompt}");
                        continue;
                    }

                    if (pd.objectIds != null && pd.objectIds.Length > 0)
                    {
                        var list = stored.objectIds.ToList();
                        int removed = 0;
                        foreach (var id in pd.objectIds)
                        {
                            if (list.Remove(id))
                                removed++;
                        }

                        if (removed > 0)
                        {
                            Debug.Log($"[DEL] {removed} id(s) from {pd.prompt}");
                        }

                        if (list.Count == 0)
                        {
                            promptDictionary.Remove(pd.prompt);
                            Debug.Log($"[DEL]  {pd.prompt} (no ids left)");
                        }
                        else
                        {
                            stored.objectIds = list.ToArray();
                        }
                    }
                    else
                    {
                        promptDictionary.Remove(pd.prompt);
                        Debug.Log($"[DEL]   {pd.prompt}");
                    }
                }
                break;

            // A full refresh of the entire dictionary.
            default:
                promptDictionary.Clear();
                foreach (var pd in msg.data)
                {
                    promptDictionary[pd.prompt] = pd;
                }
                Debug.Log("[FULL] promptDictionary replaced with new snapshot");
                break;
        }

        // Print the current state of the dictionary for debugging.
        if (promptDictionary.Count == 0)
        {
            Debug.Log("Dictionary is now EMPTY");
        }
        else
        {
            int idx = 0;
            foreach (var kv in promptDictionary)
            {
                string firstId = (kv.Value.objectIds != null && kv.Value.objectIds.Length > 0)
                                    ? kv.Value.objectIds[0] : "N/A";
                Debug.Log($"[{idx++}] Key=\"{kv.Key}\"  ->  ObjIDs=[{firstId}...]");
            }
            Debug.Log($"--- End print, total: {promptDictionary.Count} ---");
        }
    }

    // Finds the prompt associated with a specific object ID.
    public string GetPromptForObjectId(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return "ID is invalid";
        }

        // Search the dictionary for an entry containing the objectId.
        var promptEntry = promptDictionary
            .FirstOrDefault(kv => kv.Value.objectIds != null && kv.Value.objectIds.Contains(objectId));

        // If an entry is found, return its key (the prompt).
        if (promptEntry.Key != null)
        {
            return promptEntry.Key;
        }

        return "No associated prompt"; // Return a default text if not found.
    }

    // Spawns an object directly from a prefab reference.
    public void SpawnFromPrefab(GameObject prefab)
    {
        if (!prefab) return;

        // Find the matching item in the spawnable list to get its metadata.
        var item = spawnableItems.FirstOrDefault(i => i.prefab == prefab);

        // Use the metadata if found, otherwise use the prefab's name.
        string objName = item != null ? item.name : prefab.name;
        string desc = item != null ? item.description : "";

        SpawnObject(prefab, objName, desc);
    }
}