#ifndef JTRPCOMMONFUNC_INCLUDE
#define JTRPCOMMONFUNC_INCLUDE

#include "Assets/Custom RP/Tools/GI.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseColor;
float4 _EmissionMap_ST;
float4 _EmissionColor;

float3 _ViewDirection;
float _AThreshold;
CBUFFER_END

float4 GetBaseColor(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
}
float4 GetLightedColor(GI gi, Material material, ShadowMask shadowMask)
{
    // 'Lighted Color' is 'Color affected by light'
    // Initialize LightedColor with global illumination
    float3 LightedColor = gi.diffuse;
    for (int i = 0; i < _DirectionalLightCount; i++)
    {
        Light dirLight = SetupDirectionalLight(i, material, shadowMask);
        LightedColor += GetLightedColor(dirLight, material);
    }
    for (int i = 0;i < _PointLightCount;i++)
    {
        Light pointLight = SetupPointLight(i, material, shadowMask);
        LightedColor += GetLightedColor(pointLight, material);
    }
    for (int i = 0;i < _SpotLightCount;i++)
    {
        Light spotLight = SetupSpotLight(i, material, shadowMask);
        LightedColor += GetLightedColor(spotLight, material);
    }
    return float4(LightedColor, 1.0f);
}
float4 GetEmissionColor(float2 uv)
{
    return SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap, uv) * _EmissionColor;
}
float4 GetFinalColor(float4 BaseColor,
    float4 LightedColor = float4(1.0, 1.0, 1.0, 1.0), float4 EmissionColor = float4(0.0, 0.0, 0.0, 0.0))
{
    return BaseColor * LightedColor + EmissionColor;
}
#endif