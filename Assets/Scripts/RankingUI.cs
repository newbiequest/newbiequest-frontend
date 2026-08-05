using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class ScoreResponse
{
    public string nickname;
    public int score;
}

[Serializable]
public class ScoreResponseArray
{
    public ScoreResponse[] items;
}

public class RankingUI : MonoBehaviour
{

    [Header("UI References")]
    public GameObject rankingPanel;
    public Transform rankingContent;
    public GameObject rankingItemPrefab;
    [SerializeField] private Button gameRankingButton;
    [SerializeField] private Button goRankingButton;
    [SerializeField] private Button returnHomeButton;

    void Start()
    {
        EnsureRankingPanelHasActiveCanvas();
        if (rankingPanel != null) rankingPanel.SetActive(false);
        BindRankingButtons();
    }

    private void EnsureRankingPanelHasActiveCanvas()
    {
        if (rankingPanel == null) return;

        GameObject rankingCanvasObject = GameObject.Find("RankingCanvas");
        if (rankingCanvasObject == null)
        {
            rankingCanvasObject = new GameObject("RankingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = rankingCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = rankingCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        rankingCanvasObject.SetActive(true);
        rankingPanel.transform.SetParent(rankingCanvasObject.transform, false);
    }

    private void BindRankingButtons()
    {
        if (gameRankingButton == null) gameRankingButton = FindButtonByName("GameRankingButton");
        if (goRankingButton == null) goRankingButton = FindButtonByName("GoRankingButton");
        if (returnHomeButton == null && rankingPanel != null)
            returnHomeButton = rankingPanel.GetComponentInChildren<Button>(true);

        if (gameRankingButton != null)
        {
            gameRankingButton.onClick.RemoveListener(OpenRanking);
            gameRankingButton.onClick.AddListener(OpenRanking);
        }

        if (goRankingButton != null)
        {
            goRankingButton.onClick.RemoveListener(OpenRanking);
            goRankingButton.onClick.AddListener(OpenRanking);
        }

        if (returnHomeButton != null)
        {
            returnHomeButton.onClick.RemoveListener(ReturnToStartScene);
            returnHomeButton.onClick.AddListener(ReturnToStartScene);
        }
    }

    private Button FindButtonByName(string buttonName)
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (button.name == buttonName && button.gameObject.scene.IsValid())
                return button;
        }

        return null;
    }

    public void OpenRanking()
    {
        if (rankingPanel == null)
        {
            Debug.LogError("Ranking Panel is not connected.");
            return;
        }

        rankingPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(LoadRanking());
    }

    public void CloseRanking()
    {
        if (rankingPanel != null) rankingPanel.SetActive(false);

        if (MissionManager.Instance != null && MissionManager.Instance.IsGameStarted && !MissionManager.Instance.IsGameEnded)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ReturnToStartScene()
    {
        if (rankingPanel != null) rankingPanel.SetActive(false);
        CloseFinalResultPanels();

        LoginUI loginUI = FindFirstObjectByType<LoginUI>(FindObjectsInactive.Include);
        if (loginUI != null)
            loginUI.ShowStartScenePanel();
        else
            Debug.LogError("LoginUI not found. Cannot return to start scene.");

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseFinalResultPanels()
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!transform.gameObject.scene.IsValid()) continue;

            if (transform.name == "FinalresultPanel" || transform.name == "FinalResultPanel")
                transform.gameObject.SetActive(false);
        }
    }

    IEnumerator LoadRanking()
    {
        foreach (Transform child in rankingContent)
        {
            Destroy(child.gameObject);
        }

        using (UnityWebRequest req = UnityWebRequest.Get("http://newque.mirim-it-show.site:8080/score"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                ScoreResponseArray responseArray = JsonUtility.FromJson<ScoreResponseArray>("{\"items\":" + json + "}");

                Array.Sort(responseArray.items, (a, b) => b.score.CompareTo(a.score));

                for (int i = 0; i < responseArray.items.Length; i++)
                {
                    ScoreResponse item = responseArray.items[i];
                    GameObject obj = Instantiate(rankingItemPrefab, rankingContent);
                    TMP_Text text = obj.GetComponent<TMP_Text>();
                    text.text = $"{i + 1}.  {item.nickname}  {item.score}";
                }
            }
            else
            {
                Debug.LogError("랭킹 불러오기 실패: " + req.error);
            }
        }
    }
}
