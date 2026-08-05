using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoffeeMission : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private AreYouStart areYouStart;

    [Header("Panel")]
    [SerializeField] private GameObject coffeePanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Coffee Images")]
    [SerializeField] private Image emptyCoffeeImg;
    [SerializeField] private Image coffeeImg;

    [Header("Buttons")]
    [SerializeField] private Button coffeeButton;
    [SerializeField] private Button sugarButton;
    [SerializeField] private Button nextCupButton;
    [SerializeField] private Button completeButton;

    [Header("Count Text")]
    [SerializeField] private TextMeshProUGUI countCoffeeText;
    [SerializeField] private TextMeshProUGUI requestCountCoffeeText;
    [SerializeField] private TextMeshProUGUI countSugarText;
    [SerializeField] private TextMeshProUGUI requestCountSugarText;

    [Header("Title Text")]
    [SerializeField] private TextMeshProUGUI countCoffeeTitleText;
    [SerializeField] private TextMeshProUGUI requestCountCoffeeTitleText;
    [SerializeField] private TextMeshProUGUI countSugarTitleText;
    [SerializeField] private TextMeshProUGUI requestCountSugarTitleText;

    [Header("Result Panel")]
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private TextMeshProUGUI resultCommentText;
    [SerializeField] private Button resultExitButton;

    private int requiredCount;
    private int completedCount;
    private int currentCup;
    private int requiredSugar;
    private int sugarCount;
    private int totalMistakes;
    private bool hasCoffee;
    private bool isActive;
    private bool resultShown;
    private float startTime;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        coffeeButton.onClick.AddListener(OnClickCoffee);
        sugarButton.onClick.AddListener(OnClickSugar);
        nextCupButton.onClick.AddListener(OnClickNextCup);
        completeButton.onClick.AddListener(OnClickComplete);
        resultExitButton.onClick.AddListener(OnClickExit);

        coffeePanel.SetActive(false);
        resultPanel.SetActive(false);
        coffeeImg.gameObject.SetActive(false);
        emptyCoffeeImg.gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.OpenPanel("COFFEE");
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

        requiredCount = Mathf.Max(1, mission.coffeeCount);
        requiredSugar = Mathf.Max(0, mission.sugarCount);

        completedCount = 0;
        currentCup = 1;
        sugarCount = 0;
        totalMistakes = 0;
        hasCoffee = false;
        isActive = true;
        resultShown = false;
        startTime = Time.realtimeSinceStartup;

        coffeePanel.SetActive(true);
        resultPanel.SetActive(false);
        ResetCupVisual();
        UpdateUI();
        UpdateButtons();

        MissionManager.Instance.FreezeForMission(true);
    }

    private void OnClickCoffee()
    {
        if (!isActive || resultShown || hasCoffee) return;

        hasCoffee = true;
        emptyCoffeeImg.gameObject.SetActive(false);
        coffeeImg.gameObject.SetActive(true);
        UpdateButtons();
    }

    private void OnClickSugar()
    {
        if (!isActive || resultShown || !hasCoffee) return;

        sugarCount++;
        UpdateUI();
    }

    private void OnClickNextCup()
    {
        if (!isActive || resultShown || !hasCoffee) return;
        if (currentCup >= requiredCount) return;

        RecordCurrentCup();
        currentCup++;
        ResetCupVisual();
        UpdateUI();
        UpdateButtons();
    }

    private void OnClickComplete()
    {
        if (!isActive || resultShown) return;

        if (hasCoffee && completedCount < requiredCount)
            RecordCurrentCup();

        if (completedCount < requiredCount)
        {
            Debug.Log($"CoffeeMission: {requiredCount - completedCount} cup(s) still required.");
            ResetCupVisual();
            UpdateUI();
            UpdateButtons();
            return;
        }

        CompleteMission();
    }

    private void RecordCurrentCup()
    {
        totalMistakes += Mathf.Abs(sugarCount - requiredSugar);
        completedCount++;
        sugarCount = 0;
        hasCoffee = false;
    }

    private void CompleteMission()
    {
        isActive = false;
        resultShown = true;

        float elapsed = Time.realtimeSinceStartup - startTime;
        int score = CalculateScore(elapsed);

        MissionManager.Instance.AddScore(score);

        resultScoreText.text = $"점수: {score}점\n누적: {MissionManager.Instance.TotalScore}점";
        resultCommentText.text = GetComment(score);
        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();

        StartCoroutine(MissionManager.Instance.CompleteMission(elapsed));
    }

    private int CalculateScore(float elapsed)
    {
        int totalExpectedSugar = requiredCount * requiredSugar;
        int sugarPenalty = totalExpectedSugar <= 0 ? totalMistakes * 8 : Mathf.RoundToInt((float)totalMistakes / Mathf.Max(1, totalExpectedSugar) * 45f);
        int score = 100 - sugarPenalty;

        if (elapsed > 180f) score -= 15;
        else if (elapsed > 120f) score -= 10;
        else if (elapsed > 60f) score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private string GetComment(int score)
    {
        if (score >= 95) return "완벽해요. 잔 수와 설탕 수를 모두 정확하게 맞췄습니다.";
        if (score >= 85) return "아주 좋아요. 작은 차이는 있었지만 요청을 거의 정확히 처리했습니다.";
        if (score >= 70) return "괜찮아요. 커피는 준비됐지만 설탕 수를 한 번 더 확인하면 좋겠습니다.";
        if (score >= 50) return "완료는 했지만 요청과 다른 컵이 꽤 있었습니다. 조건 확인이 필요합니다.";
        return "커피 준비는 되었지만 요청 조건과 차이가 큽니다. 잔마다 설탕 수를 다시 확인해보세요.";
    }

    private void OnClickExit()
    {
        resultPanel.SetActive(false);
        coffeePanel.SetActive(false);
        ResetMissionState();
        MissionManager.Instance.FreezeForMission(false);
    }

    private void ResetMissionState()
    {
        completedCount = 0;
        currentCup = 1;
        sugarCount = 0;
        totalMistakes = 0;
        hasCoffee = false;
        isActive = false;
        resultShown = false;
        ResetCupVisual();
        UpdateUI();
        UpdateButtons();
    }

    private void ResetCupVisual()
    {
        sugarCount = 0;
        hasCoffee = false;
        emptyCoffeeImg.gameObject.SetActive(true);
        coffeeImg.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (requestCountCoffeeText) requestCountCoffeeText.text = requiredCount.ToString();
        if (requestCountSugarText) requestCountSugarText.text = requiredSugar.ToString();
        if (countCoffeeText) countCoffeeText.text = currentCup.ToString();
        if (countSugarText) countSugarText.text = sugarCount.ToString();
    }

    private void UpdateButtons()
    {
        bool canPlay = isActive && !resultShown;
        bool needsMoreCups = completedCount < requiredCount;

        coffeeButton.interactable = canPlay && needsMoreCups && !hasCoffee;
        sugarButton.interactable = canPlay && needsMoreCups && hasCoffee;
        nextCupButton.interactable = canPlay && hasCoffee && currentCup < requiredCount;
        completeButton.interactable = canPlay && hasCoffee && currentCup == requiredCount;
    }

    private bool ValidateReferences()
    {
        bool ok = true;

        ok &= Check(areYouStart, nameof(areYouStart));
        ok &= Check(coffeePanel, nameof(coffeePanel));
        ok &= Check(resultPanel, nameof(resultPanel));
        ok &= Check(emptyCoffeeImg, nameof(emptyCoffeeImg));
        ok &= Check(coffeeImg, nameof(coffeeImg));
        ok &= Check(coffeeButton, nameof(coffeeButton));
        ok &= Check(sugarButton, nameof(sugarButton));
        ok &= Check(nextCupButton, nameof(nextCupButton));
        ok &= Check(completeButton, nameof(completeButton));
        ok &= Check(resultScoreText, nameof(resultScoreText));
        ok &= Check(resultCommentText, nameof(resultCommentText));
        ok &= Check(resultExitButton, nameof(resultExitButton));

        return ok;
    }

    private bool Check(Object value, string fieldName)
    {
        if (value != null) return true;

        Debug.LogError($"CoffeeMission: {fieldName} is not assigned.", this);
        return false;
    }
}
