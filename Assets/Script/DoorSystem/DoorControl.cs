using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorControl : MonoBehaviour
{
    private static readonly int CloseHash = Animator.StringToHash("Close");
    private static readonly int OpenHash = Animator.StringToHash("Open");

    [SerializeField] private Collider doorCollider;

    [SerializeField] private Animator animator;

    Coroutine waitStateRoutine;

    enum DoorState
    {
        Closed,
        Opened,
        Closing,
        Opening,
        Locked
    }
    [SerializeField] DoorState doorState = DoorState.Closed;

    [ContextMenu("Close DoorControl")]
    public void Close()
    {
        if (!IsClosable()) return;

        doorState = DoorState.Closing;
        animator.SetTrigger(CloseHash);

        // wait for the animation to finish and then enable the collider
        waitStateRoutine = StartCoroutine(WaitForState(DoorState.Closed, "Closed"));
    }

    [ContextMenu("Open DoorControl")]
    public void Open()
    {
        if (!IsOpenable()) return;

        if (waitStateRoutine != null)
        {
            StopCoroutine(waitStateRoutine);
            waitStateRoutine = null;
        }


        doorState = DoorState.Opening;
        animator.SetTrigger(OpenHash);

        doorCollider.enabled = false;

        waitStateRoutine = StartCoroutine(WaitForState(DoorState.Opened, "Opened"));
    }


    [ContextMenu("Lock DoorControl")]
    public void Lock()
    {
        if (!IsLockable()) return;
        doorState = DoorState.Locked;
        Debug.Log($"[{name}|{transform.position}] is now locked");
    }

    [ContextMenu("Unlock DoorControl")]
    public void Unlock()
    {
        if (!IsUnlockable()) return;
        doorState = DoorState.Closed;
        Debug.Log($"[{name}|{transform.position}] is now unlocked");
    }

    public void ToggleOpen()
    {
        if (doorState == DoorState.Closed)
        {
            Open();
        }
        else if (doorState == DoorState.Opened)
        {
            Close();
        }
    }

    public void ToggleLock()
    {
        if(doorState == DoorState.Locked)
        {
            Unlock();
        }
        else if (doorState == DoorState.Closed)
        {
            Lock();
        }
    }


    #region State Checkers
    private bool IsClosable()
    {
        // TODO: check if anything is in the way of the door before closing it

        if (doorState == DoorState.Opened) return true;
        return false;
    }

    private bool IsOpenable()
    {
        if (doorState == DoorState.Closed)
        {
            return true;
        }
        return false;
    }

    private bool IsLockable()
    {
        if (doorState == DoorState.Closed)
        {
            return true;
        }

        return false;
    }

    private bool IsUnlockable()
    {
        if (doorState == DoorState.Locked)
        {
            return true;
        }
        return false;
    }
    #endregion

    IEnumerator WaitForState(DoorState state, string stateName)
    {
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName));
        doorState = state;

        if (doorState == DoorState.Closed)
        {
            doorCollider.enabled = true;
        }
    }
}
