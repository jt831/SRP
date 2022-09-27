Shader "Hidden/JTRP/Default"
{
    Subshader
    {
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "UnityMetaPass.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
        ENDHLSL
        Pass
        {
            Name "DefaultPass"
            ZWrite Off
            ZTest Always
            Cull Off
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyPassVertex
            #pragma fragment CopyPassFragment

            TEXTURE2D(GlobalTex);   SAMPLER(sampler_GlobalTex);
            struct v2f
            {
                float4 positionCS : SV_Position;
                float2 screenUV   : VAR_SCREEN_UV;
            };

            v2f CopyPassVertex(uint vertexID : SV_VertexID)
            {
                v2f output;
                // Draw a single triangle to include screen
                output.positionCS = float4(
	                vertexID <= 1 ? -1.0 : 3.0,
	                vertexID == 1 ? 3.0 : -1.0,
	                0.0, 1.0
                );
                output.screenUV = float2(
	                vertexID <= 1 ? 0.0 : 2.0,
	                vertexID == 1 ? 2.0 : 0.0
                );
                // Avoid uv flip
                if (_ProjectionParams.x < 0.0f)
                {
                    output.screenUV.y = 1 - output.screenUV.y;
                }
                return output;
            }

            float4 CopyPassFragment(v2f input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_LOD(GlobalTex, sampler_GlobalTex, input.screenUV, 0);
            }
            ENDHLSL
        }
    }
}
