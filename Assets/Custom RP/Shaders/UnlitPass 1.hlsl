#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "UnityCG.cginc"


CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
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
};

float4x4 GetObjectToWorldMatrix()
{
    return UNITY_MATRIX_M;
}

float4x4 GetWorldToHClipMatrix()
{
    return UNITY_MATRIX_VP;
}
float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
}

float4 TransformObjectToHClip(float3 positionOS)
{
    // More efficient than computing M*VP matrix product
    return mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)));
}

v2f Vertex(Attributes input)
{
    v2f output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    
    return output;
}

float4 Fragment(v2f input) : SV_Target
{
    return _BaseColor;
}
#endif