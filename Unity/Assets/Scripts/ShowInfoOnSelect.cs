using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
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
            _currentInfoPanel.transform.Rotate(0f, 180f, 0f);
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

        // --- 核心改动：获取数据接收器并传递所有信息 ---
        InfoPanelDataReceiver panelData = _currentInfoPanel.GetComponent<InfoPanelDataReceiver>();
        if (panelData != null)
        {
            // 1. 获取物体的名称
            string objectName = gameObject.name;
            // 2. 获取物体的当前缩放值
            Vector3 objectScale = transform.localScale;

            // 3. 调用面板的数据更新方法，把两条信息都传过去
            panelData.UpdateInfo(objectName, objectScale);
        }
        else
        {
            // 如果面板上没有找到接收器脚本，给出清晰的错误提示
            Debug.LogError("在InfoPanel Prefab上没有找到 InfoPanelDataReceiver 脚本！请检查Prefab的设置。", infoPanelPrefab);
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