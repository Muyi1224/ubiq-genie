using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
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

    // --- 新增的 Update 方法 ---
    /// <summary>
    /// 在每一帧调用
    /// </summary>
    void Update()
    {
        // 如果面板存在，就持续更新它的朝向，让它总是面对玩家
        if (_currentInfoPanel != null && Camera.main != null)
        {
            // 让面板朝向主摄像机
            _currentInfoPanel.transform.LookAt(Camera.main.transform);
            // 因为Canvas正面是Z轴负方向，所以需要“转身”180度
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
        if (infoPanelPrefab == null)
        {
            Debug.LogWarning("信息面板的Prefab没有被设置！", this);
            return;
        }

        // 1. 实例化面板。注意：此时先不用关心位置和旋转。
        _currentInfoPanel = Instantiate(infoPanelPrefab);

        // --- 核心改动：建立父子关系 ---
        // 2. 将面板设置为当前物体的子物体。这是实现跟随的关键！
        _currentInfoPanel.transform.SetParent(this.transform);

        // 3. 现在设置它的 *局部* 位置和旋转。
        //    因为已经是子物体，所以直接使用Offset作为局部坐标即可。
        _currentInfoPanel.transform.localPosition = panelOffset;
        _currentInfoPanel.transform.localRotation = Quaternion.identity; // 重置局部旋转

        // 4. 动态设置Canvas的Event Camera
        Canvas panelCanvas = _currentInfoPanel.GetComponent<Canvas>();
        if (panelCanvas != null)
        {
            panelCanvas.worldCamera = Camera.main;
        }

        // 5. 更新文本信息
        TextMeshProUGUI nameText = _currentInfoPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = gameObject.name;
        }

        // 6. 在Update中会处理朝向，这里可以不用再设置了
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