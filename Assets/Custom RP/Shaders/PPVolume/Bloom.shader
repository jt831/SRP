Shader "Hidden/JTRP/PP/Bloom"
{
    SubShader
    {
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
        
        TEXTURE2D(BloomSrcTex);   SAMPLER(sampler_BloomSrcTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize;
        CBUFFER_END
        
        struct Attributes
        {
            float3 positionWS : POSITION;
            float2 uv         : TEXCOORD0;
            uint vertexID     : SV_VertexID;
        };
        
        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            /*//一级纹理坐标（右上）
		    half2 uv20 : TEXCOORD1;
		    //二级纹理坐标（左下）
		    half2 uv21 : TEXCOORD2;
		    //三级纹理坐标（右下）
		    half2 uv22 : TEXCOORD3;
		    //四级纹理坐标（左上）
		    half2 uv23 : TEXCOORD4;*/
        };

        struct v2fHV
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float2 offset     : TEXCOORD1;
        };
        ENDHLSL
        
        Cull off
        ZWrite off
        ZTest off
        
        Pass
        {
            Name "BloomPass"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment BloomPassFrag
            v2f Vertex(Attributes input)
            {
                v2f output;
                // Draw a single triangle to include screen
                /*output.positionCS = float4(
	                input.vertexID <= 1 ? -1.0 : 3.0,
	                input.vertexID == 1 ? 3.0 : -1.0,
	                0.0, 1.0
                );
                output.uv = float2(
	                input.vertexID <= 1 ? 0.0 : 2.0,
	                input.vertexID == 1 ? 2.0 : 0.0
                );
                // Avoid uv flip
                if (_ProjectionParams.x < 0.0f)
                {
                    output.uv.y = 1 - output.uv.y;
                }*/
                output.positionCS = TransformWorldToHClip(input.positionWS);
                output.uv = input.uv;
                /*output.uv20 = output.uv + _TexelSize.xy * float2(1, 1);
                output.uv21 = output.uv + _TexelSize.xy * float2(-1, -1);
		        output.uv22 = output.uv + _TexelSize.xy * float2(1, -1);
		        output.uv23 = output.uv + _TexelSize.xy * float2(-1, 1);*/
                
                return output;
            }
            
            float4 BloomPassFrag(v2f input) : SV_Target
            {
                float4 baseColor = 0;
                float2 uv20 = input.uv + _TexelSize.xy * float2(1, 1);
                float2 uv21 = input.uv + _TexelSize.xy * float2(-1, -1);
		        float2 uv22 = input.uv + _TexelSize.xy * float2(1, -1);
		        float2 uv23 = input.uv + _TexelSize.xy * float2(-1, 1);
                baseColor += SAMPLE_TEXTURE2D_LOD(BloomSrcTex, sampler_BloomSrcTex, uv20, 0);
                baseColor += SAMPLE_TEXTURE2D_LOD(BloomSrcTex, sampler_BloomSrcTex, uv21, 0);
                baseColor += SAMPLE_TEXTURE2D_LOD(BloomSrcTex, sampler_BloomSrcTex, uv22, 0);
                baseColor += SAMPLE_TEXTURE2D_LOD(BloomSrcTex, sampler_BloomSrcTex, uv23, 0);
                
                return baseColor / 4;
            }
            ENDHLSL
            
        }
        
        Pass 
        {
            Name "BlurHorizontal"
            
            HLSLPROGRAM
            #pragma vertex BlurHorizontalVert
            #pragma fragment BlurHorizontalFrag

            v2fHV BlurHorizontalVert(Attributes input)
            {
                v2fHV output;
                output.positionCS = TransformWorldToHClip(input.positionWS);
                output.uv = input.uv;
                output.offset = float2(1.0, 0.0) * float2(0.001 ,0) * 2;
            
                return output;
            }
            float4 BlurHorizontalFrag(v2fHV input) : SV_Target
            {
                float3 color = 0.0;
                float weights[] = {0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703, 0.19459459, 0.12162162, 0.05405405, 0.01621622};
                float2 sampleUV = float2(input.uv - input.offset * 4);

                for (int i = 0; i < 9; i++)
                {
	                color += SAMPLE_TEXTURE2D(BloomSrcTex, sampler_BloomSrcTex, sampleUV).rgb * weights[i];
                    sampleUV += input.offset;
                }
                
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
