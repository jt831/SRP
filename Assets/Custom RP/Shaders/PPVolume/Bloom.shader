Shader "Hidden/JTRP/PP/Bloom"
{
    SubShader
    {
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
        ENDHLSL
        Cull off
        ZWrite off
        ZTest off
        
        Pass
        {
            Name "BloomPass"
            
            HLSLPROGRAM
            TEXTURE2D(GlobalTex);   SAMPLER(sampler_GlobalTex);
            #pragma vertex BloomPassVertex
            #pragma fragment BloomPassFragment

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            v2f BloomPassVertex(uint vertexID : SV_VertexID)
            {
                v2f output;
                // Draw a single triangle to include screen
                output.positionCS = float4(
	                vertexID <= 1 ? -1.0 : 3.0,
	                vertexID == 1 ? 3.0 : -1.0,
	                0.0, 1.0
                );
                output.uv = float2(
	                vertexID <= 1 ? 0.0 : 2.0,
	                vertexID == 1 ? 2.0 : 0.0
                );
                // Avoid uv flip
                if (_ProjectionParams.x < 0.0f)
                {
                    output.uv.y = 1 - output.uv.y;
                }
                
                return output;
            }

            float4 BloomPassFragment(v2f input) : SV_Target
            {
                float4 baseColor = SAMPLE_TEXTURE2D_LOD(GlobalTex, sampler_GlobalTex, input.uv, 0);

                return baseColor;
            }
            ENDHLSL
            
        }
    }
}
