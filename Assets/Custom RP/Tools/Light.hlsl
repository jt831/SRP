#ifndef LIT_PASS_INCLUDED
#define LIT_PASS_INCLUDED


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
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    float2 uv         : TEXCOORD2;
};

v2f LitPassVertex(Attributes input)
{
    v2f output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv;
    
    return output;
}

float4 LitPassFragment(v2f input) : SV_Target
{
    float4 BaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 FinalColor = BaseMap * _BaseColor;
    
    #if ENABLE_CLIPPING
    clip(FinalColor.a - _AThreshold);
    #endif
    FinalColor.rgb = normalize(input.normalWS);
    return FinalColor;
}
#endif