#ifndef LIT_PASS_INCLUDED
#define LIT_PASS_INCLUDED


#include "Assets/Custom RP/Tools/TransformTools.hlsl"
#include "Assets/Custom RP/Tools/CommonTools.hlsl"
#include "Assets/Custom RP/Tools/Light.hlsl"
#include "Assets/Custom RP/Tools/BRDF.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
float4 _BaseMap_ST;
float3 _LightColor;
float3 _LightDirection;
float3 _CameraPosition;
float3 _ViewDirection;
float3 _fresnelTerm;
float _roughness;

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
    // Calculate Surface with Light
    Light light;
    light.color = _LightColor;
    light.direction = _LightDirection;

    _ViewDirection = SafeNormalize(_CameraPosition - input.positionWS);
    _LightDirection = SafeNormalize(_LightDirection);
    input.normalWS = SafeNormalize(input.normalWS);
    
    float4 BaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 FinalColor = _BaseColor;
    
    #if ENABLE_CLIPPING
    clip(FinalColor.a - _AThreshold);
    #endif
    #if ENABLE_BRDF
    FinalColor.xyz *= BRDF(input.normalWS, _ViewDirection, _LightDirection);
    return FinalColor;
    #endif

    FinalColor = GetSurfaceWithLight(light, input.normalWS, FinalColor);
    return FinalColor;
}
#endif