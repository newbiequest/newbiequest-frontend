using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParcelMission : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private AreYouStart areYouStart;

    [Header("Panel")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("UI")]
    [SerializeField] private Image pracelImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button nextButton;

    [Header("Dummy Names")]
    [SerializeField]
    private string[] dummyNames = new[]
    {
        "김민준", "이서연", "박지훈", "최수아", "정도윤",
        "강하은", "조유진", "오지호", "한유나", "윤서준",
        "서예린", "임수빈", "신지우", "권나래", "문태오"
    };

    [Header("Result Panel Font")]
    [SerializeField] private TMP_FontAsset resultFont;

    private string targetOwnerName;
    private readonly List<string> nameList = new List<string>();
    private int currentIndex;
    private int moveCount;
    private float startTime;
    private bool missionActive;

    private void Awake()
    {
        if (previousButton != null) previousButton.onClick.AddListener(OnPrevious);
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (selectButton != null) selectButton.onClick.AddListener(OnSelect);

        if (missionPanel != null) missionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.OpenPanel("PARCEL");
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

        targetOwnerName = string.IsNullOrWhiteSpace(mission.ownerName) ? "상사" : mission.ownerName;
        BuildNameList();

        currentIndex = 0;
        moveCount = 0;
        startTime = Time.realtimeSinceStartup;
        missionActive = true;

        if (titleText != null) titleText.text = "상사의 택배를 찾아주세요";
        UpdateNameDisplay();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (missionPanel != null) missionPanel.SetActive(true);
        MissionManager.Instance.FreezeForMission(true);
    }

    private void BuildNameList()
    {
        nameList.Clear();
        nameList.Add(targetOwnerName);

        List<string> pool = new List<string>(dummyNames);
        pool.RemoveAll(n => n == targetOwnerName);

        int dummyCount = Mathf.Min(6, pool.Count);
        for (int i = 0; i < dummyCount; i++)
        {
            int idx = Random.Range(0, pool.Count);
            nameList.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        for (int i = nameList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (nameList[i], nameList[j]) = (nameList[j], nameList[i]);
        }
    }

    private void UpdateNameDisplay()
    {
        if (nameText != null && nameList.Count > 0)
            nameText.text = nameList[currentIndex];
    }

    private void OnNext()
    {
        if (!missionActive || nameList.Count == 0) return;
        currentIndex = (currentIndex + 1) % nameList.Count;
        moveCount++;
        UpdateNameDisplay();
    }

    private void OnPrevious()
    {
        if (!missionActive || nameList.Count == 0) return;
        currentIndex = (currentIndex - 1 + nameList.Count) % nameList.Count;
        moveCount++;
        UpdateNameDisplay();
    }

    private void OnSelect()
    {
        if (!missionActive) return;

        missionActive = false;

        string selectedName = nameText != null ? nameText.text : "";
        bool isCorrect = selectedName == targetOwnerName;
        float elapsed = Time.realtimeSinceStartup - startTime;

        int score = CalculateScore(isCorrect, elapsed);
        MissionManager.Instance.AddScore(score);

        ShowResultPanel(isCorrect, selectedName, score);
        StartCoroutine(MissionManager.Instance.CompleteMission(elapsed));
    }

    private int CalculateScore(bool isCorrect, float elapsed)
    {
        int score = isCorrect ? 100 : 45;

        if (moveCount > 10) score -= 10;
        else if (moveCount > 6) score -= 5;

        if (elapsed > 60f) score -= 15;
        else if (elapsed > 35f) score -= 10;
        else if (elapsed > 20f) score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private void ShowResultPanel(bool isCorrect, string selectedName, int score)
    {
        if (resultPanel != null) Destroy(resultPanel);

        resultPanel = new GameObject("ParcelResultPanel", typeof(RectTransform), typeof(Image));
        resultPanel.transform.SetParent(missionPanel.transform, false);

        RectTransform rt = resultPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.25f);
        rt.anchorMax = new Vector2(0.9f, 0.75f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = resultPanel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        string detail = isCorrect
            ? $"정확한 택배를 찾았습니다. ({selectedName})"
            : $"다른 택배를 골랐습니다. 선택: {selectedName} / 정답: {targetOwnerName}";

        CreateText(resultPanel.transform, "ScoreText",
            $"점수: {score}점\n누적: {MissionManager.Instance.TotalScore}점",
            new Vector2(0f, 0.58f), new Vector2(1f, 0.85f), 28);

        CreateText(resultPanel.transform, "CommentText",
            detail + "\n" + GetComment(score, isCorrect),
            new Vector2(0f, 0.25f), new Vector2(1f, 0.58f), 22);

        GameObject buttonGO = new GameObject("ExitButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(resultPanel.transform, false);
        RectTransform btnRt = buttonGO.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.05f);
        btnRt.anchorMax = new Vector2(0.65f, 0.18f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        Image btnImg = buttonGO.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.9f, 1f);

        Button btn = buttonGO.GetComponent<Button>();
        btn.onClick.AddListener(OnExitButtonClicked);

        CreateText(buttonGO.transform, "ExitButtonText", "나가기", Vector2.zero, Vector2.one, 22);
    }

    private GameObject CreateText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = fontSize;
        if (resultFont != null) text.font = resultFont;

        return go;
    }

    private string GetComment(int score, bool isCorrect)
    {
        if (score >= 95) return "빠르고 정확하게 처리했습니다.";
        if (score >= 80) return "잘 찾았습니다. 조금만 더 빠르면 완벽합니다.";
        if (score >= 60) return isCorrect ? "정답은 맞았지만 찾는 데 시간이 조금 걸렸습니다." : "선택은 틀렸지만 진행 과정은 완료했습니다.";
        return "택배 이름을 다시 확인하고 선택하는 연습이 필요합니다.";
    }

    private void OnExitButtonClicked()
    {
        if (resultPanel != null)
        {
            Destroy(resultPanel);
            resultPanel = null;
        }

        if (missionPanel != null) missionPanel.SetActive(false);
        MissionManager.Instance.FreezeForMission(false);
        ResetMissionState();
    }

    public void ResetMissionState()
    {
        currentIndex = 0;
        moveCount = 0;
        nameList.Clear();
        missionActive = false;
        if (nameText != null) nameText.text = "";
    }
}
