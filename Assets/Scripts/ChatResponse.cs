using System;

[Serializable]
public class ChatResponse
{
    public string npcName;
    public string taskType;
    public string condition;
    public string message;
    public bool isNpcMission;
}

[Serializable]
public class MissionCompleteRequest
{
    public bool completed;
    public double elapsedSeconds;
    public bool isNpcMission;
}