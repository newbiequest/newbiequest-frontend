using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using NativeWebSocket;

[Serializable]
public class ChatSendRequest
{
    public long accessToken;
    public string message;
}

[Serializable]
public class ChatReceiveResponse
{
    public string nickname;
    public string createAt;
    public string message;
}

[Serializable]
public class ChatReceiveResponseArray
{
    public ChatReceiveResponse[] items;
}

public class ChatUI : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string baseUrl = "http://newque.mirim-it-show.site:8080";

    [Header("WebSocket")]
    public long accessToken;
    private WebSocket websocket;

    [Header("UI References")]
    public GameObject chatPanel;
    public ScrollRect scrollRect;
    public Transform chatContent;
    public TMP_InputField inputField;
    public GameObject chatMessagePrefab;
    public TMP_FontAsset chatMessageFont;

    [Header("Settings")]
    public int maxMessages = 50;
    public float hideDelay = 3f;
    public float fadeDuration = 0.35f;

    private bool isChatOpen;
    private float hideTimer;
    private CanvasGroup chatCanvasGroup;
    private Coroutine fadeCoroutine;
    private readonly List<GameObject> messageObjects = new List<GameObject>();
    private string pendingLocalMessage = "";

    private async void Start()
    {
        EnsureCanvasGroup();
        if (chatPanel != null) chatPanel.SetActive(false);
        if (inputField != null) inputField.gameObject.SetActive(false);

        if (PlayerPrefs.HasKey("accessToken"))
        {
            accessToken = long.Parse(PlayerPrefs.GetString("accessToken"));
            GameSession.AccessToken = accessToken;
        }

        if (PlayerPrefs.HasKey("nickname"))
            GameSession.Nickname = PlayerPrefs.GetString("nickname");

        await LoadPreviousChats();
        await ConnectWebSocket();
    }

    private async System.Threading.Tasks.Task ConnectWebSocket()
    {
        websocket = new WebSocket("ws://newque.mirim-it-show.site:8080/ws/chat");

        websocket.OnOpen += () => Debug.Log("Chat connected");

        websocket.OnMessage += (bytes) =>
        {
            string json = Encoding.UTF8.GetString(bytes);
            ChatReceiveResponse res = JsonUtility.FromJson<ChatReceiveResponse>(json);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (res == null) return;

                bool isMyEcho = res.message == pendingLocalMessage
                    && res.nickname == GameSession.Nickname;

                if (isMyEcho)
                {
                    pendingLocalMessage = "";
                    return;
                }

                AddMessage(res.nickname, res.message);
            });
        };

        websocket.OnError += (e) => Debug.LogError("WebSocket error: " + e);
        websocket.OnClose += (e) => Debug.Log("WebSocket closed");

        await websocket.Connect();
    }

    private async System.Threading.Tasks.Task LoadPreviousChats()
    {
        using (UnityWebRequest req = UnityWebRequest.Get($"{baseUrl}/chat"))
        {
            var operation = req.SendWebRequest();
            while (!operation.isDone) await System.Threading.Tasks.Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Previous chat load failed: " + req.error);
                return;
            }

            string json = req.downloadHandler.text;
            ChatReceiveResponseArray responseArray = JsonUtility.FromJson<ChatReceiveResponseArray>("{\"items\":" + json + "}");
            if (responseArray == null || responseArray.items == null) return;

            foreach (ChatReceiveResponse res in responseArray.items)
                AddMessage(res.nickname, res.message);

            Debug.Log("Previous chat loaded");
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
            websocket.DispatchMessageQueue();
#endif

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame && !isChatOpen)
        {
            if (MissionManager.Instance == null
                || !MissionManager.Instance.IsGameStarted
                || MissionManager.Instance.IsGameEnded)
                return;

            if (IsTypingInInputField())
                return;

            if (MissionManager.Instance != null && MissionManager.Instance.IsMissionPanelOpen)
                return;

            OpenChat();
            return;
        }

        if (!isChatOpen)
        {
            AutoHidePreview();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (!string.IsNullOrWhiteSpace(inputField.text))
                SendChat();
            else
                inputField.ActivateInputField();

            return;
        }

        if (inputField != null && inputField.gameObject.activeSelf && !inputField.isFocused)
            inputField.ActivateInputField();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseChat();
            return;
        }

        if (Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame
            && !IsPointerInsideChatPanel())
        {
            CloseChat();
            return;
        }

    }

    private void AutoHidePreview()
    {
        if (chatPanel == null || !chatPanel.activeSelf) return;

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
        {
            chatPanel.SetActive(false);
            ApplyCursorAfterClose();
        }
    }

    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
        if (tmpInput != null) return selected.activeInHierarchy && tmpInput.isFocused;

        InputField input = selected.GetComponent<InputField>();
        return input != null && selected.activeInHierarchy && input.isFocused;
    }

    private void OpenChat()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        EnsureCanvasGroup();
        isChatOpen = true;
        if (chatPanel != null) chatPanel.SetActive(true);
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1f;
            chatCanvasGroup.interactable = true;
            chatCanvasGroup.blocksRaycasts = true;
        }

        if (inputField != null)
        {
            inputField.gameObject.SetActive(true);
            inputField.text = "";
            inputField.ActivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }

        hideTimer = hideDelay;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseChat()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        isChatOpen = false;

        if (inputField != null)
        {
            inputField.text = "";
            ClearInputFocus();
            inputField.gameObject.SetActive(false);
        }

        if (chatPanel != null) chatPanel.SetActive(false);
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1f;
            chatCanvasGroup.interactable = true;
            chatCanvasGroup.blocksRaycasts = true;
        }

        hideTimer = hideDelay;
        ApplyCursorAfterClose();
    }

    private IEnumerator FadeOutAndClose()
    {
        EnsureCanvasGroup();

        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.interactable = false;
            chatCanvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startAlpha = chatCanvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                chatCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            chatCanvasGroup.alpha = 0f;
        }

        isChatOpen = false;

        if (inputField != null)
        {
            inputField.text = "";
            ClearInputFocus();
            inputField.gameObject.SetActive(false);
        }

        if (chatPanel != null) chatPanel.SetActive(false);
        if (chatCanvasGroup != null)
        {
            chatCanvasGroup.alpha = 1f;
            chatCanvasGroup.interactable = true;
            chatCanvasGroup.blocksRaycasts = true;
        }

        hideTimer = hideDelay;
        fadeCoroutine = null;
        ApplyCursorAfterClose();
    }

    private void EnsureCanvasGroup()
    {
        if (chatPanel == null) return;

        chatCanvasGroup = chatPanel.GetComponent<CanvasGroup>();
        if (chatCanvasGroup == null)
            chatCanvasGroup = chatPanel.AddComponent<CanvasGroup>();
    }

    private void ApplyCursorAfterClose()
    {

        if (MissionManager.Instance != null
            && MissionManager.Instance.IsGameStarted
            && !MissionManager.Instance.IsGameEnded)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private bool IsPointerInsideChatPanel()
    {
        if (chatPanel == null) return false;

        RectTransform rect = chatPanel.GetComponent<RectTransform>();
        if (rect == null || Mouse.current == null) return false;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, null);
    }

    private async void SendChat()
    {
        string msg = inputField.text.Trim();
        if (string.IsNullOrWhiteSpace(msg)) return;

        accessToken = GameSession.AccessToken;
        string nickname = !string.IsNullOrWhiteSpace(GameSession.Nickname) ? GameSession.Nickname : "Me";

        AddMessage(nickname, msg);
        pendingLocalMessage = msg;

        inputField.text = "";
        FinishInputAndStartHideTimer();

        ChatSendRequest request = new ChatSendRequest
        {
            accessToken = accessToken,
            message = msg
        };

        string json = JsonUtility.ToJson(request);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest($"{baseUrl}/chat", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var operation = req.SendWebRequest();
            while (!operation.isDone) await System.Threading.Tasks.Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError("Chat send failed: " + req.error + "\n" + req.downloadHandler.text);
        }
    }

    private void FinishInputAndStartHideTimer()
    {
        isChatOpen = false;
        hideTimer = hideDelay;

        if (inputField != null)
        {
            inputField.DeactivateInputField();
            ClearInputFocus();
            inputField.gameObject.SetActive(false);
        }

        if (chatPanel != null) chatPanel.SetActive(true);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        ApplyCursorAfterClose();
    }

    private void AddMessage(string nickname, string message)
    {
        if (chatMessagePrefab == null || chatContent == null) return;

        if (messageObjects.Count >= maxMessages)
        {
            Destroy(messageObjects[0]);
            messageObjects.RemoveAt(0);
        }

        GameObject msgObj = Instantiate(chatMessagePrefab, chatContent);
        TMP_Text text = msgObj.GetComponent<TMP_Text>();
        if (text == null) text = msgObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            if (chatMessageFont != null)
                text.font = chatMessageFont;

            text.text = $"{nickname}: {message}";
        }

        messageObjects.Add(msgObj);

        if (chatPanel != null) chatPanel.SetActive(true);
        hideTimer = hideDelay;

        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    private void ClearInputFocus()
    {
        if (EventSystem.current == null || inputField == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == inputField.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private async void OnDestroy()
    {
        if (websocket != null)
            await websocket.Close();
    }
}
