// SPDX-License-Identifier: MIT
Shader "Gaussian Splatting/Render Splats CRT"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZWrite Off
            Blend OneMinusDstAlpha One
            Cull Off

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma require compute
#pragma use_dxc

#include "Packages/org.nesnausk.gaussian-splatting/Shaders/GaussianSplatting.hlsl"

StructuredBuffer<uint> _OrderBuffer;
StructuredBuffer<SplatViewData> _SplatViewData;
ByteAddressBuffer _SplatSelectedBits;
uint _SplatBitsValid;

float _CRTEffectStrength;
float _CRTWarpStrength;
float _CRTRowJitterStrength;
float _CRTFrameJitterStrength;
float _CRTScanlineSpeed;
float _CRTScanlineDensity;
float _CRTGrilleDensity;
float _CRTGrilleStrength;
float _CRTVignetteStrength;
float _CRTFlickerStrength;
float _CRTPhosphorBlend;
float _CRTMonitorMaskStrength;

float Hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float2 CRTWarp(float2 clipPos, float seed, float effectTime)
{
    float2 p = clipPos;
    float r2 = dot(p, p);
    p *= 1.0 + _CRTWarpStrength * r2;

    float row = floor((p.y * 0.5 + 0.5) * 140.0);
    float rowJitter = Hash11(row + floor(effectTime * 24.0) + seed * 0.37) - 0.5;
    float frameJitter = Hash11(floor(effectTime * 12.0) + seed * 1.91) - 0.5;
    p.x += rowJitter * _CRTRowJitterStrength;
    p.y += frameJitter * _CRTFrameJitterStrength;

    float edgeFade = smoothstep(1.3, 0.2, length(p));
    p *= lerp(1.02, 0.96, edgeFade);
    return p;
}

struct v2f
{
    half4 col : COLOR0;
    float2 pos : TEXCOORD0;
    float2 screenUv : TEXCOORD1;
    float monitorMask : TEXCOORD2;
    float4 vertex : SV_POSITION;
};

v2f vert (uint vtxID : SV_VertexID, uint instID : SV_InstanceID)
{
    v2f o = (v2f)0;
    instID = _OrderBuffer[instID];
    SplatViewData view = _SplatViewData[instID];
    float4 centerClipPos = view.pos;
    bool behindCam = centerClipPos.w <= 0;
    if (behindCam)
    {
        o.vertex = asfloat(0x7fc00000);
    }
    else
    {
        o.col.r = f16tof32(view.color.x >> 16);
        o.col.g = f16tof32(view.color.x);
        o.col.b = f16tof32(view.color.y >> 16);
        o.col.a = f16tof32(view.color.y);

        uint idx = vtxID;
        float2 quadPos = float2(idx & 1, (idx >> 1) & 1) * 2.0 - 1.0;
        quadPos *= 2.0;
        o.pos = quadPos;

        float2 deltaScreenPos = (quadPos.x * view.axis1 + quadPos.y * view.axis2) * 2.0 / _ScreenParams.xy;
        float4 clipPos = centerClipPos;
        clipPos.xy += deltaScreenPos * centerClipPos.w;

        float2 ndc = clipPos.xy / max(clipPos.w, 1e-5);
        float2 warpedNdc = lerp(ndc, CRTWarp(ndc, instID, _Time.y), saturate(_CRTEffectStrength));
        clipPos.xy = warpedNdc * clipPos.w;
        o.vertex = clipPos;
        o.screenUv = warpedNdc * 0.5 + 0.5;
        o.monitorMask = lerp(1.0, smoothstep(1.15, 0.65, length(warpedNdc)), saturate(_CRTMonitorMaskStrength));

        if (_SplatBitsValid)
        {
            uint wordIdx = instID / 32;
            uint bitIdx = instID & 31;
            uint selVal = _SplatSelectedBits.Load(wordIdx * 4);
            if (selVal & (1 << bitIdx))
            {
                o.col.a = -1;
            }
        }
    }
    FlipProjectionIfBackbuffer(o.vertex);
    return o;
}

half4 frag (v2f i) : SV_Target
{
    float power = -dot(i.pos, i.pos);
    half alpha = exp(power);
    if (i.col.a >= 0)
    {
        alpha = saturate(alpha * i.col.a);
    }
    else
    {
        half3 selectedColor = half3(1, 0, 1);
        if (alpha > 7.0 / 255.0)
        {
            if (alpha < 10.0 / 255.0)
            {
                alpha = 1;
                i.col.rgb = selectedColor;
            }
            alpha = saturate(alpha + 0.3);
        }
        i.col.rgb = lerp(i.col.rgb, selectedColor, 0.5);
    }

    if (alpha < 1.0 / 255.0)
        discard;

    float2 suv = saturate(i.screenUv);
    float effectStrength = saturate(_CRTEffectStrength);
    float scanline = lerp(1.0, 0.82 + 0.18 * sin(suv.y * _CRTScanlineDensity - _Time.y * _CRTScanlineSpeed), effectStrength);
    float grille = lerp(1.0, 1.0 - _CRTGrilleStrength + _CRTGrilleStrength * (0.5 + 0.5 * sin(suv.x * _CRTGrilleDensity + _Time.y * 2.5)), effectStrength);
    float vignette = lerp(1.0, smoothstep(1.05, 0.15, length(suv * 2.0 - 1.0)), _CRTVignetteStrength * effectStrength);
    float flicker = lerp(1.0, 0.96 + 0.2 * sin(_Time.y * 10.0 + suv.y * 12.0), _CRTFlickerStrength * effectStrength);

    float luma = dot(i.col.rgb, float3(0.299, 0.587, 0.114));
    half3 phosphor = half3(0.35, 1.0, 0.55) * (0.25 + 0.9 * luma);
    i.col.rgb = lerp(i.col.rgb, phosphor, _CRTPhosphorBlend * effectStrength);

    alpha *= i.monitorMask * vignette;
    i.col.rgb *= alpha * scanline * grille * flicker;

    return half4(i.col.rgb, alpha);
}
ENDCG
        }
    }
}
