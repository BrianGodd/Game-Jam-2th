using DoorSystem;
using UnityEngine;
using static DoorSystem.DoorControl;
using static DoorSystem.DoorControl.DoorState;

public class LevelOneObjective : GameObjective
{
    public override bool IsCompleted()
    {
        DoorState notLocked = ~Locked;
        bool AllDoorsLocked = !DoorManager.Instance.HasDoorWithState(notLocked);
        return AllDoorsLocked;
    }

    [ContextMenu("Check Completion Status")]
    private void CheckComplete()
    {
        Debug.Log($"[LevelOneObjective] Checking completion status: {IsCompleted()}");
    }
}
