#ifndef MATERIAL_INCLUDE
#define MATERIAL_INCLUDE

struct Material
{
    float4 baseColor;
    float3 normalWS;
    float3 positionWS;
    float3 positionVS;
};

Material SetupMaterial(float4 baseMap, float3 positionWS, float3 normalWS, float3 positionVS)
{
    Material material;
    material.baseColor = baseMap;
    material.normalWS = normalWS;
    material.positionWS = positionWS;
    material.positionVS = positionVS;
    return material;
}
#endif