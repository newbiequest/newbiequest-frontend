using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    [Header("Mission Box")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private TextMeshProUGUI missionText;

    [Header("Top HUD")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Final Result")]
    [SerializeField] private GameObject finalResultPanel;
    [SerializeField] private TextMeshProUGUI finalResultText;

    private string lastMissionId = "";
    private bool finalShown;

    private void Awake()
    {
        EnsureHudObjects();
        ConfigureHudLayout();
    }

    private void Start()
    {
        if (missionPanel != null) missionPanel.SetActive(false);
        if (finalResultPanel != null) finalResultPanel.SetActive(false);
    }

    private void Update()
    {
        MissionManager manager = MissionManager.Instance;
        if (manager == null) return;

        UpdateHud(manager);

        if (manager.IsGameEnded)
        {
            ShowFinalResult(manager);
            if (missionPanel != null) missionPanel.SetActive(false);
            return;
        }

        MissionData mission = manager.CurrentMission;
        if (!manager.IsGameStarted || mission == null)
        {
            if (missionPanel != null) missionPanel.SetActive(false);
            return;
        }

        if (missionPanel != null) missionPanel.SetActive(true);

        if (mission.missionId != lastMissionId)
        {
            lastMissionId = mission.missionId;
            if (missionText != null) missionText.text = mission.message;
        }
    }

    private void EnsureHudObjects()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null) return;

        if (timerText == null)
        {
            GameObject timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            timerObj.transform.SetParent(root, false);
            timerText = timerObj.GetComponent<TextMeshProUGUI>();
        }

        if (missionPanel == null)
        {
            missionPanel = new GameObject("MissionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            missionPanel.transform.SetParent(root, false);
        }

        if (missionText == null)
        {
            GameObject textObj = new GameObject("MissionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(missionPanel.transform, false);
            missionText = textObj.GetComponent<TextMeshProUGUI>();
        }
    }

    private void ConfigureHudLayout()
    {
        if (timerText != null)
        {
            RectTransform timerRect = timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0f, -18f);
            timerRect.sizeDelta = new Vector2(220f, 54f);

            timerText.alignment = TextAlignmentOptions.Center;
            timerText.fontSize = 38f;
            timerText.fontStyle = FontStyles.Bold;
            timerText.color = Color.white;
            timerText.raycastTarget = false;
        }

        if (missionPanel != null)
        {
            Image bg = missionPanel.GetComponent<Image>();
            if (bg == null) bg = missionPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);
            bg.raycastTarget = false;
        }

        if (missionText != null)
        {
            missionText.alignment = TextAlignmentOptions.MidlineLeft;
            missionText.fontSize = 22f;
            missionText.color = Color.white;
            missionText.textWrappingMode = TextWrappingModes.Normal;
            missionText.overflowMode = TextOverflowModes.Ellipsis;
            missionText.raycastTarget = false;
        }
    }

    private void UpdateHud(MissionManager manager)
    {
        if (timerText != null)
        {
            float remain = manager.GetRemainingTime();
            int min = Mathf.FloorToInt(remain / 60f);
            int sec = Mathf.FloorToInt(remain % 60f);
            timerText.text = $"{min:00}:{sec:00}";
        }

        if (scoreText != null)
            scoreText.text = $"점수: {manager.TotalScore}";
    }

    private void ShowFinalResult(MissionManager manager)
    {
        if (finalShown) return;
        finalShown = true;

        if (finalResultPanel != null) finalResultPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (finalResultText == null) return;

        int score = manager.TotalScore;
        string grade = score >= 700 ? "S"
            : score >= 500 ? "A"
            : score >= 300 ? "B"
            : score >= 150 ? "C"
            : "D";

        finalResultText.text = $"최종 점수: {score}점\n등급: {grade}\n오늘 업무 수행이 종료되었습니다.";
    }
}
