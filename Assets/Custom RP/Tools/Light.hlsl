#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

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
#include "Assets/Custom RP/Tools/Shadow.hlsl"

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

// private
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
float GetFadeWeight (Material material)
{
    return (1 + material.positionVS.z / _MaxShadowDistance) * (1 / _Fade);
}
ShadowData GetDirectionalShadowData (int index, Material material, ShadowMask mask)
{
    ShadowData data;
    int cascadeIndex = GetCascadeIndex(index, material);
    if (mask.enableShadowMask)
    {
        // If enable shadowMask, draw shadow with no fade
        data.shadowStrength = abs(_DirectionalShadowData[index].x);
        data.splitIndex = _DirectionalShadowData[index].z + cascadeIndex;
        data.bakedShadowStrength = 1 - saturate(GetFadeWeight(material));
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
            data.shadowStrength = abs(_DirectionalShadowData[index].x) * GetFadeWeight(material);
            data.splitIndex = _DirectionalShadowData[index].z + cascadeIndex;
            data.bakedShadowStrength = 1 - saturate(GetFadeWeight(material));
            data.shadowMaskChannel = _DirectionalShadowData[index].w;
        }
    }
    return data;
}
ShadowData GetPointShadowData (int index, Material material, ShadowMask mask) {
    ShadowData data;
    // If surfaceDistance is out of shadowDistance && shadowMask is disabled
    data.shadowStrength = abs(_PointShadowData[index].x);
    data.splitIndex = _PointShadowData[index].z * 6 + index;
    // The closer camera2surface，the smaller bakedShadowStrength is, vice versa
    data.bakedShadowStrength = 1 - saturate(GetFadeWeight(material));
    data.shadowMaskChannel = _PointShadowData[index].w;
    return data;
}
ShadowData GetSpotShadowData (int index, Material material, ShadowMask mask)
{
    ShadowData data;
    data.shadowStrength = abs(_SpotShadowData[index].x);
    data.splitIndex = _SpotShadowData[index].z;
    // The closer camera2surface，the smaller bakedShadowStrength is, vice versa
    data.bakedShadowStrength = 1 - saturate(GetFadeWeight(material));
    data.shadowMaskChannel = _SpotShadowData[index].w;
    return data;
}
float SampleShadowMap(float3 positionSS)
{
    float shadowStrength = 0.0f;
    // Choose sample type by PCF
    #if defined(DIRECTIONAL_FILTER_SETUP)
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowMapResolution.yyxx;
        DIRECTIONAL_FILTER_SETUP(size, positionSS.xy, weights, positions);
        for (int i = 0;i < DIRECTIONAL_FILTER_SAMPLES;i++)
        {
            float temp = SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, float3(positions[i], positionSS.z));
            shadowStrength += weights[i] * temp;
        }
        return shadowStrength;
    #else
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSS);
    #endif
}
float SampleOtherShadowMap(float3 positionSS)
{
    float shadowStrength = 0.0f;
    // Choose sample type by PCF
    #if defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowMapResolution.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSS.xy, weights, positions);
    for (int i = 0;i < DIRECTIONAL_FILTER_SAMPLES;i++)
    {
        float temp = SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, float3(positions[i], positionSS.z));
        shadowStrength += weights[i] * temp;
    }
    return shadowStrength;
    #else
    return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSS);
    #endif
}
float GetBakedShadow(ShadowMask mask, int channel)
{
    return mask.shadowMaskColor[channel];
}
float3 GetDirectionalPositionSS(ShadowData data, float3 positionWS)
{
    float4 positionSS = mul(_TransformWorldToShadowMapMatrices[data.splitIndex], float4(positionWS, 1.0f));
    return float3(positionSS.xyz / positionSS.w);
}
float3 GetSpotPositionSS(int index, ShadowData data, Material material)
{
    float4 positionBiasWS = float4(material.positionWS + GetSpotLightSampleOffset(index, material), 1.0f);
    float4 positionSS = mul(_SpotTransformWorldToShadowMapMatrices[data.splitIndex], positionBiasWS);
    return float3(positionSS.xyz / positionSS.w);
}
float3 GetPointPositionSS(int index, float dirLight2Surface, Material material, ShadowData data)
{
    float face = CubeMapFaceID(dirLight2Surface);
    float weight = dot(dirLight2Surface, pointShadowPlanes[face]) * _PointShadowData2[index].x;
    float3 offset = material.normalWS * weight;
    float4 positionBiasWS = float4(material.positionWS + offset, 1.0f);
    float4 positionSS = mul(_PointTransformWorldToShadowMapMatrices[data.splitIndex + (int)face], positionBiasWS);
    return float3(positionSS.xyz / positionSS.w);
}
float GetDirectionalLightRealtimeShadow(ShadowData data, Material material)
{
    if (data.shadowStrength <= 0.0f) return 1.0f;
    // Extend surface along normalDirection to avoid selfShadow
    float3 positionBiasWS = material.positionWS + material.normalWS;
    // Transform positionWS to positionShadowSpace
    float3 positionSS = GetDirectionalPositionSS(data, positionBiasWS);
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
    float3 dirLight2Surface = normalize(material.positionWS - _PointLightPosition[index].xyz);
    float disLight2Surface = length(material.positionWS - _PointLightPosition[index].xyz);
    float distance = max(0.001, disLight2Surface);
    float range = max(0.001, _PointLightPosition[index].w);
    float attenuation = saturate((1 - saturate(distance / range)) / distance);
    
    float3 positionSS = GetPointPositionSS(index, dirLight2Surface, material, data);
    float shadowStrength = data.shadowStrength <= 0.0f ? 1.0f : SampleOtherShadowMap(positionSS);
    lerp(1.0f, shadowStrength, data.shadowStrength);
    
    return attenuation ;
}
float GetPointLightAttenuation(int index, Material material, ShadowMask shadowMask)
{
    ShadowData data = GetPointShadowData(index, material, shadowMask);
    float realtimeShadow = GetPointLightRealtimeShadow(index, data, material);
    float shadowStrength;
    if (shadowMask.enableShadowMask)
    {
        float bakedShadow = GetBakedShadow(shadowMask, data.shadowMaskChannel);
        shadowStrength = lerp(realtimeShadow, bakedShadow, data.bakedShadowStrength);
    }
    else
        shadowStrength = realtimeShadow;

    return shadowStrength;
}
float GetSpotLightRealtimeShadow(int index, Material material, ShadowData data, float3 LightDirection)
{
    // Control the area of SpotLight(without shadow)
    float3 dirSurface2Light = normalize(_SpotLightPosition[index].xyz - material.positionWS);
    float disLight2Surface = max(0.001, length(_SpotLightPosition[index].xyz - material.positionWS));
    float range = max(0.001, _SpotLightPosition[index].w);
    float4 angles = _SpotLightAngle[index];
    float angleAttenuation = saturate(dot(dirSurface2Light, LightDirection) * angles.x + angles.y);
    float rangeAttenuation = 1 - saturate(disLight2Surface / range);
    float attenuation = rangeAttenuation * angleAttenuation / Square(disLight2Surface);
    // Control the shadow of SpotLight
    float3 positionSS = GetSpotPositionSS(index, data, material);
    float shadowStrength = data.shadowStrength <= 0.0f ? 1.0f : SampleOtherShadowMap(positionSS);
    
    return attenuation * shadowStrength;
}
float GetSpotLightAttenuation(int index, Material material, ShadowMask shadowMask, float3 LightDirection)
{
    ShadowData data = GetSpotShadowData(index, material, shadowMask);
    float realtimeShadow = GetSpotLightRealtimeShadow(index, material, data, LightDirection);
    float shadowStrength;
    if (shadowMask.enableShadowMask)
    {
        float bakedShadow = GetBakedShadow(shadowMask, data.shadowMaskChannel);
        shadowStrength = lerp(realtimeShadow, bakedShadow, data.bakedShadowStrength);
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
    pointLight.attenuation = GetPointLightAttenuation(index, material, shadowMask);
    
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