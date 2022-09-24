#ifndef CUSTOM_UNLIT_PASS1_INCLUDED
#define CUSTOM_UNLIT_PASS1_INCLUDED

#include "UnityCG.cginc"
#include "Assets/Custom RP/Tools/CommonTools.hlsl"
#include "Assets/Custom RP/Tools/TransformTools.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _AThreshold)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float2 uv         : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f Vertex (Attributes input)
{
    v2f output = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.uv = input.uv;
    return output;
}

float4 Fragment(v2f input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 BaseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 BaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 finalColor = BaseColor * BaseMap;
    
    #if ENABLE_ALPHA_CLIPPING
    clip(finalColor.a - _AThreshold);
    #endif

    return finalColor;
}
#endif 