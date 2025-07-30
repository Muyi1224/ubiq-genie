// ShowInfoOnClick.cs          �ѽű��ҵ�ÿ���ɵ�� / ��ץȡ�ġ����塱Prefab ��
using UnityEngine;
using UnityEngine.EventSystems;                  // �����
using UnityEngine.XR.Interaction.Toolkit;       // XR ����
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]            // ��Ҫ��ײ�����ڵ�������߼��
[RequireComponent(typeof(XRBaseInteractable))]  // ��֤�� XRBaseInteractable��ץȡ��ѡ���ã�
public class ShowInfoOnClick : MonoBehaviour,
                               IPointerClickHandler              // ����������
{
    /* ---------------- ���� Inspector �и�ֵ ---------------- */

    [Header("InfoPanel Ԥ���� (World-Space Canvas)")]
    public InfoPanelUI infoPanelPrefab;          // ��� World-Space ���Ԥ����

    [Header("����������ı���ƫ����")]
    public Vector3 localOffset = new Vector3(0.6f, 0.2f, 0f);

    [Header("��峯��΢�� (ŷ����)")]
    public Vector3 rotationOffset = Vector3.zero;

    /* ---------------- ˽���ֶ� ---------------- */

    private InfoPanelUI currentPanel;            // ����ʱʵ��
    private XRBaseInteractable interactable;     // XR ��� (Select / Activate �¼�)

    /* ========== ��ʼ�� / ����ʼ�� ========== */

    void Awake()
    {
        // ��ȡ XRBaseInteractable������ RequireComponent ��֤���ڣ�
        interactable = GetComponent<XRBaseInteractable>();

        // XR �ֱ� / ���⣺Select(����) �� Activate(Trigger) ����
        interactable.selectEntered.AddListener(OnXRSelect);
        // �����ĳ� activated �¼������� interactable.activated
    }

    void OnDestroy()
    {
        if (interactable)
            interactable.selectEntered.RemoveListener(OnXRSelect);
    }

    /* ========== ���棺��������� ========== */
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            TogglePanel();
        }
    }

    /* ========== XR���ֱ�/����ѡ�� ========== */
    private void OnXRSelect(SelectEnterEventArgs args) => TogglePanel();

    /* ========== ���ģ��� / �� ��� ========== */
    private void TogglePanel()
    {
        if (currentPanel)
        {
            Destroy(currentPanel.gameObject);
            currentPanel = null;
        }
        else
        {
            // ��һ�� World-Space Canvas ��Ϊ�����壻û�оͷų�����
            var canvas = FindAnyObjectByType<Canvas>();
            string objName = gameObject.name;

            currentPanel = Instantiate(
                infoPanelPrefab,
                transform.position + transform.TransformVector(localOffset),
                transform.rotation * Quaternion.Euler(rotationOffset),
                canvas ? canvas.transform : null
            );

            currentPanel.SetInfo(objName);   // �Զ��壺�� InfoPanelUI ����ʾ����
        }
    }

    /* ========== ÿ֡��������λ�� / ���� ========== */
    private void LateUpdate()
    {
        if (!currentPanel) return;

        // 1. λ�� ���� �����ƶ�
        currentPanel.transform.position =
            transform.position + transform.TransformVector(localOffset);

        // 2. ���� ���� ������������ת (�ɸ���ŷ��ƫ��)
        currentPanel.transform.rotation =
            transform.rotation * Quaternion.Euler(rotationOffset);
    }
}
