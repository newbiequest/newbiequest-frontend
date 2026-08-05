using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrintMission : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject printPanel;
    [SerializeField] private Image bgColor;

    [Header("Buttons")]
    [SerializeField] private Button startPrintButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("Titles & Info")]
    [SerializeField] private TextMeshProUGUI printSettingTitle;
    [SerializeField] private TextMeshProUGUI printPaperContTitle;

    [Header("Count Display")]
    [SerializeField] private Image countBgColor;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private TextMeshProUGUI resultCommentText;
    [SerializeField] private Button resultExitButton;

    [SerializeField] private AreYouStart areYouStart;

    private int requiredCount;
    private int currentCount;
    private float startTime;

    private void Start()
    {
        if (printPanel != null) printPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (plusButton != null) plusButton.onClick.AddListener(OnPlus);
        if (minusButton != null) minusButton.onClick.AddListener(OnMinus);
        if (startPrintButton != null) startPrintButton.onClick.AddListener(OnStartPrint);
        if (resultExitButton != null) resultExitButton.onClick.AddListener(OnExit);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            areYouStart.OpenPanel("PRINT");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            areYouStart.ClosePanel();
    }

    public void TryMission()
    {
        MissionData mission = MissionManager.Instance?.CurrentMission;
        requiredCount = mission != null && mission.copyCount > 0 ? mission.copyCount : 1;
        currentCount = 1;
        startTime = Time.realtimeSinceStartup;

        UpdateCountText();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (printPanel != null) printPanel.SetActive(true);
        MissionManager.Instance.FreezeForMission(true);
    }

    private void OnPlus()
    {
        currentCount = Mathf.Min(99, currentCount + 1);
        UpdateCountText();
    }

    private void OnMinus()
    {
        currentCount = Mathf.Max(1, currentCount - 1);
        UpdateCountText();
    }

    private void UpdateCountText()
    {
        if (countText != null) countText.text = currentCount.ToString();
    }

    private void OnStartPrint()
    {
        float elapsed = Time.realtimeSinceStartup - startTime;
        int score = CalculateScore(elapsed);

        MissionManager.Instance.AddScore(score);

        if (resultScoreText != null)
            resultScoreText.text = $"점수: {score}점\n누적: {MissionManager.Instance.TotalScore}점";
        if (resultCommentText != null)
            resultCommentText.text = GetComment(score);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        StartCoroutine(MissionManager.Instance.CompleteMission(elapsed));
    }

    private int CalculateScore(float elapsed)
    {
        int diff = Mathf.Abs(currentCount - requiredCount);
        int score = 100 - diff * 18;

        if (elapsed > 180f) score -= 15;
        else if (elapsed > 120f) score -= 10;
        else if (elapsed > 60f) score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private string GetComment(int score)
    {
        int diff = Mathf.Abs(currentCount - requiredCount);
        if (score >= 95) return "완벽해요. 요청한 출력 부수와 정확히 일치합니다.";
        if (score >= 80) return $"좋아요. 요청 부수와 {diff}부 차이만 났습니다.";
        if (score >= 60) return $"완료는 했지만 {diff}부 차이가 있습니다. 출력 부수를 다시 확인해보세요.";
        if (score >= 40) return $"요청과 차이가 큽니다. 필요한 부수는 {requiredCount}부였습니다.";
        return $"출력 결과가 요청과 많이 다릅니다. 요청 부수 {requiredCount}부를 먼저 확인하세요.";
    }

    private void OnExit()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (printPanel != null) printPanel.SetActive(false);
        MissionManager.Instance.FreezeForMission(false);
    }
}
