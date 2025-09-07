using UnityEngine;
using UnityEngine.InputSystem;                 
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
[RequireComponent(typeof(Renderer))]
public class ShowInfoOnSelect : MonoBehaviour
{
    [Header("UI settings")]
    public GameObject infoPanelPrefab;
    public Vector3 panelOffset = new Vector3(0.3f, 0f, 0f);

    private GameObject _currentInfoPanel;
    private XRBaseInteractable _interactable;
    private bool _isSelected;
    private InputAction _toggleAction;

    void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();

        // Quest right hand A: OpenXR compatible with Oculus
        _toggleAction = new InputAction("ToggleInfo");
        _toggleAction.AddBinding("<XRController>{RightHand}/primaryButton");   // OpenXR A
        _toggleAction.AddBinding("<OculusTouchController>{RightHand}/button1"); // Oculus A
    }

    void OnEnable()
    {
        // Only record whether it is caught/selected
        _interactable.selectEntered.AddListener(_ => _isSelected = true);
        _interactable.selectExited.AddListener(_ => _isSelected = false);

        _toggleAction.Enable();
        _toggleAction.performed += OnTogglePerformed;
    }

    void OnDisable()
    {
        _interactable.selectEntered.RemoveAllListeners();
        _interactable.selectExited.RemoveAllListeners();

        _toggleAction.performed -= OnTogglePerformed;
        _toggleAction.Disable();
    }

    void Update()
    {
        // panel face to camera
        if (_currentInfoPanel != null && Camera.main != null)
        {
            _currentInfoPanel.transform.LookAt(Camera.main.transform);
            _currentInfoPanel.transform.Rotate(0f, 180f, 0f);
        }

        if (_isSelected && Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            TogglePanel();
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext _)
    {
        if (_isSelected) TogglePanel();
    }
    private void TogglePanel()
    {
        if (_currentInfoPanel == null) ShowPanel();
        else HidePanel();
    }

    private void ShowPanel()
    {
        if (infoPanelPrefab == null) return;

        _currentInfoPanel = Instantiate(infoPanelPrefab);
        _currentInfoPanel.transform.SetParent(this.transform);
        _currentInfoPanel.transform.localPosition = panelOffset;
        _currentInfoPanel.transform.localRotation = Quaternion.identity;

        var panelCanvas = _currentInfoPanel.GetComponent<Canvas>();
        if (panelCanvas != null) panelCanvas.worldCamera = Camera.main;

        var panelData = _currentInfoPanel.GetComponentInChildren<InfoPanelDataReceiver>();
        if (panelData != null)
        {
            panelData.SetTargetObject(GetComponent<Renderer>());

            string objectName = gameObject.name;
            Vector3 objectScale = transform.localScale;

            string objectId = "ID not found";
            var sync = GetComponent<SyncTransformOnChange>();
            if (sync != null) objectId = sync.objectId;

            string prompt = "Prompt not available";
            if (SpawnMenu.Instance != null)
                prompt = SpawnMenu.Instance.GetPromptForObjectId(objectId);

            panelData.UpdateInfo(objectName, objectScale, prompt);
        }
        else
        {
            Debug.LogError("InfoPanelDataReceiver not found, check Prefab。", infoPanelPrefab);
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
