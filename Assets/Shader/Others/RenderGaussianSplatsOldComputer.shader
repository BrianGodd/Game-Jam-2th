// SPDX-License-Identifier: MIT
Shader "Gaussian Splatting/Render Splats Old Computer"
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

static const float OLD_COMPUTER_LOOP_RATE = 0.18;
static const float OLD_COMPUTER_BAND_MIN = 0.06;
static const float OLD_COMPUTER_BAND_MAX = 0.36;
static const float OLD_COMPUTER_ALPHA_DROP_MIN = 0.75;
static const float OLD_COMPUTER_ALPHA_DROP_MAX = 0.96;

float _OldComputerEffectStrength;
float _OldComputerEffectSpeed;
float _OldComputerScanTop;
float _OldComputerScanBottom;
float _OldComputerOverallOpacity;
float _OldComputerOverallBrightness;
float _OldComputerScanlineWidth;
float _OldComputerScanlineSpeed;
float _OldComputerScanlineDensity;
float _OldComputerScanlineOpacityMin;
float _OldComputerScanlineOpacityWave;
float _OldComputerScanlineBrightnessMin;
float _OldComputerScanlineBrightnessWave;

float OldComputerHash13(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
}

float OldComputerStrength01()
{
    return saturate(_OldComputerEffectStrength);
}

float OldComputerBandStrength()
{
    return saturate(_OldComputerEffectStrength * 0.5);
}

float OldComputerSweepProgress(float effectTime)
{
    return frac(effectTime * _OldComputerEffectSpeed * OLD_COMPUTER_LOOP_RATE);
}

float OldComputerScanY(float effectTime)
{
    return lerp(_OldComputerScanTop, _OldComputerScanBottom, OldComputerSweepProgress(effectTime));
}

float OldComputerBandWidth()
{
    return lerp(OLD_COMPUTER_BAND_MIN, OLD_COMPUTER_BAND_MAX, OldComputerBandStrength());
}

float OldComputerScanHit(float3 centerWorld, float effectTime)
{
    float scanY = OldComputerScanY(effectTime);
    float bandWidth = OldComputerBandWidth();
    float distanceToScan = abs(centerWorld.y - scanY);
    return 1.0 - smoothstep(0.0, bandWidth, distanceToScan);
}

float OldComputerQuantizedTime(float effectTime)
{
    return floor(effectTime * _OldComputerEffectSpeed * 18.0) * 0.055;
}

float OldComputerScanlineWave(float3 centerWorld, float effectTime)
{
    float scanlineWidth = max(_OldComputerScanlineWidth, 0.001);
    float scanlineDensity = rcp(scanlineWidth) * max(_OldComputerScanlineDensity, 0.0);
    float scanlinePhase = centerWorld.y * scanlineDensity + effectTime * _OldComputerScanlineSpeed * 14.0;
    return (sin(scanlinePhase) + 1.0) * 0.5;
}

float3 OldComputerWorldOffset(float3 centerWorld, float effectTime)
{
    float strength01 = OldComputerStrength01();
    float quantizedTime = OldComputerQuantizedTime(effectTime);
    float rowNoise = OldComputerHash13(
        centerWorld * float3(0.0, 9.5, 0.0) +
        quantizedTime * float3(0.0, 1.0, 0.0) +
        float3(0.0, 0.0, 1.7)
    ) - 0.5;
    float lineJitterMask = strength01 * smoothstep(
        0.12,
        0.95,
        OldComputerHash13(
            centerWorld * float3(0.0, 7.0, 0.0) +
            quantizedTime * float3(0.0, 1.0, 0.0) +
            float3(0.0, 0.0, 3.9)
        )
    );
    float lineJitter = rowNoise * lineJitterMask * 0.045;
    return float3(lineJitter, 0.0, 0.0);
}

