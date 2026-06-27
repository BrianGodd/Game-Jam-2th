using System.Collections.Generic;
using UnityEngine;

namespace LightSwitchSystem
{
    /// <summary>
    /// a simple light switch that can turn on/off a list of lights
    /// </summary>
    public class LightSwitch : MonoBehaviour
    {
        [SerializeField] private List<Light> lights = new();
        [SerializeField] private bool startOn = true;

        public bool IsOn { get; private set; }

        private void Awake()
        {
            ApplyState(startOn);
        }

        private void Start()
        {
            LightSwitchManager.Instance.RegisterLightSwitch(this);
        }

        private void OnDestroy()
        {
            LightSwitchManager.Instance?.UnregisterLightSwitch(this);
        }

        public void On()
        {
            ApplyState(true);
        }

        public void Off()
        {
            ApplyState(false);
        }

        public void Toggle()
        {
            ApplyState(!IsOn);
        }

        private void ApplyState(bool isOn)
        {
            IsOn = isOn;

            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].enabled = isOn;
            }
        }
    }
}
