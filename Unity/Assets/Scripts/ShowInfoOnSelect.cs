using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// 确保这个脚本挂载在可以被选择的、并且有Renderer组件的物体上
[RequireComponent(typeof(XRBaseInteractable))]
[RequireComponent(typeof(Renderer))]
public class ShowInfoOnSelect : MonoBehaviour
{
    [Header("UI 设置")]
    [Tooltip("要实例化显示的信息面板Prefab")]
    public GameObject infoPanelPrefab;

    [Tooltip("UI面板相对于物体的局部位置偏移")]
    public Vector3 panelOffset = new Vector3(0.3f, 0f, 0f);

    private GameObject _currentInfoPanel;
    private XRBaseInteractable _interactable;

    void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        _interactable.selectEntered.AddListener(ToggleInfoPanel);
    }

    void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(ToggleInfoPanel);
    }

    void Update()
    {
        if (_currentInfoPanel != null && Camera.main != null)
        {
            _currentInfoPanel.transform.LookAt(Camera.main.transform);
            _currentInfoPanel.transform.Rotate(0f, 180f, 0f); // 旋转180度，使其正面朝向相机
        }
    }

    private void ToggleInfoPanel(SelectEnterEventArgs args)
    {
        if (_currentInfoPanel == null)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    private void ShowPanel()
    {
        if (infoPanelPrefab == null) return;

        _currentInfoPanel = Instantiate(infoPanelPrefab);
        _currentInfoPanel.transform.SetParent(this.transform);
        _currentInfoPanel.transform.localPosition = panelOffset;
        _currentInfoPanel.transform.localRotation = Quaternion.identity;

        Canvas panelCanvas = _currentInfoPanel.GetComponent<Canvas>();
        if (panelCanvas != null)
        {
            panelCanvas.worldCamera = Camera.main;
        }

        // --- 核心修改点 ---
        // 1. 使用 GetComponentInChildren 在子对象中查找脚本
        InfoPanelDataReceiver panelData = _currentInfoPanel.GetComponentInChildren<InfoPanelDataReceiver>();

        if (panelData != null)
        {
            // 成功找到脚本，现在可以传递数据了
            Debug.Log("成功在InfoPanel Prefab的子对象中找到 InfoPanelDataReceiver 脚本。");

            // 2. 调用 SetTargetObject，将这个物体的Renderer传递过去，以控制颜色
            panelData.SetTargetObject(GetComponent<Renderer>());

            // --- 以下是原有的逻辑，保持不变 ---
            string objectName = gameObject.name;
            Vector3 objectScale = transform.localScale;
            string objectId = "ID not found";
            SyncTransformOnChange syncScript = GetComponent<SyncTransformOnChange>();
            if (syncScript != null)
            {
                objectId = syncScript.objectId;
            }

            string prompt = "Prompt not available";
            if (SpawnMenu.Instance != null)
            {
                prompt = SpawnMenu.Instance.GetPromptForObjectId(objectId);
            }
            else
            {
            }

            // 更新文本信息
            panelData.UpdateInfo(objectName, objectScale, prompt);
        }
        else
        {
            Debug.LogError("在InfoPanel Prefab及其所有子对象上都没有找到 InfoPanelDataReceiver 脚本！请检查Prefab的设置。", infoPanelPrefab);
        }
    }

    private void HidePanel()
    {
        if (_currentInfoPanel != null)
        {
            Destroy(_currentInfoPanel);
            _currentInfoPanel = null;
        }
    }
}