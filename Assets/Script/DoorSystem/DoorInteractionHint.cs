using DoorSystem;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DoorControl))]
[RequireComponent(typeof(Interactable))]
public class DoorInteractionHint : MonoBehaviour
{
    [SerializeField] private string interactKeyText = "E";

    private DoorControl doorControl;
    private Interactable interactable;
    private bool isTargeted;
    private readonly List<string> hintTexts = new(2);

    private void Awake()
    {
        doorControl = GetComponent<DoorControl>();
        interactable = GetComponent<Interactable>();

        interactable.OnRaycastEnter.AddListener(HandleRaycastEnter);
        interactable.OnRaycastExit.AddListener(HandleRaycastExit);
        interactable.OnInteract.AddListener(RefreshHint);
        interactable.OnHoldInteract.AddListener(RefreshHint);
    }

    private void OnDestroy()
    {
        interactable.OnRaycastEnter.RemoveListener(HandleRaycastEnter);
        interactable.OnRaycastExit.RemoveListener(HandleRaycastExit);
        interactable.OnInteract.RemoveListener(RefreshHint);
        interactable.OnHoldInteract.RemoveListener(RefreshHint);
    }

    private void HandleRaycastEnter()
    {
        isTargeted = true;
        RefreshHint();
    }

    private void HandleRaycastExit()
    {
        isTargeted = false;
        ButtonHintUI.Instance.Hide();
    }

    private void RefreshHint()
    {
        if (!isTargeted)
        {
            return;
        }

        BuildHintTexts();
        if (hintTexts.Count == 0)
        {
            ButtonHintUI.Instance.Hide();
            return;
        }

        ButtonHintUI.Instance.Show(hintTexts);
    }

    private void BuildHintTexts()
    {
        hintTexts.Clear();

        if (doorControl.CanToggleOpen())
        {
            if (doorControl.State == DoorControl.DoorState.Closed)
            {
                hintTexts.Add($"{interactKeyText} : \u958b\u9580");
            }
            else if (doorControl.State == DoorControl.DoorState.Opened)
            {
                hintTexts.Add($"{interactKeyText} : \u95dc\u9580");
            }
        }

        if (doorControl.CanToggleLock())
        {
            if (doorControl.State == DoorControl.DoorState.Closed)
            {
                hintTexts.Add($"\u9577\u6309 {interactKeyText} : \u9396\u9580");
            }
            else if (doorControl.State == DoorControl.DoorState.Locked)
            {
                hintTexts.Add($"\u9577\u6309 {interactKeyText} : \u89e3\u9396");
            }
        }
    }
}
