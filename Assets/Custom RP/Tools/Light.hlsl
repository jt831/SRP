#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

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
float GetDirectionalLightRealtimeShadow(int index, ShadowData data, Material material)
{
    if (data.shadowStrength <= 0.0f) return 1.0f;
    // Transform positionWS to positionShadowSpace
    float3 positionSS = GetDirectionalPositionSS(index, data, material);
    // Get shadowStrength
    float shadowStrength = SampleDirectionalShadowMap(positionSS);
    return shadowStrength;
}
float GetDirectionalLightAttenuation(int index, Material material, ShadowMask shadowMask)
{
    ShadowData data = GetDirectionalShadowData(index, material, shadowMask);
    float realtimeShadow = GetDirectionalLightRealtimeShadow(index, data, material);
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
    shadowStrength = lerp(1.0f, shadowStrength, data.shadowStrength);
    
    return attenuation * shadowStrength;
}
float GetPointLightAttenuation(int index, Material material, ShadowMask shadowMask)
{
    ShadowData data = GetPointShadowData(index, material, shadowMask);
    float realtimeShadow = GetPointLightRealtimeShadow(index, data, material);
    float shadowStrength;
    if (shadowMask.enableShadowMask)
    {
        float bakedShadow = GetBakedShadow(shadowMask, data.shadowMaskChannel);
        shadowStrength =  lerp(realtimeShadow, bakedShadow, data.bakedShadowStrength);
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
    return (saturate(dot(material.normalWS, light.direction)) + 0.01) * light.color * light.attenuation;
}
#endif