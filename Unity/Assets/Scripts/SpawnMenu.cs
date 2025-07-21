using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Ubiq.Messaging;
using TMPro;
using System;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.VisualScripting;
using UnityEditor;
public class SpawnMenu : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    [System.Serializable]
    public class SpawnableItem
    {
        public string name;
        public GameObject prefab;
        [TextArea] public string description;
    }

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

    private NetworkId networkId = new NetworkId(99);
    private NetworkContext context;

    void Start()
    {
        //context = NetworkScene.Register(this);
        context = NetworkScene.Register(this, networkId);
        PopulateMenu();
    }

    void PopulateMenu()
    {
        foreach (var item in spawnableItems)
        {
            var buttonGO = Instantiate(buttonPrefab, buttonContainer);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = item.name;

            var prefab = item.prefab;
            var objectName = item.name;
            var description = item.description;


            buttonGO.GetComponent<Button>().onClick.AddListener(() =>
            {
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
                //context.SendJson(msg);
                //Debug.Log($"[SpawnMenu] Sent - Name: {msg.objectName}, Pos: {msg.position}, Scale: {msg.scale}");

            });
        }
    }

    
    GameObject SpawnObject(GameObject prefab, string objectName, string description, Vector3? position = null)
    {
        if (prefab)
        {
            Debug.Log("Instantiating prefab: " + prefab.name);
            var go = Instantiate(prefab);
            go.transform.position = position ?? (Camera.main.transform.position + Camera.main.transform.forward * 1.5f);

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

            // 添加唯一 ID 组件（或获取已有的）
            var idComponent = go.GetComponent<UniqueObjectId>();
            if (idComponent == null)
            {
                idComponent = go.AddComponent<UniqueObjectId>();
            }
            string objectId = idComponent.objectId; // 获取 GUID

            // 配置并赋值到 Sync 脚本
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


            // 放置后立即尝试同步一次 transform + 发出消息
            sync.SyncIfChanged();

            return go;
        }
        else
        {
            Debug.LogError("Prefab is null.");
            return null;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<SpawnMessage>();
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

    public void SpawnByName(string name, Vector3? position = null)
    {
        Debug.Log("Spawn by name: " + name);
        var item = spawnableItems.Find(i => i.name.ToLower() == name.ToLower());
        if (item != null)
        {
            SpawnObject(item.prefab, item.name, item.description);
            context.SendJson(new SpawnMessage { objectName = item.name });
        }
        else
        {
            Debug.LogWarning("No item matched for voice command: " + name);
        }
    }

  
}