Shader "Hidden/GameJam/PostProcessing/ItStealsHorror"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ItStealsHorror"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelResolutionX;
            float _PixelResolutionY;
            float _PosterizeSteps;
            float _Contrast;
            float _BlackCrush;
            float _Gamma;
            float _BlueTintStrength;
            float _NoiseStrength;
            float _VignetteStrength;
            float _VignetteRadius;
            float _BloomStrength;
            float _BloomThreshold;
            float _UseLumaPosterize;
            float _UseAnimatedNoise;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float3 SampleScene(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 pixelCount = max(float2(_PixelResolutionX, _PixelResolutionY), 1.0);
                float2 pixelUV = floor(uv * pixelCount) / pixelCount;

                float3 color = SampleScene(pixelUV);

                float3 bloom = 0.0;
                if (_BloomStrength > 0.001)
                {
                    float2 texel = 1.0 / pixelCount;
                    const float2 offsets[8] =
                    {
                        float2(1.0, 0.0), float2(-1.0, 0.0), float2(0.0, 1.0), float2(0.0, -1.0),
                        float2(1.0, 1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(-1.0, -1.0)
                    };

                    [unroll]
                    for (int i = 0; i < 8; i++)
                    {
                        float3 sampleColor = SampleScene(saturate(pixelUV + offsets[i] * texel * 2.0));
                        bloom += max(sampleColor - _BloomThreshold, 0.0);
                    }

                    bloom /= 8.0;
                }

                float luma = dot(color, float3(0.299, 0.587, 0.114));
                float3 shadowTint = float3(0.02, 0.035, 0.08);
                float3 midTint = float3(0.12, 0.22, 0.45);
                float3 highTint = float3(0.55, 0.85, 1.0);

                float3 graded = color;
                graded *= lerp(shadowTint, midTint, smoothstep(0.02, 0.45, luma)) * 1.8;
                graded = lerp(graded, graded * highTint * 1.25, smoothstep(0.45, 1.0, luma));
                color = lerp(color, graded, _BlueTintStrength);

                color += bloom * _BloomStrength * float3(0.5, 0.8, 1.0);
                color = saturate((color - _BlackCrush) * _Contrast);
                color = pow(max(color, 0.0001), _Gamma);

                float steps = max(_PosterizeSteps, 2.0);
                if (_UseLumaPosterize > 0.5)
                {
                    float lum = dot(color, float3(0.299, 0.587, 0.114));
                    float quantized = floor(lum * steps) / steps;
                    color *= quantized / max(lum, 0.001);
                }
                else
                {
                    color = floor(color * steps) / steps;
                }

                color = saturate(color);

                float2 noiseUV = uv * _ScreenParams.xy;
                if (_UseAnimatedNoise > 0.5)
                {
                    noiseUV += _Time.y * 60.0;
                }

                float noise = Hash21(floor(noiseUV));
                color += (noise - 0.5) * _NoiseStrength;
                color = saturate(color);

                float2 centered = uv - 0.5;
                float dist = length(centered);
                float vignette = 1.0 - smoothstep(_VignetteRadius - _VignetteStrength, _VignetteRadius, dist);
                color *= vignette;

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
