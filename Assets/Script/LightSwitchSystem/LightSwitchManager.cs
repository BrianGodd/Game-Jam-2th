using System.Collections.Generic;
using UnityEngine;

namespace LightSwitchSystem
{
    public class LightSwitchManager : MonoBehaviour
    {
        private static LightSwitchManager instance;
        public static LightSwitchManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<LightSwitchManager>();
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        [SerializeField] private List<LightSwitch> lightSwitches = new();

        public List<LightSwitch> LightSwitches => lightSwitches;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeLightSwitchList();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterLightSwitch(LightSwitch lightSwitch)
        {
            if (lightSwitch == null || lightSwitches.Contains(lightSwitch))
            {
                return;
            }

            lightSwitches.Add(lightSwitch);
        }

        public void UnregisterLightSwitch(LightSwitch lightSwitch)
        {
            if (lightSwitch == null)
            {
                return;
            }

            lightSwitches.Remove(lightSwitch);
        }

        private void InitializeLightSwitchList()
        {
            List<LightSwitch> orderedLightSwitches = new(lightSwitches.Count);
            for (int i = 0; i < lightSwitches.Count; i++)
            {
                LightSwitch lightSwitch = lightSwitches[i];
                if (lightSwitch != null && !orderedLightSwitches.Contains(lightSwitch))
                {
                    orderedLightSwitches.Add(lightSwitch);
                }
            }

            lightSwitches.Clear();
            for (int i = 0; i < orderedLightSwitches.Count; i++)
            {
                RegisterLightSwitch(orderedLightSwitches[i]);
            }
        }
    }
}
