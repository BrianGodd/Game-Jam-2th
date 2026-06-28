Shader "Hidden/GameJam/PostProcessing/CRTScanline"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "CRTScanline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _EffectStrength;
            float _WarpStrength;
            float _RowJitterStrength;
            float _FrameJitterStrength;
            float _ScanlineSpeed;
            float _ScanlineDensity;
            float _GrilleDensity;
            float _GrilleStrength;
            float _VignetteStrength;
            float _FlickerStrength;
            float _PhosphorBlend;
            float _MonitorMaskStrength;

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float2 WarpUv(float2 uv, float effectTime)
            {
                float2 ndc = uv * 2.0 - 1.0;
                float2 warped = ndc;
                float radiusSq = dot(warped, warped);
                warped *= 1.0 + _WarpStrength * radiusSq;

                float row = floor((warped.y * 0.5 + 0.5) * 140.0);
                float rowJitter = Hash11(row + floor(effectTime * 24.0)) - 0.5;
                float frameJitter = Hash11(floor(effectTime * 12.0) + 1.91) - 0.5;
                warped.x += rowJitter * _RowJitterStrength;
                warped.y += frameJitter * _FrameJitterStrength;

                float edgeFade = smoothstep(1.3, 0.2, length(warped));
                warped *= lerp(1.02, 0.96, edgeFade);
                return warped * 0.5 + 0.5;
            }

            float MonitorMask(float2 uv)
            {
                float2 ndc = uv * 2.0 - 1.0;
                return lerp(1.0, smoothstep(1.15, 0.65, length(ndc)), saturate(_MonitorMaskStrength));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float effectStrength = saturate(_EffectStrength);
                float2 baseUv = input.texcoord;
                float2 warpedUv = WarpUv(baseUv, _Time.y);
                float2 sampleUv = lerp(baseUv, warpedUv, effectStrength);

                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUv).rgb;

                float scanline = lerp(
                    1.0,
                    0.82 + 0.18 * sin(sampleUv.y * _ScanlineDensity - _Time.y * _ScanlineSpeed),
                    effectStrength);
                float grille = lerp(
                    1.0,
                    1.0 - _GrilleStrength + _GrilleStrength * (0.5 + 0.5 * sin(sampleUv.x * _GrilleDensity + _Time.y * 2.5)),
                    effectStrength);
                float vignette = lerp(
                    1.0,
                    smoothstep(1.05, 0.15, length(sampleUv * 2.0 - 1.0)),
                    _VignetteStrength * effectStrength);
                float flicker = lerp(
                    1.0,
                    0.96 + 0.2 * sin(_Time.y * 10.0 + sampleUv.y * 12.0),
                    _FlickerStrength * effectStrength);

                float luma = dot(color, float3(0.299, 0.587, 0.114));
                half3 phosphor = half3(0.35, 1.0, 0.55) * (0.25 + 0.9 * luma);
                color = lerp(color, phosphor, _PhosphorBlend * effectStrength);

                float monitorMask = MonitorMask(sampleUv);
                color *= scanline * grille * flicker * vignette * monitorMask;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}
