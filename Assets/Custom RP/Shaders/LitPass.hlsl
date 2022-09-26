#ifndef LIT_PASS_INCLUDED
#define LIT_PASS_INCLUDED

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    float2 uv_lightMap: TEXCOORD1;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    float2 uv         : TEXCOORD2;
    float3 positionVS : TEXCOORD3;
    float2 uv_lightMap: VAR_LIGHT_MAP_UV;
};

v2f LitPassVertex(Attributes input)
{
    v2f output;
    output.uv = input.uv;
    #ifdef LIGHTMAP_ON
    output.uv_lightMap = input.uv_lightMap * unity_LightmapST.xy + unity_LightmapST.zw;
    #else
    output.uv_lightMap = 0.0;
    #endif
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionVS = TransformWorldToView(output.positionWS);
    output.normalWS   = normalize(TransformObjectToWorldNormal(input.normalOS));
    
    return output;
}

float4 LitPassFragment(v2f input) : SV_Target
{
    float4 BaseColor = GetBaseColor(input.uv);
    Material material = SetupMaterial(BaseColor, input.positionWS, input.normalWS, input.positionVS);
    ShadowMask shadowMask = GetShadowMask(input.uv_lightMap);
    GI gi = GetGlobalIllumination(input.uv_lightMap, material, shadowMask);
    float4 LightedColor = GetLightedColor(gi, material, shadowMask);
    float4 EmissionColor = GetEmissionColor(input.uv);
    float4 FinalColor = GetFinalColor(BaseColor, LightedColor, EmissionColor);
    #if defined(ENABLE_CLIPPING)
    clip(FinalColor.a - _AThreshold);
    #endif
    #ifdef LOD_FADE_CROSSFADE
    float dither = 0;
    clip(unity_LODFade.x - InterleavedGradientNoise(input.positionCS.xy, 0));
    #endif
    return FinalColor;
}
#endif