using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.PostProcessing
{
    public sealed class CRTScanlineRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class Settings
        {
            public Shader shader;
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        sealed class CRTScanlinePass : ScriptableRenderPass
        {
            static readonly int EffectStrengthId = Shader.PropertyToID("_EffectStrength");
            static readonly int WarpStrengthId = Shader.PropertyToID("_WarpStrength");
            static readonly int RowJitterStrengthId = Shader.PropertyToID("_RowJitterStrength");
            static readonly int FrameJitterStrengthId = Shader.PropertyToID("_FrameJitterStrength");
            static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
            static readonly int ScanlineDensityId = Shader.PropertyToID("_ScanlineDensity");
            static readonly int GrilleDensityId = Shader.PropertyToID("_GrilleDensity");
            static readonly int GrilleStrengthId = Shader.PropertyToID("_GrilleStrength");
            static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
            static readonly int FlickerStrengthId = Shader.PropertyToID("_FlickerStrength");
            static readonly int PhosphorBlendId = Shader.PropertyToID("_PhosphorBlend");
            static readonly int MonitorMaskStrengthId = Shader.PropertyToID("_MonitorMaskStrength");

            readonly Material material;
            readonly ProfilingSampler profilingSampler = new ProfilingSampler("CRT Scanline");

            RTHandle source;
            RTHandle temporaryColorTexture;

            public CRTScanlinePass(Material material)
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
                    name: "_CRTScanlineTemp");
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

                CRTScanlineVolume volume = VolumeManager.instance.stack.GetComponent<CRTScanlineVolume>();
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

            void UpdateMaterialProperties(CRTScanlineVolume volume)
            {
                material.SetFloat(EffectStrengthId, volume.effectStrength.value);
                material.SetFloat(WarpStrengthId, volume.warpStrength.value);
                material.SetFloat(RowJitterStrengthId, volume.rowJitterStrength.value);
                material.SetFloat(FrameJitterStrengthId, volume.frameJitterStrength.value);
                material.SetFloat(ScanlineSpeedId, volume.scanlineSpeed.value);
                material.SetFloat(ScanlineDensityId, volume.scanlineDensity.value);
                material.SetFloat(GrilleDensityId, volume.grilleDensity.value);
                material.SetFloat(GrilleStrengthId, volume.grilleStrength.value);
                material.SetFloat(VignetteStrengthId, volume.vignetteStrength.value);
                material.SetFloat(FlickerStrengthId, volume.flickerStrength.value);
                material.SetFloat(PhosphorBlendId, volume.phosphorBlend.value);
                material.SetFloat(MonitorMaskStrengthId, volume.monitorMaskStrength.value);
            }
        }

        public Settings settings = new Settings();

        Material material;
        CRTScanlinePass pass;

        public override void Create()
        {
            DisposeMaterial();

            Shader shader = settings.shader != null
                ? settings.shader
                : Shader.Find("Hidden/GameJam/PostProcessing/CRTScanline");

            if (shader == null)
            {
                Debug.LogWarning("CRT Scanline Renderer Feature could not find its shader.");
                return;
            }

            material = CoreUtils.CreateEngineMaterial(shader);
            pass = new CRTScanlinePass(material)
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
