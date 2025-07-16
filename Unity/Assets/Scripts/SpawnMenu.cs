using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Ubiq.Messaging;
using TMPro;
using System;
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

            buttonGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                SpawnObject(prefab);
                context.SendJson(new SpawnMessage { objectName = objectName });
                Debug.Log($"send name: {objectName}");
            });
        }
    }

    void SpawnObject(GameObject prefab, Vector3? position = null)
    {
        if (prefab)
        {
            Debug.Log("Instantiating prefab: " + prefab.name);
            var go = Instantiate(prefab);
            go.transform.position = position ?? (Camera.main.transform.position + Camera.main.transform.forward * 1.5f);

            //// 打包参数为 JSON 并发送
            //var data = new SpawnedObjectData
            //{
            //    name = prefab.name,
            //    position = go.transform.position,
            //    rotation = go.transform.eulerAngles,
            //    scale = go.transform.localScale
            //};

            //string json = JsonUtility.ToJson(data);
            //// TBD
            //context.SendJson(json);
            //Debug.Log("send json");
        }
        else
        {
            Debug.LogError("Prefab is null.");
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<SpawnMessage>();
        var item = spawnableItems.Find(i => i.name == msg.objectName);
        if (item != null)
        {
            SpawnObject(item.prefab);
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
            SpawnObject(item.prefab, position);
            context.SendJson(new SpawnMessage { objectName = item.name });
        }
        else
        {
            Debug.LogWarning("No item matched for voice command: " + name);
        }
    }

    struct SpawnMessage
    {
        public string objectName;
    }
}