using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.PostProcessing
{
    [Serializable]
    [VolumeComponentMenu("Custom/It Steals Horror")]
    public sealed class ItStealsHorrorVolume : VolumeComponent, IPostProcessComponent
    {
        public ClampedIntParameter pixelResolutionX = new ClampedIntParameter(320, 160, 640);
        public ClampedIntParameter pixelResolutionY = new ClampedIntParameter(180, 90, 360);
        public ClampedFloatParameter posterizeSteps = new ClampedFloatParameter(7f, 3f, 16f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.35f, 0.8f, 3f);
        public ClampedFloatParameter blackCrush = new ClampedFloatParameter(0.035f, 0f, 0.2f);
        public ClampedFloatParameter gamma = new ClampedFloatParameter(1.25f, 0.5f, 2.5f);
        public ClampedFloatParameter blueTintStrength = new ClampedFloatParameter(0.75f, 0f, 1f);
        public ClampedFloatParameter noiseStrength = new ClampedFloatParameter(0.02f, 0f, 0.1f);
        public ClampedFloatParameter vignetteStrength = new ClampedFloatParameter(0.55f, 0f, 1f);
        public ClampedFloatParameter vignetteRadius = new ClampedFloatParameter(0.85f, 0.1f, 1.5f);
        public ClampedFloatParameter bloomStrength = new ClampedFloatParameter(0.15f, 0f, 2f);
        public ClampedFloatParameter bloomThreshold = new ClampedFloatParameter(0.65f, 0f, 2f);
        public BoolParameter useLumaPosterize = new BoolParameter(true);
        public BoolParameter useAnimatedNoise = new BoolParameter(true);

        public bool IsActive()
        {
            return active &&
                   pixelResolutionX.value > 0 &&
                   pixelResolutionY.value > 0 &&
                   (blueTintStrength.value > 0f ||
                    noiseStrength.value > 0f ||
                    vignetteStrength.value > 0f ||
                    bloomStrength.value > 0f ||
                    Mathf.Abs(contrast.value - 1f) > 0.001f ||
                    blackCrush.value > 0f ||
                    Mathf.Abs(gamma.value - 1f) > 0.001f ||
                    posterizeSteps.value < 255f);
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
