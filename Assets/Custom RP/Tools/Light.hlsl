#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#if defined (_DIRECTIONAL_PCSS)
#elif defined(_DIRECTIONAL_PCF3)
#define DIRECTIONAL_FILTER_SAMPLES 4
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
#define DIRECTIONAL_FILTER_SAMPLES 9
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
#define DIRECTIONAL_FILTER_SAMPLES 16
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_LIGHT_COUNT 64
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_POINT_LIGHT_COUNT 60
#define MAX_SPOT_LIGHT_COUNT 10
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
#define NUM_SAMPLE 64
#include <HLSLSupport.cginc>
#include "Assets/Custom RP/Tools/CommonTools.hlsl"
#include "Assets/Custom RP/Tools/Material.hlsl"

TEXTURE2D(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER(SHADOW_SAMPLER);

TEXTURE2D(_OtherShadowAtlas);
SAMPLER(sampler_OtherShadowAtlas);

CBUFFER_START(_CustomLight)
int _LightCount;
float _MaxShadowDistance;
float _Fade;
float _SampleBlockerDepthRadius;
float _LightWidth;
float4 _ShadowMapResolution;
// Directional Light
int _DirectionalLightCount;
int _DirectionalLightCascadeCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirection[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalCascadeSphere[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
float4x4 _TransformWorldToShadowMapMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
// Point Light
int _PointLightCount;
float4 _PointLightColors[MAX_POINT_LIGHT_COUNT];
float4 _PointLightPosition[MAX_POINT_LIGHT_COUNT];
float4 _PointShadowData[MAX_POINT_LIGHT_COUNT];
// Spot Light
int _SpotLightCount;
float4 _SpotLightColors[MAX_SPOT_LIGHT_COUNT];
float4 _SpotLightPosition[MAX_SPOT_LIGHT_COUNT];
float4 _SpotLightDirection[MAX_SPOT_LIGHT_COUNT];
float4 _SpotLightAngle[MAX_SPOT_LIGHT_COUNT];
float4 _SpotShadowData[MAX_SPOT_LIGHT_COUNT];
//
float4x4 _OtherTransformWorldToShadowMapMatrices[16];
CBUFFER_END

static float2 poissonDisk[NUM_SAMPLE] ={
    float2(-0.5119625f, -0.4827938f),
    float2(-0.2171264f, -0.4768726f),
    float2(-0.7552931f, -0.2426507f),
    float2(-0.7136765f, -0.4496614f),
    float2(-0.5938849f, -0.6895654f),
    float2(-0.3148003f, -0.7047654f),
    float2(-0.42215f, -0.2024607f),
    float2(-0.9466816f, -0.2014508f),
    float2(-0.8409063f, -0.03465778f),
    float2(-0.6517572f, -0.07476326f),
    float2(-0.1041822f, -0.02521214f),
    float2(-0.3042712f, -0.02195431f),
    float2(-0.5082307f, 0.1079806f),
    float2(-0.08429877f, -0.2316298f),
    float2(-0.9879128f, 0.1113683f),
    float2(-0.3859636f, 0.3363545f),
    float2(-0.1925334f, 0.1787288f),
    float2(0.003256182f, 0.138135f),
    float2(-0.8706837f, 0.3010679f),
    float2(-0.6982038f, 0.1904326f),
    float2(0.1975043f, 0.2221317f),
    float2(0.1507788f, 0.4204168f),
    float2(0.3514056f, 0.09865579f),
    float2(0.1558783f, -0.08460935f),
    float2(-0.0684978f, 0.4461993f),
    float2(0.3780522f, 0.3478679f),
    float2(0.3956799f, -0.1469177f),
    float2(0.5838975f, 0.1054943f),
    float2(0.6155105f, 0.3245716f),
    float2(0.3928624f, -0.4417621f),
    float2(0.1749884f, -0.4202175f),
    float2(0.6813727f, -0.2424808f),
    float2(-0.6707711f, 0.4912741f),
    float2(0.0005130528f, -0.8058334f),
    float2(0.02703013f, -0.6010728f),
    float2(-0.1658188f, -0.9695674f),
    float2(0.4060591f, -0.7100726f),
    float2(0.7713396f, -0.4713659f),
    float2(0.573212f, -0.51544f),
    float2(-0.3448896f, -0.9046497f),
    float2(0.1268544f, -0.9874692f),
    float2(0.7418533f, -0.6667366f),
    float2(0.3492522f, 0.5924662f),
    float2(0.5679897f, 0.5343465f),
    float2(0.5663417f, 0.7708698f),
    float2(0.7375497f, 0.6691415f),
    float2(0.2271994f, -0.6163502f),
    float2(0.2312844f, 0.8725659f),
    float2(0.4216993f, 0.9002838f),
    float2(0.4262091f, -0.9013284f),
    float2(0.2001408f, -0.808381f),
    float2(0.149394f, 0.6650763f),
    float2(-0.09640376f, 0.9843736f),
    float2(0.7682328f, -0.07273844f),
    float2(0.04146584f, 0.8313184f),
    float2(0.9705266f, -0.1143304f),
    float2(0.9670017f, 0.1293385f),
    float2(0.9015037f, -0.3306949f),
    float2(-0.5085648f, 0.7534177f),
    float2(0.9055501f, 0.3758393f),
    float2(0.7599946f, 0.1809109f),
    float2(-0.2483695f, 0.7942952f),
    float2(-0.4241052f, 0.5581087f),
    float2(-0.1020106f, 0.6724468f)
};
struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};
struct ShadowData
{
    float shadowStrength;
    int splitIndex;
    float bakedShadowStrength;
    int shadowMaskChannel;
};
struct ShadowMask
{
    bool enableShadowMask;
    float4 shadowMaskColor;
};
// private
float GetBlockerDepth(float3 positionSS, float lightWidth)
{
    float radius = _SampleBlockerDepthRadius;
    float blockerDepth = 0.0f;
    for (int i = 0;i < NUM_SAMPLE;i++)
    {
        float2 uv = positionSS.xy + poissonDisk[i] * radius;
        float depth = SAMPLE_TEXTURE2D(_DirectionalShadowAtlas, SHADOW_SAMPLER, uv);
        #if UNITY_REVERSED_Z
        blockerDepth += step(positionSS.z, depth) * (1 - depth);
        #else
        blockerDepth += step(depth, positionSS.z) * depth;
        #endif
    }
    return blockerDepth / NUM_SAMPLE;
}
float PCF(float3 positionSS, float radius)
{
    float shadowStrength = 0.0f, res = 0.0f;
    for (int i = 0; i < NUM_SAMPLE; i++)
    {
        float2 offset = poissonDisk[i] * radius;
        positionSS.xy += offset;
        shadowStrength = SAMPLE_TEXTURE2D(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSS.xy);
        // Compare shadowStrength with positionSS.z to judge whether it's in shadow
        #if UNITY_REVERSED_Z
        //the closer surface to camera, the bigger it's depth is
        shadowStrength = step(shadowStrength, positionSS.z);
        #else
        shadowStrength = step(positionSS.z, shadowStrength);
        #endif
        res += shadowStrength;
    }
    return res / NUM_SAMPLE;
}
float PCSS(float3 positionSS, float lightWidth)
{
    #if UNITY_REVERSED_Z
    //the closer surface to camera, the bigger it's depth is
    float d_receiver = 1 - positionSS.z;
    #else
    float d_receiver = positionSS.z;
    #endif
    float d_blocker = GetBlockerDepth(positionSS, lightWidth);
    if (d_receiver - d_blocker)
    {
        float radius = (lightWidth / d_blocker) * (d_receiver - d_blocker);
        return PCF(positionSS, radius);
    }
    return 0;
}
bool OutOfMaxShadowDistance(int cascadeIndex, int lightIndex, Material material)
{
    float distancePosition2Camera = dot(material.positionWS - _WorldSpaceCameraPos.xyz, material.positionWS - _WorldSpaceCameraPos.xyz);
    float maxCascadeSphereRadius = _DirectionalCascadeSphere[_DirectionalLightCascadeCount * lightIndex + MAX_CASCADE_COUNT - 1].w;
    if (cascadeIndex == MAX_CASCADE_COUNT - 1 && distancePosition2Camera > maxCascadeSphereRadius)
        return true;
    return false;
}
int GetCascadeIndex(int lightIndex, Material material)
{
    // 实现“根据cameraPosition与surfacePosition的距离选择合适的cascadeSphereIndex“
    // 但直接使用cameraPosition会出现一些奇怪的问题，所以我使用cascadeSphereCenter代替cameraPosition
    // 这就是forLoop在做的事情
    // 我还想通过cascadeIndex == MAX_CASCADE_COUNT表示cameraPosition超出了maxShadowDistance
    // 但是forLoop无法完成这个目标
    int i;
    for (i = 0; i < MAX_CASCADE_COUNT;i++)
    {
        float3 cascadeSphereCenter = _DirectionalCascadeSphere[_DirectionalLightCascadeCount * lightIndex + i].xyz;
        float cascadeSphereRadius = _DirectionalCascadeSphere[_DirectionalLightCascadeCount * lightIndex + i].w;
        float distanceSurface2SphereCenter = dot(material.positionWS - cascadeSphereCenter, material.positionWS - cascadeSphereCenter);
        // 因为当cameraPosition超过最大的cascadeSphereCenter之后，cascadeSphereCenter就不会继续变化了，即distanceSurface2SphereCenter不会继续变化。
        // 那么，如果此时的distanceSurface2SphereCenter < cascadeSphereRadius，则 forLoop会break，i，即cascadeIndex就不会等于MAX_CASCADE_COUNT。
        // 从而无法通过cascadeIndex == MAX_CASCADE_COUNT来判断camera是否超出maxShadowDistance
        if (distanceSurface2SphereCenter < cascadeSphereRadius) break;
    }
    // 但是我想保留这种判断方法，所以得想办法实现“当camera超出maxShadowDistance时，cascadeIndex == MAX_CASCADE_COUNT”
    // 所以在这里做一个特殊的判断，只在cascadeIndex == MAX_CASCADE_COUNT - 1时使用cameraPosition而不是cascadeSphereCenter
    // 如果cascadeIndex == MAX_CASCADE_COUNT - 1 && distancePosition2Camera > maxCascadeSphereRadius
    // 这就说明，此时的cameraPosition确实超出了maxShadowDistance，所以返回MAX_CASCADE_COUNT
    if (OutOfMaxShadowDistance(i, lightIndex, material)) return MAX_CASCADE_COUNT;
    return i;
}
float GetDirectionalFadeWeight (Material material)
{
    return (1 + material.positionVS.z / _MaxShadowDistance) * (1 / _Fade);
}
float GetOthersFadeWeight (Material material, float shadowDistance)
{
    return (1 + material.positionVS.z / shadowDistance) * (1 / _Fade);
}
ShadowData GetDirectionalShadowData (int index, Material material, ShadowMask mask)
{
    ShadowData data;
    int cascadeIndex = GetCascadeIndex(index, material);
    if (mask.enableShadowMask)
    {
        // If enable shadowMask, draw shadow with no fade
        data.shadowStrength = abs(_DirectionalShadowData[index].x);
        data.splitIndex = _DirectionalShadowData[index].y + cascadeIndex;
        data.bakedShadowStrength = 1 - saturate(GetDirectionalFadeWeight(material));
        data.shadowMaskChannel = _DirectionalShadowData[index].w;
    }
    else
    {
        // If disable shadowMask && Camera2SurfaceDistance is out of maxShadowDistance, don't draw shadow at all
        if (cascadeIndex == MAX_CASCADE_COUNT)
        {
            data.shadowStrength = 0;
            data.splitIndex = 0;
            data.shadowMaskChannel = 0;
            data.bakedShadowStrength = 0;
        }
        else
        {
            data.shadowStrength = abs(_DirectionalShadowData[index].x) * GetDirectionalFadeWeight(material);
            data.splitIndex = _DirectionalShadowData[index].y + cascadeIndex;
            data.bakedShadowStrength = 1 - saturate(GetDirectionalFadeWeight(material));
            data.shadowMaskChannel = _DirectionalShadowData[index].w;
        }
    }
    return data;
}
ShadowData GetPointShadowData (int index, Material material, ShadowMask mask, float disSurface2Light) {
    ShadowData data;
    // If surfaceDistance is out of shadowDistance && shadowMask is disabled
    data.shadowStrength = abs(_PointShadowData[index].x) * GetOthersFadeWeight(material, disSurface2Light * 2);
    data.splitIndex = _PointShadowData[index].y;
    // The closer camera2surface，the smaller bakedShadowStrength is, vice versa
    data.bakedShadowStrength = 1 - saturate(GetDirectionalFadeWeight(material));
    data.shadowMaskChannel = _PointShadowData[index].w;
    return data;
}
ShadowData GetSpotShadowData (int index, Material material, ShadowMask mask)
{
    ShadowData data;
    // If surfaceDistance is out of shadowDistance && shadowMask is disabled
    data.shadowStrength = abs(_SpotShadowData[index].x) * GetDirectionalFadeWeight(material);
    data.splitIndex = _SpotShadowData[index].y;
    // The closer camera2surface，the smaller bakedShadowStrength is, vice versa
    data.bakedShadowStrength = 1 - saturate(GetDirectionalFadeWeight(material));
    data.shadowMaskChannel = _SpotShadowData[index].w;
    return data;
}
float SampleShadowMap(float3 positionSS)
{
    float shadowStrength;
    // Choose sample type by PCF
    #if defined(DIRECTIONAL_FILTER_SETUP)
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowMapResolution.yyxx;
        DIRECTIONAL_FILTER_SETUP(size, positionSS.xy, weights, positions);
        for (int i = 0;i < DIRECTIONAL_FILTER_SAMPLES;i++)
        {
            float temp = SAMPLE_TEXTURE2D(_DirectionalShadowAtlas, SHADOW_SAMPLER, positions[i].xy);
            #if UNITY_REVERSED_Z
            //the closer surface to camera, the bigger it's depth is
            temp = step(temp, positionSS.z);
            #else
            temp = step(positionSS.z, temp);
            #endif
            shadowStrength += weights[i] * temp;
        }
        return shadowStrength;
    #else
    shadowStrength = SAMPLE_TEXTURE2D(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSS.xy);
    #if UNITY_REVERSED_Z
    //the closer surface to camera, the bigger it's depth is
    shadowStrength = step(shadowStrength, positionSS.z);
    #else
    shadowStrength = step(positionSS.z, shadowStrength);
    #endif
    return  shadowStrength;
    #endif
}
float GetBakedShadow(ShadowMask mask, int channel)
{
    return mask.shadowMaskColor[channel];
}
float GetDirectionalLightRealtimeShadow(ShadowData data, Material material)
{
    if (data.shadowStrength <= 0.0f) return 1.0f;
    // Extend surface along normalDirection to avoid selfShadow
    float4 positionBiasWS = float4(material.positionWS + material.normalWS, 1.0f);
    // Transform positionWS to positionShadowSpace
    float3 positionSS = mul(_TransformWorldToShadowMapMatrices[data.splitIndex], positionBiasWS).xyz;
    // Get shadowStrength
    float shadowStrength = SampleShadowMap(positionSS);
    // float shadowStrength = PCSS(positionSS, _LightWidth);
    return shadowStrength;
}
float GetDirectionalLightAttenuation(int index, Material material, ShadowMask shadowMask)
{
    ShadowData data = GetDirectionalShadowData(index, material, shadowMask);
    float realtimeShadow = GetDirectionalLightRealtimeShadow(data, material);
    float shadowStrength;
    if (shadowMask.enableShadowMask)
    {
        float bakedShadow = GetBakedShadow(shadowMask, data.shadowMaskChannel);
        shadowStrength = min(bakedShadow, lerp(realtimeShadow, bakedShadow, data.bakedShadowStrength));
    }
    else
        shadowStrength = realtimeShadow;
    return lerp(1.0, shadowStrength, data.shadowStrength);
}
float GetPointLightRealtimeShadow(int index, ShadowData data, Material material)
{
    float distance = max(0.001, length(_PointLightPosition[index].xyz - material.positionWS));
    float range = max(0.001, _PointLightPosition[index].w);
    return (1 - saturate(distance / range)) / distance;
}
float GetPointLightAttenuation(int index, Material material, ShadowMask shadowMask, float3 surfacePositionWS)
{
    float disSurface2Light = max(0.0001, length(material.positionWS - surfacePositionWS));
    ShadowData data = GetPointShadowData(index, material, shadowMask, disSurface2Light);
    float realtimeShadow = GetPointLightRealtimeShadow(index, data, material);
    float shadowStrength;
    if (shadowMask.enableShadowMask)
    {
        float bakedShadow = GetBakedShadow(shadowMask, data.shadowMaskChannel);
        shadowStrength = lerp(realtimeShadow, bakedShadow, 1);
    }
    else
        shadowStrength = realtimeShadow;
    
    return shadowStrength;
}
float GetSpotLightRealtimeShadow(int index, Material material, ShadowData data, float3 LightDirection)
{
    if (data.shadowStrength <= 0.0f) return 1.0f;
    float3 dirSurface2Light = normalize(_SpotLightPosition[index].xyz - material.positionWS);
    float disLight2Surface = max(0.001, length(_SpotLightPosition[index].xyz - material.positionWS));
    float range = max(0.001, _SpotLightPosition[index].w);
    float4 angles = _SpotLightAngle[index];
    float attenuation1 = saturate(dot(dirSurface2Light, LightDirection) * angles.x + angles.y);
    float attenuation2 = 1 - saturate(disLight2Surface / range);
    return attenuation1 * attenuation2 / disLight2Surface;
}
float GetSpotLightAttenuation(int index, Material material, ShadowMask shadowMask, float3 LightDirection)
{
    ShadowData data = GetSpotShadowData(index, material, shadowMask);
    float realtimeShadow = GetSpotLightRealtimeShadow(index, material, data, LightDirection);
    float shadowStrength;
    if (shadowMask.enableShadowMask)
    {
        float bakedShadow = GetBakedShadow(shadowMask, data.shadowMaskChannel);
        shadowStrength = lerp(realtimeShadow, bakedShadow, 1);
    }
    else
        shadowStrength = realtimeShadow;
    
    return shadowStrength;
}
// public 
Light SetupDirectionalLight(int index, Material material, ShadowMask shadowMask)
{
    Light dirLight;
    dirLight.color = _DirectionalLightColors[index].rgb;
    dirLight.direction = _DirectionalLightDirection[index].xyz;
    dirLight.attenuation = GetDirectionalLightAttenuation(index, material, shadowMask);
    /*int cascadeIndex = GetCascadeIndex(index, material);
    dirLight.attenuation = cascadeIndex * 0.25;*/
    return dirLight;
}
Light SetupPointLight(int index, Material material, ShadowMask shadowMask)
{
    Light pointLight;
    pointLight.color = _PointLightColors[index].rgb;
    pointLight.direction = normalize(_PointLightPosition[index].xyz - material.positionWS);
    pointLight.attenuation = GetPointLightAttenuation(index, material, shadowMask, _PointLightPosition[index].xyz);
    
    return pointLight;
}
Light SetupSpotLight(int index, Material material, ShadowMask shadowMask)
{
    Light spotLight;
    spotLight.color = _SpotLightColors[index].rgb;
    spotLight.direction = normalize(_SpotLightDirection[index].xyz);
    spotLight.attenuation =  GetSpotLightAttenuation(index, material, shadowMask, spotLight.direction);
    
    return spotLight;
}
float3 GetLightedColor(Light light, Material material)
{
    return (saturate(dot(material.normalWS, light.direction)) * 0.5 + 0.5) * light.color * light.attenuation;
}
#endif