#ifndef META_PASS_INCLUDED
#define META_PASS_INCLUDED

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
    float2 uv_lightmap: TEXCOORD1;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float2 uv         : VAR_BASE_UV;
};

v2f MetaPassVertex(Attributes input)
{
    v2f output;
    input.positionOS.xy = float2(input.uv_lightmap * unity_LightmapST.xy + unity_LightmapST.zw);
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.uv = input.uv;
    
    return output;
}

float4 MetaPassFragment(v2f input) : SV_Target
{
    float4 BaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 FinalColor = BaseMap * _BaseColor;
    
    float4 meta = 0.0f;
    if (unity_MetaFragmentControl.x)
    {
        meta = float4(FinalColor.rgb, 1.0f);
        meta.rgb += 0.5;
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
    else if (unity_MetaFragmentControl.y)
    {
        meta = GetEmissionColor(input.uv);
    }
    return meta;
}
#endif