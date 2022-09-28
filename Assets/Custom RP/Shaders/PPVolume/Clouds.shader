Shader "Hidden/JTRP/PP/Clouds"
{
    Subshader
   {
       HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
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
           float3 camPos;
           float3 camDir;
           CBUFFER_END
           
           TEXTURE2D(GlobalTex);   SAMPLER(sampler_GlobalTex);
           
           struct Attributes
           {
               float3 positionOS : POSITION;
               float2 uv : TEXCOORD0;
               uint vertexID : SV_VertexID;
           };

           struct v2f
           {
               float4 positionCS : SV_POSITION;
               float2 uv : TEXCOORD0;
               float3 viewDir : TEXCOORD1;
           };

           v2f VolumeCloudPassVertex(Attributes input)
           {
                v2f output;
                // Draw a single triangle to include screen
                output.positionCS = float4(
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
                }
               float3 viewDir = mul(unity_CameraInvProjection, float4(input.uv * 2 - 1, 0, -1));
               output.viewDir = mul(unity_CameraToWorld, float4(viewDir, 0));

               return output;
           }

           float2 GetRayToContainerInfo(float3 minBoxPoint, float3 maxBoxPoint, float3 lightPos, float3 lightDir)
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
               float4 finalColor = SAMPLE_TEXTURE2D_LOD(GlobalTex, sampler_GlobalTex, input.uv, 0);
               float3 lightPos = _WorldSpaceCameraPos;
               float3 lightDir = normalize(input.viewDir);
               float2 ray2ContainerInfo = GetRayToContainerInfo(minBoxPoint, maxBoxPoint, lightPos, lightDir);
               float dst2Box = ray2ContainerInfo.x;
               float dstInsideBox = ray2ContainerInfo.y;
               if (dstInsideBox > 0) finalColor = 0.0f;
               
               return finalColor;
           }
           ENDHLSL
       }
   }
}
