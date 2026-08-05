using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Server")]
    [SerializeField] private string baseUrl = "http://newque.mirim-it-show.site:8080";

    [Header("Mission Panels")]
    [SerializeField] private GameObject printPanel;
    [SerializeField] private GameObject coffeePanel;
    [SerializeField] private GameObject computerPanel;
    [SerializeField] private GameObject docStoragePanel;
    [SerializeField] private GameObject parcelPanel;
    [SerializeField] private GameObject bigMtgPanel;
    [SerializeField] private GameObject smallMtgPanel;

    [Header("HUD")]
    [SerializeField] private GameObject gameHudCanvas;

    [Header("Player Reset")]
    [SerializeField] private Transform playerTransform;

    public MissionData CurrentMission { get; private set; }
    public int TotalScore { get; private set; }
    public bool IsGameStarted { get; private set; }
    public bool IsGameEnded { get; private set; }
    public bool IsMissionPanelOpen { get; private set; }

    private const float TotalGameTime = 300f;
    private float elapsedTime;
    private bool finalScoreSubmitted;
    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;
    private bool hasPlayerStartTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CachePlayerStartTransform();
        CloseAllPanels();
    }

    private void Update()
    {
        if (!IsGameStarted || IsGameEnded) return;

        elapsedTime += Time.unscaledDeltaTime;
        if (elapsedTime >= TotalGameTime)
            EndGame();
    }

    public void BeginGameAfterLogin()
    {
        CloseAllPanels();
        CurrentMission = null;
        TotalScore = 0;
        elapsedTime = 0f;
        finalScoreSubmitted = false;
        IsGameStarted = true;
        IsGameEnded = false;
        IsMissionPanelOpen = false;
        ResetPlayerTransform();
        SetGameHudVisible(true);
        FreezePlayer(false);
        StartCoroutine(FetchMission());
    }

    public IEnumerator FetchMission()
    {
        if (!IsGameStarted || IsGameEnded) yield break;

        string url = $"{baseUrl}/mission/{GameSession.AccessToken}";
        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            CurrentMission = JsonUtility.FromJson<MissionData>(req.downloadHandler.text);
            CurrentMission.givenAt = DateTime.Now;
            CurrentMission.missionId = Guid.NewGuid().ToString();
            Debug.Log($"미션 수신: {CurrentMission.taskType} / {CurrentMission.message}");
        }
        else
        {
            Debug.LogError($"미션 요청 실패: {req.error}\n{req.downloadHandler.text}");
        }
    }

    public IEnumerator CompleteMission(double elapsedSeconds, bool isNpcMission = false)
    {
        if (CurrentMission == null || IsGameEnded) yield break;

        string url = $"{baseUrl}/mission/complete/{GameSession.AccessToken}/{CurrentMission.taskType}";
        string json = "{\"completed\":true}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("미션 완료 전송 성공");
            if (!IsGameEnded)
                yield return StartCoroutine(FetchMission());
        }
        else
        {
            Debug.LogError("미션 완료 전송 실패: " + req.error);
        }
    }

    public void OpenMission(string missionName)
    {
        GetPanel(missionName)?.SetActive(true);
        IsMissionPanelOpen = true;
        SetGameHudVisible(false);
        FreezePlayer(true);
    }

    public void CloseMission(string missionName)
    {
        GetPanel(missionName)?.SetActive(false);
        IsMissionPanelOpen = false;
        SetGameHudVisible(true);
        FreezePlayer(false);
    }

    public void AddScore(int score)
    {
        TotalScore += score;
        Debug.Log($"+{score} / 누적: {TotalScore}");
    }

    public void FreezeForMission(bool freeze)
    {
        IsMissionPanelOpen = freeze;
        SetGameHudVisible(!freeze);
        FreezePlayer(freeze);
    }

    private GameObject GetPanel(string name) => name switch
    {
        "PRINT" => printPanel,
        "COFFEE" => coffeePanel,
        "COMPUTER" => computerPanel,
        "DOC_STORAGE" => docStoragePanel,
        "PARCEL" => parcelPanel,
        "BIG_MTG" => bigMtgPanel,
        "SMALL_MTG" => smallMtgPanel,
        _ => null
    };

    private void CloseAllPanels()
    {
        foreach (string name in new[] { "PRINT", "COFFEE", "COMPUTER", "DOC_STORAGE", "PARCEL", "BIG_MTG", "SMALL_MTG" })
            GetPanel(name)?.SetActive(false);
        IsMissionPanelOpen = false;
        SetGameHudVisible(true);
    }

    private void SetGameHudVisible(bool visible)
    {
        if (gameHudCanvas == null)
            gameHudCanvas = FindSceneObject("GameHUDCanvas");

        if (gameHudCanvas == null) return;

        bool shouldShow = visible && IsGameStarted && !IsGameEnded;
        gameHudCanvas.SetActive(shouldShow);
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

    private void CachePlayerStartTransform()
    {
        if (playerTransform == null)
        {
            Player player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null) return;

        playerStartPosition = playerTransform.position;
        playerStartRotation = playerTransform.rotation;
        hasPlayerStartTransform = true;
    }

    private void ResetPlayerTransform()
    {
        if (!hasPlayerStartTransform)
            CachePlayerStartTransform();

        if (playerTransform == null || !hasPlayerStartTransform) return;

        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerTransform.SetPositionAndRotation(playerStartPosition, playerStartRotation);

        Player player = playerTransform.GetComponent<Player>();
        if (player != null)
            player.ResetMovement();
    }

    private void FreezePlayer(bool freeze)
    {
        Time.timeScale = freeze ? 0f : 1f;
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }

    private void EndGame()
    {
        IsGameEnded = true;
        CurrentMission = null;
        CloseAllPanels();
        SetGameHudVisibleForFinalResult();
        FreezePlayer(true);
        StartCoroutine(SubmitFinalScore());
        Debug.Log($"게임 종료 - 최종 점수: {TotalScore}");
    }

    private void SetGameHudVisibleForFinalResult()
    {
        if (gameHudCanvas == null)
            gameHudCanvas = FindSceneObject("GameHUDCanvas");

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(true);
    }

    private IEnumerator SubmitFinalScore()
    {
        if (finalScoreSubmitted) yield break;
        finalScoreSubmitted = true;

        string url = $"{baseUrl}/score/{GameSession.AccessToken}";
        string json = "{\"score\":" + TotalScore + "}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("Final score submitted: " + TotalScore);
        else
            Debug.LogError("Final score submit failed: " + req.error + "\n" + req.downloadHandler.text);
    }

    public float GetRemainingTime() => Mathf.Max(0f, TotalGameTime - elapsedTime);
    public float GetElapsedTime() => elapsedTime;
    public float GetTotalGameTime() => TotalGameTime;
}
