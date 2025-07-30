// ShowInfoOnClick.cs          把脚本挂到每个可点击 / 可抓取的“球体”Prefab 上
using UnityEngine;
using UnityEngine.EventSystems;                  // 鼠标点击
using UnityEngine.XR.Interaction.Toolkit;       // XR 交互
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]            // 需要碰撞体用于点击／光线检测
[RequireComponent(typeof(XRBaseInteractable))]  // 保证有 XRBaseInteractable（抓取或选择用）
public class ShowInfoOnClick : MonoBehaviour,
                               IPointerClickHandler              // 桌面左键点击
{
    /* ---------------- 可在 Inspector 中赋值 ---------------- */

    [Header("InfoPanel 预制体 (World-Space Canvas)")]
    public InfoPanelUI infoPanelPrefab;          // 你的 World-Space 面板预制体

    [Header("面板相对球体的本地偏移量")]
    public Vector3 localOffset = new Vector3(0.6f, 0.2f, 0f);

    [Header("面板朝向微调 (欧拉角)")]
    public Vector3 rotationOffset = Vector3.zero;

    /* ---------------- 私有字段 ---------------- */

    private InfoPanelUI currentPanel;            // 运行时实例
    private XRBaseInteractable interactable;     // XR 组件 (Select / Activate 事件)

    /* ========== 初始化 / 反初始化 ========== */

    void Awake()
    {
        // 获取 XRBaseInteractable（已由 RequireComponent 保证存在）
        interactable = GetComponent<XRBaseInteractable>();

        // XR 手柄 / 激光：Select(按下) 或 Activate(Trigger) 都可
        interactable.selectEntered.AddListener(OnXRSelect);
        // 如果想改成 activated 事件，换成 interactable.activated
    }

    void OnDestroy()
    {
        if (interactable)
            interactable.selectEntered.RemoveListener(OnXRSelect);
    }

    /* ========== 桌面：鼠标左键点击 ========== */
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            TogglePanel();
        }
    }

    /* ========== XR：手柄/激光选中 ========== */
    private void OnXRSelect(SelectEnterEventArgs args) => TogglePanel();

    /* ========== 核心：开 / 关 面板 ========== */
    private void TogglePanel()
    {
        if (currentPanel)
        {
            Destroy(currentPanel.gameObject);
            currentPanel = null;
        }
        else
        {
            // 找一个 World-Space Canvas 作为父物体；没有就放场景根
            var canvas = FindAnyObjectByType<Canvas>();
            string objName = gameObject.name;

            currentPanel = Instantiate(
                infoPanelPrefab,
                transform.position + transform.TransformVector(localOffset),
                transform.rotation * Quaternion.Euler(rotationOffset),
                canvas ? canvas.transform : null
            );

            currentPanel.SetInfo(objName);   // 自定义：在 InfoPanelUI 里显示文字
        }
    }

    /* ========== 每帧更新面板的位置 / 朝向 ========== */
    private void LateUpdate()
    {
        if (!currentPanel) return;

        // 1. 位置 —— 随球移动
        currentPanel.transform.position =
            transform.position + transform.TransformVector(localOffset);

        // 2. 朝向 —— 跟随球自身旋转 (可附加欧拉偏移)
        currentPanel.transform.rotation =
            transform.rotation * Quaternion.Euler(rotationOffset);
    }
}
