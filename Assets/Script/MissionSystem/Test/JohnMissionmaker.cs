using UnityEngine;
using MissionSystem;
public class JohnMissionmaker : MonoBehaviour
{
    Mission myMission;

    [ContextMenu("Create Mission")]
    public void CreateMission()
    {
        if(myMission != null)
        {
            Debug.LogWarning("Mission already exists for John. Cannot create a new one.");
            return;
        }
        var mission = new Mission();
        MissionManager.Instance.AddMission(mission);
        myMission = mission;
    }

    [ContextMenu("Complete Mission")]
    public void CompleteMission()
    {
        myMission?.Complete();
    }
}
