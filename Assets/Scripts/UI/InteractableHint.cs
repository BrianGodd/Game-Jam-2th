using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class InteractableHint : MonoBehaviour
{
    [SerializeField] private string hintText;

    private Interactable interactable;
    private readonly List<string> hintTexts = new(1);

    private void Awake()
    {
        interactable = GetComponent<Interactable>();

        interactable.OnRaycastEnter.AddListener(ShowHint);
        interactable.OnRaycastExit.AddListener(HideHint);
        interactable.OnInteract.AddListener(HideHint);
    }

    private void OnDestroy()
    {
        interactable.OnRaycastEnter.RemoveListener(ShowHint);
        interactable.OnRaycastExit.RemoveListener(HideHint);
        interactable.OnInteract.RemoveListener(HideHint);
    }

    private void ShowHint()
    {
        hintTexts.Clear();
        hintTexts.Add(hintText);
        ButtonHintUI.Instance.Show(hintTexts);
    }

    private void HideHint()
    {
        ButtonHintUI.Instance.Hide();
    }
}
