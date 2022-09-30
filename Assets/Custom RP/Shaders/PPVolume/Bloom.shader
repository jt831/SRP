Shader "Hidden/JTRP/PostProcessing/Bloom"
{
    SubShader
    {
        Cull off
        ZWrite off
        ZTest off
        
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
        
        TEXTURE2D(BloomSrcTex);   SAMPLER(sampler_BloomSrcTex);
        TEXTURE2D(BloomDarkTex);   SAMPLER(sampler_BloomDarkTex);
        TEXTURE2D(BloomBlurTex);   SAMPLER(sampler_BloomBlurTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _BloomTexelSize;
        float4 _BloomColor;
        float _BloomThreshold;
        float _BloomWeight;
        float _BloomDownSample;
        CBUFFER_END
        
        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
            uint vertexID     : SV_VertexID;
        };

        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float2 offsetH    : TEXCOORD1;
            float2 offsetV    : TEXCOORD2;
        };
        
        v2f Vert(Attributes input)
        {
            v2f output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.offsetH = float2(1.0, 0.0) * _BloomTexelSize.xy * _BloomDownSample;
            output.offsetV = float2(0.0, 1.0) * _BloomTexelSize.xy * _BloomDownSample;
            
            return output;
        }
        float4 DarkFrag(v2f input) : SV_Target
        {
            float4 baseColor = SAMPLE_TEXTURE2D_LOD(BloomDarkTex, sampler_BloomDarkTex, input.uv, 0);
            float lightness = 0.2 * baseColor.r + 0.7 * baseColor.g + 0.1 * baseColor.b;
            float finalLight = (lightness - _BloomThreshold) / lightness;
            return float4(finalLight * baseColor.rgb, baseColor.a);
        }

        float4 BlurFrag(v2f input) : SV_Target
        {
            float3 finalColor = 0.0;
            float weights[] = {0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703, 0.19459459, 0.12162162, 0.05405405, 0.01621622};
            float2 sampleUV = float2(input.uv - input.offsetH * 4);

            for (int i = 0; i < 9; i++)
            {
	            finalColor += SAMPLE_TEXTURE2D_LOD(BloomBlurTex, sampler_BloomBlurTex, sampleUV, 0).rgb * weights[i];
                sampleUV += input.offsetH;
            }

            sampleUV = float2(input.uv - input.offsetV * 4);
            for (int j = 0; j < 9; j++)
            {
                finalColor += SAMPLE_TEXTURE2D_LOD(BloomBlurTex, sampler_BloomBlurTex, sampleUV, 0).rgb * weights[j];
                sampleUV += input.offsetV;
            }
            
            return float4(finalColor / 2, 1.0);
        }

        float4 BloomFrag(v2f input) : SV_Target
        {
            float4 blurColor = SAMPLE_TEXTURE2D_LOD(BloomBlurTex, sampler_BloomBlurTex, input.uv, 0);
            float4 baseColor = SAMPLE_TEXTURE2D_LOD(BloomSrcTex, sampler_BloomSrcTex, input.uv, 0);

            return float4(baseColor.rgb + _BloomWeight * _BloomColor.rgb * blurColor.rgb, 1.0f);
        }
        ENDHLSL
        
        Pass
        {
            Name "DefaultPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DefaultFrag
            
            float4 DefaultFrag(v2f input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_LOD(BloomSrcTex, sampler_BloomSrcTex,input.uv, 0);
            }
            ENDHLSL
            
        }
        Pass
        {
            Name "BlurPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurFrag
            
            ENDHLSL
        }
        Pass
        {
            Name "BloomPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BloomFrag
            
            ENDHLSL
        }
        Pass
        {
            Name "DarkPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DarkFrag
            
            ENDHLSL
        }
    }
}
