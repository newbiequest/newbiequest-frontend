using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigMTG : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private AreYouStart areYouStart;

    [Header("Panel")]
    [SerializeField] private GameObject mtgPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Guide Text")]
    [SerializeField] private TextMeshProUGUI mtgTitleText;
    [SerializeField] private TextMeshProUGUI mtgInfoTitleText;
    [SerializeField] private TextMeshProUGUI mtgTimeText;
    [SerializeField] private TextMeshProUGUI mtgEmpCountText;
    [SerializeField] private TextMeshProUGUI purposeMtgText;

    [Header("Input")]
    [SerializeField] private TMP_InputField startHourInput;
    [SerializeField] private TMP_InputField startMinuteInput;
    [SerializeField] private TMP_InputField purposeInput;
    [SerializeField] private TextMeshProUGUI empCountText;

    [Header("Count Buttons")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("Reserve Button")]
    [SerializeField] private Button mtgBtn;

    [Header("Result Panel")]
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private TextMeshProUGUI resultCommentText;
    [SerializeField] private Button resultExitButton;

    private const int MinHeadcount = 8;
    private const int MaxHeadcount = 9;

    private int inputHeadcount = MinHeadcount;
    private int targetHeadcount;
    private int targetStartHour;
    private int targetStartMinute;
    private float startTime;
    private bool missionActive;

    private void Awake()
    {
        AutoBindCountButtons();

        if (plusButton != null) plusButton.onClick.AddListener(OnPlus);
        if (minusButton != null) minusButton.onClick.AddListener(OnMinus);
        if (mtgBtn != null) mtgBtn.onClick.AddListener(OnReserve);
        if (resultExitButton != null) resultExitButton.onClick.AddListener(OnExit);

        if (mtgPanel != null) mtgPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.OpenPanel("BIG_MTG");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.ClosePanel();
    }

    public void TryMission()
    {
        MissionData mission = MissionManager.Instance?.CurrentMission;
        if (mission == null) return;

        targetHeadcount = Mathf.Clamp(mission.meetingHeadcount, MinHeadcount, MaxHeadcount);
        targetStartHour = Mathf.Clamp(mission.meetingStartHour, 9, 18);
        targetStartMinute = Mathf.Clamp(mission.meetingStartMinute, 0, 59);

        inputHeadcount = MinHeadcount;
        UpdateEmpCount();

        if (startHourInput != null) startHourInput.text = string.Empty;
        if (startMinuteInput != null) startMinuteInput.text = string.Empty;
        if (purposeInput != null) purposeInput.text = string.Empty;

        startTime = Time.realtimeSinceStartup;
        missionActive = true;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (mtgPanel != null) mtgPanel.SetActive(true);
        MissionManager.Instance.FreezeForMission(true);
    }

    private void OnPlus()
    {
        if (!missionActive) return;
        if (inputHeadcount < MaxHeadcount) inputHeadcount++;
        UpdateEmpCount();
    }

    private void OnMinus()
    {
        if (!missionActive) return;
        if (inputHeadcount > MinHeadcount) inputHeadcount--;
        UpdateEmpCount();
    }

    private void AutoBindCountButtons()
    {
        if (plusButton != null && minusButton != null) return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            string buttonName = button.name.ToLowerInvariant();
            string buttonText = button.GetComponentInChildren<TMP_Text>(true)?.text.Trim();

            if (plusButton == null && (buttonName.Contains("plus") || buttonName.Contains("add") || buttonText == "+"))
                plusButton = button;

            if (minusButton == null && (buttonName.Contains("minus") || buttonName.Contains("sub") || buttonText == "-"))
                minusButton = button;
        }
    }

    private void UpdateEmpCount()
    {
        if (empCountText != null) empCountText.text = inputHeadcount.ToString();
    }

    private void OnReserve()
    {
        if (!missionActive) return;

        float elapsed = Time.realtimeSinceStartup - startTime;
        bool countCorrect = inputHeadcount == targetHeadcount;
        bool startCorrect = IsStartTimeCorrect();
        bool purposeFilled = purposeInput != null && !string.IsNullOrWhiteSpace(purposeInput.text);

        int score = CalculateScore(countCorrect, startCorrect, purposeFilled, elapsed);
        MissionManager.Instance.AddScore(score);

        if (resultScoreText != null)
            resultScoreText.text = $"점수: {score}\n누적: {MissionManager.Instance.TotalScore}";
        if (resultCommentText != null)
            resultCommentText.text = GetComment(score, countCorrect, startCorrect, purposeFilled);

        missionActive = false;
        ShowResultPanel();

        StartCoroutine(MissionManager.Instance.CompleteMission(elapsed));
    }

    private void ShowResultPanel()
    {
        if (resultPanel == null) return;

        bool resultIsInsideMtgPanel = mtgPanel != null && resultPanel.transform.IsChildOf(mtgPanel.transform);
        if (!resultIsInsideMtgPanel && mtgPanel != null)
            mtgPanel.SetActive(false);

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();

        Canvas resultCanvas = resultPanel.GetComponent<Canvas>();
        if (resultCanvas == null) resultCanvas = resultPanel.AddComponent<Canvas>();
        resultCanvas.overrideSorting = true;
        resultCanvas.sortingOrder = 100;

        if (resultPanel.GetComponent<GraphicRaycaster>() == null)
            resultPanel.AddComponent<GraphicRaycaster>();
    }

    private int CalculateScore(bool countCorrect, bool startCorrect, bool purposeFilled, float elapsed)
    {
        int score = 100;
        if (!countCorrect) score -= 30;
        if (!startCorrect) score -= 25;
        if (!purposeFilled) score -= 15;

        if (elapsed > 180f) score -= 15;
        else if (elapsed > 120f) score -= 10;
        else if (elapsed > 60f) score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private string GetComment(int score, bool countCorrect, bool startCorrect, bool purposeFilled)
    {
        if (countCorrect && startCorrect && purposeFilled)
            return score >= 95 ? "완벽해요. 대회의실 예약 조건을 모두 정확히 맞췄습니다." : "예약 정보는 정확합니다. 다음에는 조금 더 빠르게 처리해보세요.";

        string message = "예약은 완료됐지만 ";
        if (!countCorrect) message += "회의 인원 ";
        if (!startCorrect) message += "시작 시간 ";
        if (!purposeFilled) message += "회의 목적 ";
        return message.TrimEnd() + " 항목을 다시 확인하면 더 좋은 점수를 받을 수 있습니다.";
    }

    private bool IsStartTimeCorrect()
    {
        int hour = -1;
        int minute = -1;
        bool hourOk = startHourInput != null && int.TryParse(startHourInput.text.Trim(), out hour);
        bool minuteOk = startMinuteInput != null && int.TryParse(startMinuteInput.text.Trim(), out minute);
        return hourOk && minuteOk && hour == targetStartHour && minute == targetStartMinute;
    }

    private void OnExit()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (mtgPanel != null) mtgPanel.SetActive(false);
        missionActive = false;
        MissionManager.Instance.FreezeForMission(false);
    }
}
