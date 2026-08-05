using UnityEngine;
using TMPro;

public class AreYouStart : MonoBehaviour
{
    [SerializeField] private GameObject areYouStartPanel;
    [SerializeField] private TextMeshProUGUI missionDescText; // 선택사항

    private string currentTriggerType;

    void Start()
    {
        if (areYouStartPanel != null)
            areYouStartPanel.SetActive(false);
    }

    public void OpenPanel(string triggerTaskType)
    {
        MissionData mission = MissionManager.Instance?.CurrentMission;

        if (mission == null)
        {
            Debug.Log("미션 없음");
            return;
        }

        if (mission.taskType != triggerTaskType)
        {
            Debug.Log($"현재 미션({mission.taskType})과 불일치({triggerTaskType})");
            return;
        }

        currentTriggerType = triggerTaskType;

        if (missionDescText != null)
            missionDescText.text = mission.message;

        areYouStartPanel.SetActive(true);
        MissionManager.Instance.FreezeForMission(true);
    }

    public void ClosePanel()
    {
        areYouStartPanel?.SetActive(false);
        MissionManager.Instance?.FreezeForMission(false);
    }

    [SerializeField] private PrintMission printMission;
    [SerializeField] private CoffeeMission coffeeMission;
    [SerializeField] private ComputerMission computerMission;
    [SerializeField] private ParcelMission parcelMission;
    [SerializeField] private BigMTG bigMtgMission;
    [SerializeField] private SmallMTG smallMtgMission;
    [SerializeField] private DocStorageMission docStorageMission;

    public void StartMissionButton()
    {
        areYouStartPanel?.SetActive(false);

        switch (currentTriggerType)
        {
            case "PRINT":
                printMission.TryMission();
                break;
            case "COFFEE":
                coffeeMission.TryMission();
                break;
            case "COMPUTER":
                computerMission.TryMission();
                break;
            case "PARCEL":
                parcelMission.TryMission();
                break;
            case "BIG_MTG":
                bigMtgMission.TryMission();
                break;
            case "SMALL_MTG":
                smallMtgMission.TryMission();
                break;
            case "DOC_STORAGE":
                docStorageMission.StartMission();
                break;
        }
    }
}
