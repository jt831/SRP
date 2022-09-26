#ifndef GI_INCLUDE
#define GI_INCLUDE

#include <UnityShaderVariables.cginc>
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Assets/Custom RP/Tools/Light.hlsl"

struct GI
{
    float3 diffuse;
};
float3 GetLightMap(float2 uv_lightMap)
{
    bool encodedLightMap;
    #ifdef UNITY_LIGHTMAP_FULL_HDR
    encodedLightMap = false;
    #else
    encodedLightMap = true;
    #endif
    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
        uv_lightMap,
        float4(1.0, 1.0, 0.0, 0.0),
        encodedLightMap,
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0));
}
float3 GetLightProbe(Material material)
{
    #ifdef LIGHTMAP_ON
    return 0.0f;
    if (unity_ProbeVolumeParams.x)
        // LPPV
        return SampleProbeVolumeSH4(
            TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
            material.positionWS, material.normalWS,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
        );
    else
        // Light Probes
        float4 coefficients[7];
        coefficients[0] = unity_SHAr;
        coefficients[1] = unity_SHAg;
        coefficients[2] = unity_SHAb;
        coefficients[3] = unity_SHBr;
        coefficients[4] = unity_SHBg;
        coefficients[5] = unity_SHBb;
        coefficients[6] = unity_SHC;
        return max(0.0, SampleSH9(coefficients, material.normalWS));
    #endif
}
float4 GetShadowMaskColor(float2 uv_lightMap)
{
    #ifdef LIGHTMAP_ON
        return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, uv_lightMap);
    #else
        return unity_ProbesOcclusion;
    #endif
}
ShadowMask GetShadowMask(float2 uv_lightMap)
{
    ShadowMask shadowMask;
    shadowMask.enableShadowMask = false;
    shadowMask.shadowMaskColor = 1.0f;
    #ifdef ENABLE_SHADOW_MASK
    shadowMask.enableShadowMask = true;
    shadowMask.shadowMaskColor = GetShadowMaskColor(uv_lightMap);
    #endif
    
    return shadowMask;
}
float3 GetGIDiffuseColor(float2 uv_lightMap, Material material)
{
    #ifdef LIGHTMAP_ON
        return GetLightMap(uv_lightMap) + GetLightProbe(material);
    #else
        return 0.0f;
    #endif
}
GI GetGlobalIllumination(float2 uv_lightMap, Material material, ShadowMask shadowMask)
{
    GI gi;
    float3 shadowMaskColor = shadowMask.shadowMaskColor.xyz;
    gi.diffuse = GetGIDiffuseColor(uv_lightMap, material);
    return gi;
}
#endif