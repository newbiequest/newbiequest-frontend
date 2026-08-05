using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DocStorageMission : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private AreYouStart areYouStart;

    [Header("Panel")]
    [SerializeField] private GameObject docStoragePanel;
    [SerializeField] private TMP_FontAsset koreanFont;

    [Header("Answer Mapping: fileIndex -> trayIndex")]
    public int[] correctTrayIndex = new int[] { 0, 1, 2 };

    [Header("Score")]
    public int scorePerCorrect = 34;

    private readonly List<FileDragHandler> fileHandlers = new List<FileDragHandler>();
    private readonly List<RectTransform> trayRects = new List<RectTransform>();
    private GameObject resultPanel;
    private bool missionActive;
    private float startTime;

    private void Awake()
    {
        if (docStoragePanel == null)
        {
            Debug.LogError("DocStorageMission: docStoragePanel is not assigned.", this);
            enabled = false;
            return;
        }

        SetupFile("file1", 0);
        SetupFile("file2", 1);
        SetupFile("file3", 2);
        SetupTray("Tray1", 0);
        SetupTray("Tray2", 1);
        SetupTray("Tray3", 2);

        if (koreanFont == null)
        {
            TextMeshProUGUI titleText = docStoragePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (titleText != null) koreanFont = titleText.font;
        }
        ApplyKoreanFontToChildTexts();

        docStoragePanel.SetActive(false);
    }

    private void ApplyKoreanFontToChildTexts()
    {
        if (koreanFont == null) return;

        TextMeshProUGUI[] texts = docStoragePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
            text.font = koreanFont;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.OpenPanel("DOC_STORAGE");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        areYouStart.ClosePanel();
    }

    public void TryMission()
    {
        StartMission();
    }

    public void StartMission(int[] answerMap = null)
    {
        if (answerMap != null && answerMap.Length == 3)
            correctTrayIndex = answerMap;

        if (resultPanel != null)
        {
            Destroy(resultPanel);
            resultPanel = null;
        }

        ResetMissionState();
        startTime = Time.realtimeSinceStartup;
        missionActive = true;
        docStoragePanel.SetActive(true);
        MissionManager.Instance.FreezeForMission(true);
    }

    private void SetupFile(string objName, int fileIndex)
    {
        Transform t = docStoragePanel.transform.Find(objName);
        if (t == null)
        {
            Debug.LogWarning($"DocStorageMission: {objName} object not found.", this);
            return;
        }

        FileDragHandler handler = t.GetComponent<FileDragHandler>();
        if (handler == null) handler = t.gameObject.AddComponent<FileDragHandler>();

        handler.Init(fileIndex, this);
        fileHandlers.Add(handler);
    }

    private void SetupTray(string objName, int trayIndex)
    {
        Transform t = docStoragePanel.transform.Find(objName);
        if (t == null)
        {
            Debug.LogWarning($"DocStorageMission: {objName} object not found.", this);
            return;
        }

        RectTransform rt = t.GetComponent<RectTransform>();
        trayRects.Add(rt);

        TrayDropHandler dropHandler = t.GetComponent<TrayDropHandler>();
        if (dropHandler == null) dropHandler = t.gameObject.AddComponent<TrayDropHandler>();

        dropHandler.Init(trayIndex, this);

        Image img = t.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
    }

    public void NotifyDropped(int fileIndex, int trayIndex, RectTransform trayRect)
    {
        if (!missionActive) return;
        if (fileIndex < 0 || fileIndex >= fileHandlers.Count) return;

        fileHandlers[fileIndex].SnapTo(trayRect, trayIndex);

        foreach (FileDragHandler handler in fileHandlers)
        {
            if (!handler.IsPlaced) return;
        }

        FinishMission();
    }

    public bool TryDropAtPointer(FileDragHandler file, PointerEventData eventData)
    {
        if (!missionActive || file == null) return false;

        TrayDropHandler tray = FindNearestTrayByPanelPosition(eventData.position);
        if (tray == null) tray = FindTrayUnderPointer(eventData);
        if (tray == null) return false;

        NotifyDropped(file.FileIndex, tray.TrayIndex, tray.GetComponent<RectTransform>());
        return true;
    }

    private TrayDropHandler FindTrayUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return null;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            TrayDropHandler tray = result.gameObject.GetComponentInParent<TrayDropHandler>();
            if (tray != null) return tray;
        }

        return null;
    }

    private TrayDropHandler FindNearestTrayByPanelPosition(Vector2 screenPosition)
    {
        Canvas canvas = docStoragePanel.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransform panelRect = docStoragePanel.GetComponent<RectTransform>();
        if (panelRect == null) return null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPosition, cam, out Vector2 localPoint))
            return null;

        const float maxHorizontalDistance = 360f;
        const float maxVerticalDistance = 300f;
        TrayDropHandler bestTray = null;
        float bestDistance = float.MaxValue;

        foreach (RectTransform trayRect in trayRects)
        {
            if (trayRect == null) continue;

            Vector2 trayPosition = trayRect.anchoredPosition;
            float horizontalDistance = Mathf.Abs(localPoint.x - trayPosition.x);
            float verticalDistance = Mathf.Abs(localPoint.y - trayPosition.y);
            if (horizontalDistance > maxHorizontalDistance || verticalDistance > maxVerticalDistance)
                continue;

            float distance = Vector2.Distance(localPoint, trayPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTray = trayRect.GetComponent<TrayDropHandler>();
            }
        }

        return bestTray;
    }

    private void FinishMission()
    {
        missionActive = false;

        int correctCount = 0;
        for (int i = 0; i < fileHandlers.Count; i++)
        {
            if (i < correctTrayIndex.Length && fileHandlers[i].CurrentTrayIndex == correctTrayIndex[i])
                correctCount++;
        }

        float elapsed = Time.realtimeSinceStartup - startTime;
        int score = CalculateScore(correctCount, fileHandlers.Count, elapsed);

        MissionManager.Instance.AddScore(score);
        ShowResultPanel(correctCount, fileHandlers.Count, score);
        StartCoroutine(MissionManager.Instance.CompleteMission(elapsed));
    }

    private int CalculateScore(int correctCount, int totalCount, float elapsed)
    {
        if (totalCount <= 0) return 0;

        int score = Mathf.RoundToInt((float)correctCount / totalCount * 100f);

        if (elapsed > 180f) score -= 15;
        else if (elapsed > 120f) score -= 10;
        else if (elapsed > 60f) score -= 5;

        return Mathf.Clamp(score, 0, 100);
    }

    private void ShowResultPanel(int correctCount, int totalCount, int score)
    {
        resultPanel = new GameObject("DocResultPanel", typeof(RectTransform), typeof(Image));
        resultPanel.transform.SetParent(docStoragePanel.transform, false);

        RectTransform rt = resultPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.25f);
        rt.anchorMax = new Vector2(0.9f, 0.75f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = resultPanel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        CreateText(resultPanel.transform, "ScoreText",
            $"정리 결과: {correctCount} / {totalCount}\n점수: {score}점\n누적: {MissionManager.Instance.TotalScore}점",
            new Vector2(0f, 0.55f), new Vector2(1f, 0.9f), 28);

        CreateText(resultPanel.transform, "GradeText",
            GetGradeText(correctCount, totalCount),
            new Vector2(0f, 0.25f), new Vector2(1f, 0.55f), 22);

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
        if (koreanFont != null) text.font = koreanFont;
        text.text = content;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = fontSize;

        return go;
    }

    private string GetGradeText(int correctCount, int totalCount)
    {
        if (correctCount == totalCount)
            return "완벽해요. 모든 문서를 올바른 위치에 정리했습니다.";
        if (correctCount >= totalCount - 1)
            return "거의 맞았습니다. 한 번만 더 확인하면 완벽합니다.";
        if (correctCount > 0)
            return "일부 문서가 다른 트레이에 들어갔습니다. 분류 기준을 다시 확인해보세요.";
        return "문서 정리가 요청과 많이 다릅니다. 파일과 트레이 번호를 차근차근 맞춰보세요.";
    }

    private void OnExitButtonClicked()
    {
        if (resultPanel != null)
        {
            Destroy(resultPanel);
            resultPanel = null;
        }

        docStoragePanel.SetActive(false);
        ResetMissionState();
        MissionManager.Instance.FreezeForMission(false);
    }

    public void ResetMissionState()
    {
        foreach (FileDragHandler handler in fileHandlers)
            handler.ResetToOrigin();

        missionActive = false;
    }

    public class FileDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private DocStorageMission mission;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 originalAnchoredPos;
        private Transform originalParent;

        public int FileIndex { get; private set; }
        public bool IsPlaced { get; private set; }
        public int CurrentTrayIndex { get; private set; } = -1;

        public void Init(int fileIndex, DocStorageMission owner)
        {
            FileIndex = fileIndex;
            mission = owner;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image img = GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }

            originalAnchoredPos = rectTransform.anchoredPosition;
            originalParent = transform.parent;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsPlaced = false;
            CurrentTrayIndex = -1;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            float scale = GetCanvasScale();
            rectTransform.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            if (!mission.TryDropAtPointer(this, eventData))
                ResetToOrigin();
        }

        public void SnapTo(RectTransform trayRect, int trayIndex)
        {
            IsPlaced = true;
            CurrentTrayIndex = trayIndex;
            rectTransform.position = trayRect.position;
        }

        public void ResetToOrigin()
        {
            IsPlaced = false;
            CurrentTrayIndex = -1;
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPos;
        }

        private float GetCanvasScale()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.scaleFactor : 1f;
        }
    }

    public class TrayDropHandler : MonoBehaviour, IDropHandler
    {
        private DocStorageMission mission;
        public int TrayIndex { get; private set; }

        public void Init(int trayIndex, DocStorageMission owner)
        {
            TrayIndex = trayIndex;
            mission = owner;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            FileDragHandler file = eventData.pointerDrag.GetComponent<FileDragHandler>();
            if (file == null) return;

            mission.NotifyDropped(file.FileIndex, TrayIndex, GetComponent<RectTransform>());
        }
    }
}
