Shader "JTRP/Clouds"
{
   Properties
   {
       [MainTexture] _MainTex ("Texture", 2D) = "white" {}
   }
   Subshader
   {
       HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/Light.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
        ENDHLSL
       Cull Off
       ZWrite Off
       ZTest Always
       Pass
       {
           Name "VolumeCloudPass"
           Tags {"LightMode" = "JTRPLit"}
           
           HLSLPROGRAM
           #pragma vertex VolumeCloudPassVertex
           #pragma fragment VolumeCloudPassFragment

           CBUFFER_START(UnityPerMaterial)
           float3 minBoxPoint;
           float3 maxBoxPoint;
           CBUFFER_END
           
           TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
           
           struct Attributes
           {
               float3 positionOS : POSITION;
               float2 uv : TEXCOORD0;
           };

           struct v2f
           {
               float4 positionCS : SV_POSITION;
               float2 uv : TEXCOORD0;
               float3 viewDirection : TEXCOORD1;
           };

           v2f VolumeCloudPassVertex(Attributes input)
           {
               v2f output;
               output.uv = input.uv;
               output.positionCS = TransformObjectToHClip(input.positionOS);
               float3 viewDir = mul(unity_CameraInvProjection, float4(input.uv * 2 - 1, 0, -1));
               output.viewDirection = mul(unity_CameraToWorld, float4(viewDir, 0));

               return output;
           }

           float2 GetRayToContinerInformation(float3 minBoxPoint, float3 maxBoxPoint, float3 lightPos, float3 lightDir)
           {
               float3 t0 = (minBoxPoint - lightPos) / lightDir;
               float3 t1 = (maxBoxPoint - lightPos) / lightDir;

               float3 tmin = min(t0, t1);
               float3 tmax = max(t0, t1);

               float dstin = max(max(tmin.x, tmin.y), tmin.z);
               float dstout = min(min(tmax.x, tmax.y), tmax.z);

               float dst2Box = max(dstin, 0);
               float dstInsideBox = max(dstout - dst2Box, 0);

               return float2(dst2Box, dstInsideBox);
           }
           
           float4 VolumeCloudPassFragment(v2f input) : SV_Target
           {
               float4 finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
               float3 lightPos = _WorldSpaceCameraPos;
               float3 lightDir = SafeNormalize(input.viewDirection);
               float2 ray2ContainerInfo = GetRayToContinerInformation(minBoxPoint, maxBoxPoint, lightPos, lightDir);
               float dst2Box = ray2ContainerInfo.x;
               float dstInsideBox = ray2ContainerInfo.y;
               //if (dstInsideBox > 0) finalColor = 0.0f;
               
               return 0;
           }
           ENDHLSL
       }
   }
}
