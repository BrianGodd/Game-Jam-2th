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

    public void CheckMission()
    {
        if(MissionManager.Instance.HasIncompleteMission)
        {
            Debug.Log("O: Incomplete missions.");
        }
        else
        {
            Debug.Log("X: No incomplete missions.");
        }
    }

    public void Start()
    {
        InvokeRepeating(nameof(CheckMission), 1f, 2f);
    }
}
