#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED


#include "Assets/Custom RP/Tools/TransformTools.hlsl"
#include "Assets/Custom RP/Tools/CommonTools.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _BaseMap_ST;
float _AThreshold;
CBUFFER_END

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float2 uv         : TEXCOORD1;
};

v2f Vertex(Attributes input)
{
    v2f output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.uv = input.uv;
    
    return output;
}

float4 Fragment(v2f input) : SV_Target
{
    float4 BaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 FinalColor = BaseMap * _BaseColor;
    
    #if ENABLE_CLIPPING
    clip(FinalColor.a - _AThreshold);
    #endif
    
    return FinalColor;
}
#endif