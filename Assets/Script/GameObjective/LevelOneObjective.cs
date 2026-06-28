using DoorSystem;
using LightSwitchSystem;
using UnityEngine;
using static DoorSystem.DoorControl;
using static DoorSystem.DoorControl.DoorState;

public class LevelOneObjective : GameObjective
{
    public int LightsOnCount => LightSwitchManager.Instance.GetSwitchOnCount();

    public int UnlockedDoorCount => DoorManager.Instance.CountDoorsWithState(~Locked);

    public override bool IsCompleted()
    {
        DoorState notLocked = ~Locked;
        int doorCount = DoorManager.Instance.Doors.Count;
        int switchCount = LightSwitchManager.Instance.LightSwitches.Count;
        bool allDoorsLocked = doorCount > 0 && !DoorManager.Instance.HasDoorWithState(notLocked);
        bool allLightsOff = switchCount > 0 && !LightSwitchManager.Instance.HasSwitchOn();
        return allDoorsLocked && allLightsOff;
    }

    [ContextMenu("Check Completion Status")]
    private void CheckComplete()
    {
        Debug.Log($"[LevelOneObjective] Checking completion status: {IsCompleted()}");
    }
}
