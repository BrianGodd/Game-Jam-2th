using UnityEngine;

namespace VTuber.GSShader
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GaussianCRTEffectController : MonoBehaviour
    {
        static readonly int EffectStrengthId = Shader.PropertyToID("_CRTEffectStrength");
        static readonly int WarpStrengthId = Shader.PropertyToID("_CRTWarpStrength");
        static readonly int RowJitterStrengthId = Shader.PropertyToID("_CRTRowJitterStrength");
        static readonly int FrameJitterStrengthId = Shader.PropertyToID("_CRTFrameJitterStrength");
        static readonly int ScanlineSpeedId = Shader.PropertyToID("_CRTScanlineSpeed");
        static readonly int ScanlineDensityId = Shader.PropertyToID("_CRTScanlineDensity");
        static readonly int GrilleDensityId = Shader.PropertyToID("_CRTGrilleDensity");
        static readonly int GrilleStrengthId = Shader.PropertyToID("_CRTGrilleStrength");
        static readonly int VignetteStrengthId = Shader.PropertyToID("_CRTVignetteStrength");
        static readonly int FlickerStrengthId = Shader.PropertyToID("_CRTFlickerStrength");
        static readonly int PhosphorBlendId = Shader.PropertyToID("_CRTPhosphorBlend");
        static readonly int MonitorMaskStrengthId = Shader.PropertyToID("_CRTMonitorMaskStrength");

        [Range(0f, 1f)]
        public float effectStrength = 1f;

        [Min(0f)]
        public float warpStrength = 0.095f;

        [Min(0f)]
        public float rowJitterStrength = 0.01f;

        [Min(0f)]
        public float frameJitterStrength = 0.004f;

        [Min(0f)]
        public float scanlineSpeed = 1f;

        [Min(0f)]
        public float scanlineDensity = 900f;

        [Min(0f)]
        public float grilleDensity = 1500f;

        [Range(0f, 1f)]
        public float grilleStrength = 0.08f;

        [Range(0f, 1f)]
        public float vignetteStrength = 1f;

        [Range(0f, 1f)]
        public float flickerStrength = 1f;

        [Range(0f, 1f)]
        public float phosphorBlend = 0.88f;

        [Range(0f, 1f)]
        public float monitorMaskStrength = 1f;

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
            Shader.SetGlobalFloat(EffectStrengthId, effectStrength);
            Shader.SetGlobalFloat(WarpStrengthId, warpStrength);
            Shader.SetGlobalFloat(RowJitterStrengthId, rowJitterStrength);
            Shader.SetGlobalFloat(FrameJitterStrengthId, frameJitterStrength);
            Shader.SetGlobalFloat(ScanlineSpeedId, scanlineSpeed);
            Shader.SetGlobalFloat(ScanlineDensityId, scanlineDensity);
            Shader.SetGlobalFloat(GrilleDensityId, grilleDensity);
            Shader.SetGlobalFloat(GrilleStrengthId, grilleStrength);
            Shader.SetGlobalFloat(VignetteStrengthId, vignetteStrength);
            Shader.SetGlobalFloat(FlickerStrengthId, flickerStrength);
            Shader.SetGlobalFloat(PhosphorBlendId, phosphorBlend);
            Shader.SetGlobalFloat(MonitorMaskStrengthId, monitorMaskStrength);
        }
    }
}
