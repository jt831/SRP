Shader "Hidden/JTRP/PP/AwakeEyes"
{
    Subshader
   {
       HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
       CBUFFER_START(UnityPerMaterial)
       float processing;
       float upBound;
       float lowBound;
       float pixelWidth;
       float pixelHeight;
       CBUFFER_END
       
       TEXTURE2D(AwakeEyesSrcTex);   SAMPLER(sampler_AwakeEyesSrcTex);
   
       struct Attributes
       {
           float3 positionOS : POSITION;
           float2 uv : TEXCOORD0;
       };

       struct v2f
       {
           float4 positionCS : SV_POSITION;
           float2 uv : TEXCOORD0;
       };

       v2f Vert(Attributes input)
       {
           v2f output;
           output.uv = input.uv;
           output.positionCS = TransformObjectToHClip(input.positionOS);

           return output;
       }
       ENDHLSL
       Cull Off
       ZWrite Off
       ZTest Always
       
       Pass
        {
            Name "DefaultPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DefaultFrag
            
            float4 DefaultFrag(v2f input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_LOD(AwakeEyesSrcTex, sampler_AwakeEyesSrcTex,input.uv, 0);
            }
            ENDHLSL
            
        }
       Pass
       {
           Name "EyelidPass"
           
           HLSLPROGRAM
           #pragma vertex Vert
           #pragma fragment EyelidFrag

           float4 EyelidFrag(v2f input) : SV_Target
           {
               float4 finalColor = 0.f;
               float4 baseColor = SAMPLE_TEXTURE2D(AwakeEyesSrcTex, sampler_AwakeEyesSrcTex, input.uv);
               // Get upBound and lowBound as tow lips
               float archHeight = 0.7;
               upBound -= archHeight * pow(input.uv.x - 0.5f, 2);
               lowBound += archHeight * pow(input.uv.x - 0.5f, 2);
               // Dark the area of non-visible
               if(_ProjectionParams.x < 0.0f) input.uv.y = 1 - input.uv.y;
               float isDark = step(upBound, input.uv.y) + step(input.uv.y, lowBound);
               finalColor = (1 - isDark) * baseColor;
               
               return finalColor;
           }
           ENDHLSL
       }
       Pass
       {
           Name "BlurPass"
           
           HLSLPROGRAM
           #pragma vertex Vert
           #pragma fragment BlurFrag

           float4 BlurFrag(v2f input) : SV_Target
           {
               float4 finalColor = 0.f;
               // Blur and dark the area of visible
               if(_ProjectionParams.x < 0.0f) input.uv = filpUV(input.uv);
               float isDark = step(upBound, input.uv.y) + step(input.uv.y, lowBound);
               float darkWeight = clamp(processing + 0.1, 0, 1);
               int downsampleWeight = 5 - int(processing * 5);
               // Only process visible area
               if (1 - isDark)
               {
                   input.uv = filpUV(input.uv);
                   float2 uv0 = input.uv + float2(-pixelWidth, pixelHeight) * downsampleWeight;
                   float2 uv1 = input.uv + float2(-pixelWidth, -pixelHeight) * downsampleWeight;
                   float2 uv2 = input.uv + float2(pixelWidth, pixelHeight) * downsampleWeight;
                   float2 uv3 = input.uv + float2(pixelWidth, -pixelHeight) * downsampleWeight;

                   finalColor += SAMPLE_TEXTURE2D(AwakeEyesSrcTex, sampler_AwakeEyesSrcTex, uv0) * darkWeight;
                   finalColor += SAMPLE_TEXTURE2D(AwakeEyesSrcTex, sampler_AwakeEyesSrcTex, uv1) * darkWeight;
                   finalColor += SAMPLE_TEXTURE2D(AwakeEyesSrcTex, sampler_AwakeEyesSrcTex, uv2) * darkWeight;
                   finalColor += SAMPLE_TEXTURE2D(AwakeEyesSrcTex, sampler_AwakeEyesSrcTex, uv3) * darkWeight;
                   finalColor /= 4;
                   return finalColor;
               }
               // For non-visible area, return 0;
               return 0;
           }
           ENDHLSL
       }
   }
}