half OldComputerApplyOpacity(float3 centerWorld, half opacity, float effectTime)
{
    float strength01 = OldComputerStrength01();
    float scanHit = OldComputerScanHit(centerWorld, effectTime);
    float alphaDrop = lerp(
        OLD_COMPUTER_ALPHA_DROP_MIN,
        OLD_COMPUTER_ALPHA_DROP_MAX,
        strength01
    );
    float scanlineWave = OldComputerScanlineWave(centerWorld, effectTime);
    float scanlineOpacity = lerp(_OldComputerScanlineOpacityMin, 1.0, scanlineWave * _OldComputerScanlineOpacityWave);
    return opacity * (1.0 - scanHit * alphaDrop) * scanlineOpacity;
}

half3 OldComputerApplyColor(float3 centerWorld, half3 baseColor, float effectTime)
{
    half3 phosphorTint = half3(0.96, 1.0, 0.95);
    half3 crtShadowTint = half3(0.88, 0.92, 0.89);
    half3 phosphorLift = half3(0.01, 0.025, 0.01);
    float strength01 = OldComputerStrength01();
    float scanHit = OldComputerScanHit(centerWorld, effectTime);
    float scanlineWave = OldComputerScanlineWave(centerWorld, effectTime);
    float scanlineAmount = lerp(_OldComputerScanlineBrightnessMin, 1.0, scanlineWave * _OldComputerScanlineBrightnessWave);
    float staticNoise = OldComputerHash13(
        centerWorld * float3(28.0, 36.0, 0.0) +
        (_Time.y * _OldComputerEffectSpeed * 9.0) * float3(0.0, 0.0, 1.0) +
        float3(0.0, 0.0, 0.73)
    ) - 0.5;
    float staticAmount = 1.0 + staticNoise * (strength01 * 0.16);
    float scanGlow = scanHit * (0.35 + scanlineWave * 0.45);
    half3 darkPass = baseColor * (crtShadowTint * scanlineAmount);
    half3 glowPass = phosphorTint * staticAmount;
    return lerp(darkPass, glowPass, scanGlow * 0.6) + phosphorLift * (scanGlow * (strength01 * 0.55));
}

struct v2f
{
    half4 col : COLOR0;
    float2 pos : TEXCOORD0;
    float3 centerWorld : TEXCOORD1;
    float4 vertex : SV_POSITION;
};

StructuredBuffer<SplatViewData> _SplatViewData;
ByteAddressBuffer _SplatSelectedBits;
uint _SplatBitsValid;

v2f vert (uint vtxID : SV_VertexID, uint instID : SV_InstanceID)
{
    v2f o = (v2f)0;
    instID = _OrderBuffer[instID];
    SplatViewData view = _SplatViewData[instID];
    float3 centerObjectPos = LoadSplatPos(instID);
    float3 centerWorldPos = mul(unity_ObjectToWorld, float4(centerObjectPos, 1.0)).xyz;
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

        float effectTime = _Time.y;
        float3 jitteredWorldPos = centerWorldPos + OldComputerWorldOffset(centerWorldPos, effectTime);
        float4 jitteredCenterClipPos = mul(UNITY_MATRIX_VP, float4(jitteredWorldPos, 1.0));

        uint idx = vtxID;
        float2 quadPos = float2(idx & 1, (idx >> 1) & 1) * 2.0 - 1.0;
        quadPos *= 2.0;

        o.pos = quadPos;
        o.centerWorld = centerWorldPos;

        float2 deltaScreenPos = (quadPos.x * view.axis1 + quadPos.y * view.axis2) * 2.0 / _ScreenParams.xy;
        o.vertex = jitteredCenterClipPos;
        o.vertex.xy += deltaScreenPos * jitteredCenterClipPos.w;

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
        i.col.a = OldComputerApplyOpacity(i.centerWorld, alpha, _Time.y);
        i.col.rgb = OldComputerApplyColor(i.centerWorld, i.col.rgb, _Time.y);
        alpha = saturate(i.col.a * _OldComputerOverallOpacity);
        i.col.rgb *= _OldComputerOverallBrightness;
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

    return half4(i.col.rgb * alpha, alpha);
}
ENDCG
        }
    }
}
