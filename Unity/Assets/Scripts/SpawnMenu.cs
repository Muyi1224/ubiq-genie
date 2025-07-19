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
                var msg = new SpawnMessage
                {
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

    //GameObject SpawnObject(GameObject prefab, string objectName, string description, Vector3? position = null)
    //{
    //    if (prefab)
    //    {
    //        Debug.Log("Instantiating prefab: " + prefab.name);
    //        var go = Instantiate(prefab);
    //        go.transform.position = position ?? (Camera.main.transform.position + Camera.main.transform.forward * 1.5f);

    //        // 检查并添加 XRGrabInteractable 组件
    //        if (go.GetComponent<XRGrabInteractable>() == null)
    //        {
    //            go.AddComponent<XRGrabInteractable>();
    //        }

    //        // 检查并添加 DeleteOnButton 组件
    //        if (go.GetComponent<DeleteOnButton>() == null)
    //        {
    //            go.AddComponent<DeleteOnButton>();
    //        }

    //        // 添加并配置 Rigidbody
    //        var rb = go.GetComponent<Rigidbody>();
    //        if (rb == null)
    //        {
    //            rb = go.AddComponent<Rigidbody>();
    //        }
    //        rb.useGravity = false;
    //        rb.isKinematic = true; // 让它不会被物理系统影响（比如掉下来）

    //        if (go.GetComponent<SyncTransformOnChange>() == null)
    //        {
    //            var sync = go.AddComponent<SyncTransformOnChange>();
    //            sync.objectName = objectName;  // 把名字传进去
    //            sync.description = description;
    //            sync.context = context;        // 传递 NetworkContext
    //        }

    //        return go;
    //    }
    //    else
    //    {
    //        Debug.LogError("Prefab is null.");
    //        return null;
    //    }
    //}

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

            SyncTransformOnChange sync = go.GetComponent<SyncTransformOnChange>();
            if (sync == null)
            {
                sync = go.AddComponent<SyncTransformOnChange>();
            }
            sync.objectName = objectName;
            sync.description = description;
            sync.context = context; // 传递 NetworkContext

            // ★ 新增：放置完成后立刻尝试同步一次（如果与默认缓存不同）
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