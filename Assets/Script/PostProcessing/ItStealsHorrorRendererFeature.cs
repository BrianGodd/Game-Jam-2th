using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.PostProcessing
{
    public sealed class ItStealsHorrorRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class Settings
        {
            public Shader shader;
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        sealed class ItStealsHorrorPass : ScriptableRenderPass
        {
            static readonly int PixelResolutionXId = Shader.PropertyToID("_PixelResolutionX");
            static readonly int PixelResolutionYId = Shader.PropertyToID("_PixelResolutionY");
            static readonly int PosterizeStepsId = Shader.PropertyToID("_PosterizeSteps");
            static readonly int ContrastId = Shader.PropertyToID("_Contrast");
            static readonly int BlackCrushId = Shader.PropertyToID("_BlackCrush");
            static readonly int GammaId = Shader.PropertyToID("_Gamma");
            static readonly int BlueTintStrengthId = Shader.PropertyToID("_BlueTintStrength");
            static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
            static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
            static readonly int VignetteRadiusId = Shader.PropertyToID("_VignetteRadius");
            static readonly int BloomStrengthId = Shader.PropertyToID("_BloomStrength");
            static readonly int BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
            static readonly int UseLumaPosterizeId = Shader.PropertyToID("_UseLumaPosterize");
            static readonly int UseAnimatedNoiseId = Shader.PropertyToID("_UseAnimatedNoise");

            readonly Material material;
            readonly ProfilingSampler profilingSampler = new ProfilingSampler("It Steals Horror");

            RTHandle source;
            RTHandle temporaryColorTexture;

            public ItStealsHorrorPass(Material material)
            {
                this.material = material;
                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            public void Setup(RTHandle sourceHandle)
            {
                source = sourceHandle;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(
                    ref temporaryColorTexture,
                    descriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_ItStealsHorrorTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || source == null)
                {
                    return;
                }

                ref CameraData cameraData = ref renderingData.cameraData;
                if (!cameraData.postProcessEnabled ||
                    cameraData.isPreviewCamera ||
                    cameraData.cameraType == CameraType.Reflection)
                {
                    return;
                }

                ItStealsHorrorVolume volume = VolumeManager.instance.stack.GetComponent<ItStealsHorrorVolume>();
                if (volume == null || !volume.IsActive())
                {
                    return;
                }

                UpdateMaterialProperties(volume);

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    Blitter.BlitCameraTexture(cmd, source, temporaryColorTexture, material, 0);
                    Blitter.BlitCameraTexture(cmd, temporaryColorTexture, source);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                temporaryColorTexture?.Release();
            }

            void UpdateMaterialProperties(ItStealsHorrorVolume volume)
            {
                material.SetFloat(PixelResolutionXId, volume.pixelResolutionX.value);
                material.SetFloat(PixelResolutionYId, volume.pixelResolutionY.value);
                material.SetFloat(PosterizeStepsId, volume.posterizeSteps.value);
                material.SetFloat(ContrastId, volume.contrast.value);
                material.SetFloat(BlackCrushId, volume.blackCrush.value);
                material.SetFloat(GammaId, volume.gamma.value);
                material.SetFloat(BlueTintStrengthId, volume.blueTintStrength.value);
                material.SetFloat(NoiseStrengthId, volume.noiseStrength.value);
                material.SetFloat(VignetteStrengthId, volume.vignetteStrength.value);
                material.SetFloat(VignetteRadiusId, volume.vignetteRadius.value);
                material.SetFloat(BloomStrengthId, volume.bloomStrength.value);
                material.SetFloat(BloomThresholdId, volume.bloomThreshold.value);
                material.SetFloat(UseLumaPosterizeId, volume.useLumaPosterize.value ? 1f : 0f);
                material.SetFloat(UseAnimatedNoiseId, volume.useAnimatedNoise.value ? 1f : 0f);
            }
        }

        public Settings settings = new Settings();

        Material material;
        ItStealsHorrorPass pass;

        public override void Create()
        {
            DisposeMaterial();

            Shader shader = settings.shader != null
                ? settings.shader
                : Shader.Find("Hidden/GameJam/PostProcessing/ItStealsHorror");

            if (shader == null)
            {
                Debug.LogWarning("It Steals Horror Renderer Feature could not find its shader.");
                return;
            }

            material = CoreUtils.CreateEngineMaterial(shader);
            pass = new ItStealsHorrorPass(material)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (pass == null)
            {
                return;
            }

            pass.Setup(renderer.cameraColorTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || material == null)
            {
                return;
            }

            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;
            DisposeMaterial();
        }

        void DisposeMaterial()
        {
            if (material != null)
            {
                CoreUtils.Destroy(material);
                material = null;
            }
        }
    }
}
