using DoorSystem;
using LightSwitchSystem;
using UnityEngine;
using static DoorSystem.DoorControl;
using static DoorSystem.DoorControl.DoorState;

public class LevelOneObjective : GameObjective
{
    public override bool IsCompleted()
    {
        DoorState notLocked = ~Locked;
        bool allDoorsLocked = !DoorManager.Instance.HasDoorWithState(notLocked);
        bool allLightsOff = !LightSwitchManager.Instance.HasSwitchOn();
        return allDoorsLocked && allLightsOff;
    }

    [ContextMenu("Check Completion Status")]
    private void CheckComplete()
    {
        Debug.Log($"[LevelOneObjective] Checking completion status: {IsCompleted()}");
    }
}
