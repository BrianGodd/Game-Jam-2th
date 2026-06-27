using UnityEngine;

namespace VTuber.GSShader
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GaussianOldComputerEffectController : MonoBehaviour
    {
        static readonly int EffectStrengthId = Shader.PropertyToID("_OldComputerEffectStrength");
        static readonly int EffectSpeedId = Shader.PropertyToID("_OldComputerEffectSpeed");
        static readonly int ScanTopId = Shader.PropertyToID("_OldComputerScanTop");
        static readonly int ScanBottomId = Shader.PropertyToID("_OldComputerScanBottom");
        static readonly int OverallOpacityId = Shader.PropertyToID("_OldComputerOverallOpacity");
        static readonly int OverallBrightnessId = Shader.PropertyToID("_OldComputerOverallBrightness");
        static readonly int ScanlineWidthId = Shader.PropertyToID("_OldComputerScanlineWidth");
        static readonly int ScanlineSpeedId = Shader.PropertyToID("_OldComputerScanlineSpeed");
        static readonly int ScanlineDensityId = Shader.PropertyToID("_OldComputerScanlineDensity");
        static readonly int ScanlineOpacityMinId = Shader.PropertyToID("_OldComputerScanlineOpacityMin");
        static readonly int ScanlineOpacityWaveId = Shader.PropertyToID("_OldComputerScanlineOpacityWave");
        static readonly int ScanlineBrightnessMinId = Shader.PropertyToID("_OldComputerScanlineBrightnessMin");
        static readonly int ScanlineBrightnessWaveId = Shader.PropertyToID("_OldComputerScanlineBrightnessWave");

        [Range(0f, 1f)]
        public float effectStrength = 1f;

        [Min(0f)]
        public float effectSpeed = 1f;

        [Range(0f, 1f)]
        public float overallOpacity = 1f;

        [Min(0f)]
        public float overallBrightness = 1f;

        [Min(0.001f)]
        public float scanlineWidth = 0.1f;

        [Min(0f)]
        public float scanlineSpeed = 1f;

        [Min(0f)]
        public float scanlineDensity = 12f;

        [Range(0f, 1f)]
        public float scanlineOpacityMin = 0.86f;

        [Range(0f, 1f)]
        public float scanlineOpacityWave = 0.85f;

        [Range(0f, 1f)]
        public float scanlineBrightnessMin = 0.78f;

        [Range(0f, 1f)]
        public float scanlineBrightnessWave = 0.82f;

        public bool autoScanBounds = true;

        public float scanTopWorldY = 1.15f;
        public float scanBottomWorldY = -1.85f;

        public float scanTopOffset = 1.15f;
        public float scanBottomOffset = -1.85f;

        void OnEnable()
        {
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }

        void Update()
        {
            Apply();
        }

        void Apply()
        {
            float top = autoScanBounds ? transform.position.y + scanTopOffset : scanTopWorldY;
            float bottom = autoScanBounds ? transform.position.y + scanBottomOffset : scanBottomWorldY;

            Shader.SetGlobalFloat(EffectStrengthId, effectStrength);
            Shader.SetGlobalFloat(EffectSpeedId, effectSpeed);
            Shader.SetGlobalFloat(ScanTopId, top);
            Shader.SetGlobalFloat(ScanBottomId, bottom);
            Shader.SetGlobalFloat(OverallOpacityId, overallOpacity);
            Shader.SetGlobalFloat(OverallBrightnessId, overallBrightness);
            Shader.SetGlobalFloat(ScanlineWidthId, scanlineWidth);
            Shader.SetGlobalFloat(ScanlineSpeedId, scanlineSpeed);
            Shader.SetGlobalFloat(ScanlineDensityId, scanlineDensity);
            Shader.SetGlobalFloat(ScanlineOpacityMinId, scanlineOpacityMin);
            Shader.SetGlobalFloat(ScanlineOpacityWaveId, scanlineOpacityWave);
            Shader.SetGlobalFloat(ScanlineBrightnessMinId, scanlineBrightnessMin);
            Shader.SetGlobalFloat(ScanlineBrightnessWaveId, scanlineBrightnessWave);
        }
    }
}
