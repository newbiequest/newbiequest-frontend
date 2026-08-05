using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MapUI : MonoBehaviour
{
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject mapToggleButton;

    void Start()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
        if (mapToggleButton != null) mapToggleButton.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (MissionManager.Instance == null
                || !MissionManager.Instance.IsGameStarted
                || MissionManager.Instance.IsGameEnded)
                return;

            if (IsTypingInInputField())
                return;

            if (MissionManager.Instance != null && MissionManager.Instance.IsMissionPanelOpen)
                return;

            if (mapPanel != null && mapPanel.activeSelf)
                CloseMap();
            else
                OpenMap();
        }
    }

    private void LateUpdate()
    {
        if (mapPanel != null && mapPanel.activeSelf)
            UnlockCursorForMap();
    }

    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
        if (tmpInput != null)
            return selected.activeInHierarchy && tmpInput.isFocused && !IsChatInput(selected);

        UnityEngine.UI.InputField input = selected.GetComponent<UnityEngine.UI.InputField>();
        return input != null && selected.activeInHierarchy && input.isFocused && !IsChatInput(selected);
    }

    private bool IsChatInput(GameObject selected)
    {
        Transform current = selected.transform;
        while (current != null)
        {
            if (current.name == "ChatPanel")
                return true;

            current = current.parent;
        }

        return false;
    }

    public void OpenMap()
    {
        if (mapPanel != null) mapPanel.SetActive(true);
        if (mapToggleButton != null) mapToggleButton.SetActive(false);

        FreezePlayer(true);
        UnlockCursorForMap();

    }

    public void CloseMap()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
        if (mapToggleButton != null) mapToggleButton.SetActive(true);

        FreezePlayer(false);

    }

    private void FreezePlayer(bool freeze)
    {
        Time.timeScale = freeze ? 0f : 1f;
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }

    private void UnlockCursorForMap()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
