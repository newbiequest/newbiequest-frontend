using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string baseUrl = "http://newque.mirim-it-show.site:8080";

    [Header("Canvas")]
    [SerializeField] private GameObject gameStartCanvas;
    [SerializeField] private GameObject signUpCanvas;
    [SerializeField] private GameObject startScenePanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signPanel;
    [SerializeField] private GameObject gameCanvasRoot;

    [Header("Login Inputs")]
    [SerializeField] private TMP_InputField loginNicknameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;

    [Header("SignUp Inputs")]
    [SerializeField] private TMP_InputField signUpNicknameInput;
    [SerializeField] private TMP_InputField signUpPasswordInput;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button goLoginButton;
    [SerializeField] private Button goSignUpButton;
    [SerializeField] private Button RetrunHomeButton;


    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Login Error")]
    [SerializeField] private TextMeshProUGUI loginSignUperrorText;

    private bool requestRunning;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(ShowSignUpPanel);
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (signUpButton != null) signUpButton.onClick.AddListener(OnSignUpClicked);
        if (goLoginButton != null) goLoginButton.onClick.AddListener(ShowLoginPanel);
        if (goSignUpButton != null) goSignUpButton.onClick.AddListener(ShowSignUpPanel);
        if (RetrunHomeButton != null) RetrunHomeButton.onClick.AddListener(ShowStartScene);

        if (gameStartCanvas != null) gameStartCanvas.SetActive(true);
        if (signUpCanvas != null) signUpCanvas.SetActive(false);
        if (startScenePanel != null) startScenePanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signPanel != null) signPanel.SetActive(false);
        if (gameCanvasRoot != null) gameCanvasRoot.SetActive(false);
        ClearLoginError();

        FreezeGame(true);
    }

    private void OnLoginClicked()
    {
        if (requestRunning) return;
        StartCoroutine(AuthRequest("/auth/login", false, loginNicknameInput, loginPasswordInput));
    }

    private void OnSignUpClicked()
    {
        if (requestRunning) return;
        StartCoroutine(AuthRequest("/auth/signup", true, signUpNicknameInput, signUpPasswordInput));
    }

    public void ShowLoginPanel()
    {
        ClearAuthInputs();
        if (gameStartCanvas != null) gameStartCanvas.SetActive(true);
        if (signUpCanvas != null) signUpCanvas.SetActive(true);
        if (startScenePanel != null) startScenePanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(true);
        if (signPanel != null) signPanel.SetActive(false);
        FreezeGame(true);
        ClearLoginError();
        SetStatus("");
    }

    public void ShowStartScenePanel()
    {
        ShowStartScene();
    }

    public void ShowSignUpPanel()
    {
        ClearAuthInputs();
        if (gameStartCanvas != null) gameStartCanvas.SetActive(true);
        if (signUpCanvas != null) signUpCanvas.SetActive(true);
        if (startScenePanel != null) startScenePanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signPanel != null) signPanel.SetActive(true);
        FreezeGame(true);
        ClearLoginError();
        SetStatus("");
    }

    public void ShowStartScene()
    {
        EnsureCanvasReferences();
        CloseRankingPanels();
        ClearAuthInputs();

        if (gameStartCanvas != null) gameStartCanvas.SetActive(true);
        if (signUpCanvas != null) signUpCanvas.SetActive(false);
        if (startScenePanel != null) startScenePanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signPanel != null) signPanel.SetActive(false);
        if (gameCanvasRoot != null) gameCanvasRoot.SetActive(false);

        FreezeGame(true);
        ClearLoginError();
        SetStatus("");
    }

    private void EnsureCanvasReferences()
    {
        if (gameStartCanvas == null) gameStartCanvas = FindSceneObject("GameStartCanvas");
        if (signUpCanvas == null) signUpCanvas = FindSceneObject("SignUpcanvas");
        if (startScenePanel == null) startScenePanel = FindSceneObject("StartScenePanel(Img)");
        if (loginPanel == null) loginPanel = FindSceneObject("LoginPanel");
        if (signPanel == null) signPanel = FindSceneObject("SignPanel");
        if (gameCanvasRoot == null) gameCanvasRoot = FindSceneObject("GameCanvasRoot");
    }

    private void CloseRankingPanels()
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.gameObject.scene.IsValid() && transform.name == "RankingPanel")
                transform.gameObject.SetActive(false);
        }
    }

    private GameObject FindSceneObject(string objectName)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.gameObject.scene.IsValid() && transform.name == objectName)
                return transform.gameObject;
        }

        return null;
    }

    private IEnumerator AuthRequest(string path, bool signUp, TMP_InputField nicknameField, TMP_InputField passwordField)
    {
        string nickname = nicknameField != null ? nicknameField.text.Trim() : "";
        string password = passwordField != null ? passwordField.text : "";

        if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(password))
        {
            ShowLoginError("\uB2C9\uB124\uC784\uACFC \uBE44\uBC00\uBC88\uD638\uB97C \uC785\uB825\uD574\uC8FC\uC138\uC694.");
            yield break;
        }

        if (nickname.Length > 10)
        {
            ShowLoginError("\uC544\uC774\uB514\uB294 10\uAE00\uC790 \uC774\uD558\uB85C \uC785\uB825\uD574\uC8FC\uC138\uC694.");
            yield break;
        }

        if (password.Length < 5 || password.Length > 20)
        {
            ShowLoginError("\uBE44\uBC00\uBC88\uD638\uB294 5\uAE00\uC790 \uC774\uC0C1 20\uAE00\uC790 \uC774\uD558\uB85C \uC785\uB825\uD574\uC8FC\uC138\uC694.");
            yield break;
        }

        requestRunning = true;
        SetButtons(false);
        SetStatus(signUp ? "?뚯썝媛??以?.." : "濡쒓렇??以?..");

        string json = signUp
            ? $"{{\"nickname\":\"{EscapeJson(nickname)}\",\"password\":\"{EscapeJson(password)}\",\"consentToTerms\":true}}"
            : $"{{\"nickname\":\"{EscapeJson(nickname)}\",\"password\":\"{EscapeJson(password)}\"}}";

        byte[] body = Encoding.UTF8.GetBytes(json);
        using var req = new UnityWebRequest(baseUrl + path, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        requestRunning = false;
        SetButtons(true);

        if (req.result != UnityWebRequest.Result.Success)
        {
            /*
            string actionName = signUp ? "?뚯썝媛?? : "濡쒓렇??;
            string message = req.responseCode >= 500
                ? $"{actionName}???ㅽ뙣?덉뒿?덈떎.\n?쒕쾭 ?ㅻ쪟媛 諛쒖깮?덉뒿?덈떎."
                : $"{actionName}???ㅽ뙣?덉뒿?덈떎.\n?낅젰 ?뺣낫瑜??뺤씤?댁＜?몄슂.";
            ShowLoginError(message);
            SetStatus($"{(signUp ? "?뚯썝媛?? : "濡쒓렇??)} ?ㅽ뙣: {req.error}");
            */
            ShowLoginError(req.responseCode >= 500
                ? (signUp ? "\uD68C\uC6D0\uAC00\uC785" : "\uB85C\uADF8\uC778") + "\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4.\n\uC11C\uBC84 \uC624\uB958\uAC00 \uBC1C\uC0DD\uD588\uC2B5\uB2C8\uB2E4."
                : (signUp ? "\uD68C\uC6D0\uAC00\uC785" : "\uB85C\uADF8\uC778") + "\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4.\n\uC785\uB825 \uC815\uBCF4\uB97C \uD655\uC778\uD574\uC8FC\uC138\uC694.");
            yield break;
        }

        AuthResponse response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
        GameSession.AccessToken = response.accessToken;
        GameSession.Nickname = response.nickname;
        PlayerPrefs.SetString("accessToken", response.accessToken.ToString());
        PlayerPrefs.SetString("nickname", response.nickname);
        PlayerPrefs.Save();

        SetStatus($"{GameSession.Nickname} 濡쒓렇???깃났");

        if (gameStartCanvas != null) gameStartCanvas.SetActive(false);
        if (signUpCanvas != null) signUpCanvas.SetActive(false);
        if (startScenePanel != null) startScenePanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signPanel != null) signPanel.SetActive(false);
        if (gameCanvasRoot != null) gameCanvasRoot.SetActive(true);
        ClearAuthInputs();

        FreezeGame(false);
        MissionManager.Instance.BeginGameAfterLogin();
    }

    private void SetButtons(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (signUpButton != null) signUpButton.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log(message);
    }

    private void ShowLoginError(string message)
    {
        SetStatus(message);
        if (loginSignUperrorText != null) loginSignUperrorText.text = message;
    }

    private void ClearLoginError()
    {
        if (loginSignUperrorText != null) loginSignUperrorText.text = "";
    }

    private void ClearAuthInputs()
    {
        if (loginNicknameInput != null) loginNicknameInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (signUpNicknameInput != null) signUpNicknameInput.text = "";
        if (signUpPasswordInput != null) signUpPasswordInput.text = "";
    }

    private void FreezeGame(bool freeze)
    {
        Time.timeScale = freeze ? 0f : 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [Serializable]
    private class AuthResponse
    {
        public long accessToken;
        public string nickname;
    }
}
