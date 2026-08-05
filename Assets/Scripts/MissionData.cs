using System;

[Serializable]
public class MissionData
{
    public string npcName;
    public string taskType;
    public string message;
    public bool isNpcMission;
    public string npcMissionType;

    // PRINT
    public int copyCount;

    // COFFEE
    public int coffeeCount;
    public int sugarCount;

    // COMPUTER
    public int pageCount;

    // DELIVERY_DOC
    public string targetNpc;

    // PARCEL
    public string ownerName;

    // DOC_STORAGE
    public string sortingType;

    // NOTICE_BOARD
    public string noticeTitle;

    // 회의실
    public int meetingHeadcount;
    public int meetingStartHour;
    public int meetingStartMinute;
    public string meetingPurpose;

    [NonSerialized] public DateTime givenAt;
    [NonSerialized] public string missionId;
}
