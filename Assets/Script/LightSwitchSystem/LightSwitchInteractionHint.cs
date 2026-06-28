using System.Collections.Generic;
using UnityEngine;

namespace LightSwitchSystem
{
    [RequireComponent(typeof(LightSwitch))]
    [RequireComponent(typeof(Interactable))]
    public class LightSwitchInteractionHint : MonoBehaviour
    {
        [SerializeField] private string interactKeyText = "E";

        private LightSwitch lightSwitch;
        private Interactable interactable;
        private readonly List<string> hintTexts = new(1);

        private void Awake()
        {
            lightSwitch = GetComponent<LightSwitch>();
            interactable = GetComponent<Interactable>();

            interactable.OnRaycastEnter.AddListener(ShowHint);
            interactable.OnRaycastExit.AddListener(HideHint);
            interactable.OnInteract.AddListener(RefreshHint);
        }

        private void OnDestroy()
        {
            interactable.OnRaycastEnter.RemoveListener(ShowHint);
            interactable.OnRaycastExit.RemoveListener(HideHint);
            interactable.OnInteract.RemoveListener(RefreshHint);
        }

        private void ShowHint()
        {
            RefreshHint();
        }

        private void RefreshHint()
        {
            hintTexts.Clear();
            hintTexts.Add(lightSwitch.IsOn
                ? $"\u6309{interactKeyText}\u95dc\u71c8"
                : $"\u6309{interactKeyText}\u958b\u71c8");

            ButtonHintUI.Instance.Show(hintTexts);
        }

        private void HideHint()
        {
            ButtonHintUI.Instance.Hide();
        }
    }
}
