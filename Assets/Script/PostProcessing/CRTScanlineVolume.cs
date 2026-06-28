using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.PostProcessing
{
    [Serializable]
    [VolumeComponentMenu("Custom/CRT Scanline")]
    public sealed class CRTScanlineVolume : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter effectStrength = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter warpStrength = new ClampedFloatParameter(0.12f, 0f, 0.5f);
        public ClampedFloatParameter rowJitterStrength = new ClampedFloatParameter(0.003f, 0f, 0.05f);
        public ClampedFloatParameter frameJitterStrength = new ClampedFloatParameter(0.0015f, 0f, 0.05f);
        public ClampedFloatParameter scanlineSpeed = new ClampedFloatParameter(12f, 0f, 80f);
        public ClampedFloatParameter scanlineDensity = new ClampedFloatParameter(900f, 100f, 3000f);
        public ClampedFloatParameter grilleDensity = new ClampedFloatParameter(2400f, 100f, 5000f);
        public ClampedFloatParameter grilleStrength = new ClampedFloatParameter(0.12f, 0f, 1f);
        public ClampedFloatParameter vignetteStrength = new ClampedFloatParameter(0.45f, 0f, 1f);
        public ClampedFloatParameter flickerStrength = new ClampedFloatParameter(0.06f, 0f, 1f);
        public ClampedFloatParameter phosphorBlend = new ClampedFloatParameter(0.2f, 0f, 1f);
        public ClampedFloatParameter monitorMaskStrength = new ClampedFloatParameter(0.5f, 0f, 1f);

        public bool IsActive()
        {
            return active &&
                   effectStrength.value > 0f &&
                   (warpStrength.value > 0f ||
                    rowJitterStrength.value > 0f ||
                    frameJitterStrength.value > 0f ||
                    grilleStrength.value > 0f ||
                    vignetteStrength.value > 0f ||
                    flickerStrength.value > 0f ||
                    phosphorBlend.value > 0f ||
                    monitorMaskStrength.value > 0f);
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
