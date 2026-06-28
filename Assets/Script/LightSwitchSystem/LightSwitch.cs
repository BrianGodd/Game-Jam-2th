using System.Collections.Generic;
using UnityEngine;

namespace LightSwitchSystem
{
    /// <summary>
    /// a simple light switch that can turn on/off a list of lights
    /// </summary>
    public class LightSwitch : MonoBehaviour
    {
        private const string EmissionColorProperty = "_EmissionColor";
        private const string EmissionKeyword = "_EMISSION";

        [SerializeField] private List<Light> lights = new();
        [SerializeField] private bool startOn = true;

        public bool IsOn { get; private set; }
        public List<Light> Lights => lights;

        private readonly List<EmissionMaterialData> emissionMaterials = new();

        private class EmissionMaterialData
        {
            public Material material;
            public Color emissionColor;
            public bool emissionKeywordEnabled;
        }

        private void Awake()
        {
            CacheEmissionMaterials();
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

            ApplyEmissionState(isOn);
        }

        private void CacheEmissionMaterials()
        {
            emissionMaterials.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (!material.HasProperty(EmissionColorProperty))
                    {
                        continue;
                    }

                    emissionMaterials.Add(new EmissionMaterialData
                    {
                        material = material,
                        emissionColor = material.GetColor(EmissionColorProperty),
                        emissionKeywordEnabled = material.IsKeywordEnabled(EmissionKeyword)
                    });
                }
            }
        }

        private void ApplyEmissionState(bool isOn)
        {
            for (int i = 0; i < emissionMaterials.Count; i++)
            {
                EmissionMaterialData emissionMaterial = emissionMaterials[i];
                emissionMaterial.material.SetColor(
                    EmissionColorProperty,
                    isOn ? emissionMaterial.emissionColor : Color.black);

                if (isOn && emissionMaterial.emissionKeywordEnabled)
                {
                    emissionMaterial.material.EnableKeyword(EmissionKeyword);
                    continue;
                }

                if (isOn)
                {
                    emissionMaterial.material.DisableKeyword(EmissionKeyword);
                    continue;
                }

                emissionMaterial.material.DisableKeyword(EmissionKeyword);
            }
        }
    }
}
