#ifndef GI_INCLUDE
#define GI_INCLUDE


#include "Assets/Custom RP/Tools/TransformTools.hlsl"
#include "Assets/Custom RP/Tools/CommonTools.hlsl"

struct GI
{
    float3 diffuse;
};

GI SetGlobalIllumination(float2 uv_lightMap)
{
    GI gi;
    gi.diffuse = float3(uv_lightMap, 0);
    return gi;
}
#endif