using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerMission : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private AreYouStart areYouStart;

    [Header("Panel")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI statPageText;
    [SerializeField] private TextMeshProUGUI statAccText;
    [SerializeField] private TextMeshProUGUI statErrorsText;
    [SerializeField] private TextMeshProUGUI statCpmText;

    [Header("Page Dots")]
    [SerializeField] private Transform pageDotsContainer;
    [SerializeField] private GameObject pageDotPrefab;

    [Header("Code Box")]
    [SerializeField] private TextMeshProUGUI codeDisplayText;
    [SerializeField] private Image progressBarFill;

    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;

    [Header("Buttons")]
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("Result Panel")]
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI resultDescText;
    [SerializeField] private TextMeshProUGUI resultStatsText;
    [SerializeField] private Button exitButton;

    [Header("Colors")]
    [SerializeField] private Color colorCorrect = new Color(0.11f, 0.62f, 0.46f);
    [SerializeField] private Color colorWrong = new Color(0.89f, 0.29f, 0.29f);
    [SerializeField] private Color colorPending = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color colorCursor = new Color(0.50f, 0.44f, 0.87f);

    private static readonly string[] CodeSnippets =
    {
@"using UnityEngine;",
@"public class PlayerController : MonoBehaviour {",
@"public float speed = 5f; private Rigidbody rb;",
@"void Awake() { rb = GetComponent<Rigidbody>(); }",
@"void Update() { ReadInput(); RotateCamera(); }",
@"float h = Input.GetAxisRaw(""Horizontal"");",
@"float v = Input.GetAxisRaw(""Vertical"");",
@"Vector3 move = new Vector3(h, 0f, v).normalized;",
@"rb.MovePosition(rb.position + move * speed * Time.fixedDeltaTime);",
@"bool running = Input.GetKey(KeyCode.LeftShift);",
@"currentSpeed = running ? speed * 1.7f : speed;",
@"if (Camera.main != null) Camera.main.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);",
@"public class MissionTimer : MonoBehaviour {",
@"elapsed += Time.unscaledDeltaTime;",
@"int min = Mathf.FloorToInt(remain / 60f); int sec = Mathf.FloorToInt(remain % 60f);",
@"timerText.text = min.ToString(""00"") + "":"" + sec.ToString(""00"");",
@"public void AddScore(int score) { totalScore += score; }",
@"if (currentMission == null) return;",
@"Debug.Log(""Mission received: "" + currentMission.taskType);",
@"panel.SetActive(true); Cursor.visible = true;",
@"panel.SetActive(false); Cursor.lockState = CursorLockMode.None;",
@"public class CoffeeMission : MonoBehaviour {",
@"if (madeCoffee >= requestCoffee) ShowResult();",
@"sugarCount = Mathf.Clamp(sugarCount + 1, 0, 5);",
@"coffeeImage.gameObject.SetActive(true);",
@"public class PrintMission : MonoBehaviour {",
@"copyCount = Mathf.Clamp(copyCount + 1, 1, 20);",
@"bool correct = copyCount == mission.printCount;",
@"public class MeetingReservation : MonoBehaviour {",
@"headCount = Mathf.Clamp(headCount, minPeople, maxPeople);",
@"bool timeOk = inputTime.Trim() == mission.meetingStartTime.Trim();",
@"bool purposeOk = !string.IsNullOrWhiteSpace(purposeInput.text);",
@"public class DeliveryMission : MonoBehaviour {",
@"if (selectedNpcName == mission.targetNpcName) CompleteDelivery();",
@"public class ParcelMission : MonoBehaviour {",
@"currentIndex = (currentIndex + 1) % parcelNames.Length;",
@"nameText.text = parcelNames[currentIndex];",
@"public class DocumentStorage : MonoBehaviour {",
@"fileRect.SetParent(targetTray, false);",
@"if (placedCount >= fileCount) ShowResult();",
@"public static class GameSession { public static long AccessToken; public static string Nickname; }",
@"string json = JsonUtility.ToJson(request);",
@"request.SetRequestHeader(""Content-Type"", ""application/json"");",
@"yield return request.SendWebRequest();",
@"if (request.result != UnityWebRequest.Result.Success) Debug.LogError(request.error);",
@"Application.targetFrameRate = 60;",
@"DontDestroyOnLoad(gameObject);",
@"Time.timeScale = freeze ? 0f : 1f;",
@"Cursor.lockState = CursorLockMode.None;",
@"return Mathf.Clamp(score, 0, 100);",
@"}"
    };

    private readonly List<int> pageOrder = new List<int>();
    private readonly List<float> pageAccuracy = new List<float>();

    private int targetPages = 1;
    private int currentPage = 0;
    private int totalErrors = 0;
    private int totalTyped = 0;
    private float startTime;
    private float elapsedSec;
    private bool isRunning;
    private bool resultShown;

    private void Awake()
    {
        if (inputField != null)
            inputField.onValueChanged.AddListener(OnInputChanged);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(SubmitCurrentPage);
        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitCurrentPage);
        if (resetButton != null)
            resetButton.onClick.AddListener(OnReset);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);

        if (missionPanel != null) missionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        SetSubmitButtons(false, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.OpenPanel("COMPUTER");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.ClosePanel();
    }

    private void Update()
    {
        if (!isRunning || resultShown) return;

        elapsedSec = Time.realtimeSinceStartup - startTime;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            SubmitCurrentPage();
    }

    public void TryMission()
    {
        MissionData mission = MissionManager.Instance?.CurrentMission;
        if (mission == null) return;

        targetPages = mission.pageCount > 0
            ? Mathf.Clamp(mission.pageCount, 1, CodeSnippets.Length)
            : 1;

        BuildPageOrder();
        ResetState();

        missionPanel.SetActive(true);
        resultPanel?.SetActive(false);
        inputField.gameObject.SetActive(true);
        inputField.ActivateInputField();

        startTime = Time.realtimeSinceStartup;
        isRunning = true;
        resultShown = false;

        RefreshAll();
        MissionManager.Instance.FreezeForMission(true);
    }

    private void BuildPageOrder()
    {
        pageOrder.Clear();
        List<int> pool = new List<int>();
        for (int i = 0; i < CodeSnippets.Length; i++)
            pool.Add(i);

        for (int i = 0; i < targetPages; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            pageOrder.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }
    }

    private void OnInputChanged(string value)
    {
        if (!isRunning || resultShown) return;
        RefreshAll();
    }

    private void SubmitCurrentPage()
    {
        if (!isRunning || resultShown) return;

        string input = CleanInput(inputField.text);
        string target = CurrentSnippet();
        int errors = CountErrors(input, target);
        int typed = Mathf.Max(input.Length, target.Length);
        int correct = Mathf.Max(0, typed - errors);
        float acc = typed > 0 ? (float)correct / typed * 100f : 100f;

        totalErrors += errors;
        totalTyped += target.Length;
        pageAccuracy.Add(acc);

        if (currentPage >= targetPages - 1)
        {
            FinishMission();
            return;
        }

        currentPage++;
        inputField.text = string.Empty;
        inputField.ActivateInputField();
        RefreshAll();
    }

    private void FinishMission()
    {
        isRunning = false;
        resultShown = true;
        elapsedSec = Time.realtimeSinceStartup - startTime;

        float avgAcc = 100f;
        if (pageAccuracy.Count > 0)
        {
            float sum = 0f;
            foreach (float acc in pageAccuracy)
                sum += acc;
            avgAcc = sum / pageAccuracy.Count;
        }

        int score = CalculateScore(avgAcc, totalErrors, elapsedSec);
        MissionManager.Instance.AddScore(score);
        ShowResult(score, avgAcc);

        StartCoroutine(MissionManager.Instance.CompleteMission(elapsedSec));
    }

    private int CalculateScore(float avgAcc, int errors, float elapsed)
    {
        int score = Mathf.RoundToInt(avgAcc);
        score -= Mathf.Min(25, errors);

        if (elapsed > 180f) score -= 15;
        else if (elapsed > 120f) score -= 10;
        else if (elapsed > 60f) score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private void ShowResult(int score, float avgAcc)
    {
        if (gradeText != null)
            gradeText.text = GetGrade(score);
        if (resultDescText != null)
            resultDescText.text = GetComment(score);
        if (resultStatsText != null)
        {
            resultStatsText.text =
                $"점수: {score}점\n" +
                $"누적 점수: {MissionManager.Instance.TotalScore}점\n" +
                $"정확도: {Mathf.RoundToInt(avgAcc)}%\n" +
                $"오타 수: {totalErrors}개\n" +
                $"소요 시간: {Mathf.RoundToInt(elapsedSec)}초";
        }

        missionPanel.SetActive(false);
        resultPanel.SetActive(true);
    }

    private string GetGrade(int score)
    {
        if (score >= 95) return "S";
        if (score >= 85) return "A";
        if (score >= 70) return "B";
        if (score >= 50) return "C";
        return "D";
    }

    private string GetComment(int score)
    {
        if (score >= 95) return "완벽에 가까워요. 정확도와 속도 모두 훌륭합니다.";
        if (score >= 85) return "아주 좋아요. 몇 글자만 더 조심하면 완벽합니다.";
        if (score >= 70) return "잘했습니다. 오타는 조금 있었지만 업무 처리에는 충분합니다.";
        if (score >= 50) return "완료는 했지만 정확도가 아쉽습니다. 천천히 확인하며 입력해보세요.";
        return "오타가 많았습니다. 줄을 끝까지 보고 차분히 입력하는 연습이 필요합니다.";
    }

    private void RefreshAll()
    {
        RefreshCodeDisplay();
        RefreshStats();
        RefreshProgress();
        RefreshDots();
        UpdateSubmitButtons();
    }

    private void RefreshCodeDisplay()
    {
        if (codeDisplayText == null) return;

        string target = CurrentSnippet();
        string input = CleanInput(inputField.text);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < target.Length; i++)
        {
            string ch = EscapeRichText(target[i].ToString());
            if (i < input.Length)
            {
                Color color = input[i] == target[i] ? colorCorrect : colorWrong;
                string hex = ColorUtility.ToHtmlStringRGB(color);
                sb.Append("<color=#").Append(hex).Append(">").Append(ch).Append("</color>");
            }
            else if (i == input.Length)
            {
                string hex = ColorUtility.ToHtmlStringRGB(colorCursor);
                sb.Append("<color=#").Append(hex).Append("><u>").Append(ch).Append("</u></color>");
            }
            else
            {
                string hex = ColorUtility.ToHtmlStringRGB(colorPending);
                sb.Append("<color=#").Append(hex).Append(">").Append(ch).Append("</color>");
            }
        }

        if (input.Length > target.Length)
        {
            string hex = ColorUtility.ToHtmlStringRGB(colorWrong);
            for (int i = target.Length; i < input.Length; i++)
            {
                sb.Append("<color=#").Append(hex).Append(">").Append(EscapeRichText(input[i].ToString())).Append("</color>");
            }
        }

        codeDisplayText.text = sb.ToString();
    }

    private void RefreshStats()
    {
        string input = CleanInput(inputField.text);
        string target = CurrentSnippet();
        int liveErrors = CountErrors(input, target);
        int typed = Mathf.Max(input.Length, target.Length);
        int correct = Mathf.Max(0, typed - liveErrors);
        int liveAcc = typed > 0 ? Mathf.RoundToInt((float)correct / typed * 100f) : 100;

        if (statPageText != null) statPageText.text = $"{currentPage + 1} / {targetPages}";
        if (statAccText != null) statAccText.text = $"{liveAcc}%";
        if (statErrorsText != null) statErrorsText.text = (totalErrors + liveErrors).ToString();

        if (statCpmText != null)
        {
            float minutes = Mathf.Max(0.01f, elapsedSec / 60f);
            statCpmText.text = Mathf.RoundToInt(input.Length / minutes).ToString();
        }
    }

    private void RefreshProgress()
    {
        if (progressBarFill == null) return;

        string input = CleanInput(inputField.text);
        string target = CurrentSnippet();
        progressBarFill.fillAmount = target.Length > 0
            ? Mathf.Clamp01((float)input.Length / target.Length)
            : 0f;
    }

    private void RefreshDots()
    {
        if (pageDotsContainer == null) return;

        foreach (Transform child in pageDotsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < targetPages; i++)
        {
            GameObject dot;
            if (pageDotPrefab != null)
            {
                dot = Instantiate(pageDotPrefab, pageDotsContainer);
                dot.SetActive(true);
            }
            else
            {
                dot = new GameObject("PageDot", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(pageDotsContainer, false);
                dot.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 12f);
            }

            Image img = dot.GetComponent<Image>();
            if (img != null)
            {
                if (i < currentPage) img.color = colorCorrect;
                else if (i == currentPage) img.color = colorCursor;
                else img.color = colorPending;
            }
        }
    }

    private void UpdateSubmitButtons()
    {
        bool lastPage = currentPage >= targetPages - 1;
        SetSubmitButtons(!lastPage, lastPage);
    }

    private void SetSubmitButtons(bool nextVisible, bool submitVisible)
    {
        if (nextPageButton != null) nextPageButton.gameObject.SetActive(nextVisible);
        if (submitButton != null) submitButton.gameObject.SetActive(submitVisible);
    }

    private void OnReset()
    {
        ResetState();
        resultPanel?.SetActive(false);
        missionPanel.SetActive(true);
        isRunning = true;
        resultShown = false;
        startTime = Time.realtimeSinceStartup;
        inputField.ActivateInputField();
        RefreshAll();
    }

    private void OnClose()
    {
        missionPanel.SetActive(false);
        resultPanel?.SetActive(false);
        isRunning = false;
        resultShown = false;
        MissionManager.Instance.FreezeForMission(false);
    }

    private void OnExit()
    {
        resultPanel?.SetActive(false);
        missionPanel.SetActive(false);
        ResetState();
        MissionManager.Instance.FreezeForMission(false);
    }

    private void ResetState()
    {
        currentPage = 0;
        totalErrors = 0;
        totalTyped = 0;
        elapsedSec = 0f;
        startTime = -1f;
        isRunning = false;
        resultShown = false;
        pageAccuracy.Clear();

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.interactable = true;
        }

        SetSubmitButtons(false, false);
    }

    private string CurrentSnippet()
    {
        if (pageOrder.Count == 0) return string.Empty;
        return CodeSnippets[pageOrder[currentPage]];
    }

    private int CountErrors(string input, string target)
    {
        int errors = 0;
        int min = Mathf.Min(input.Length, target.Length);

        for (int i = 0; i < min; i++)
        {
            if (input[i] != target[i]) errors++;
        }

        errors += Mathf.Abs(target.Length - input.Length);
        return errors;
    }

    private string CleanInput(string input)
    {
        return input.TrimEnd('\r', '\n');
    }

    private string EscapeRichText(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
