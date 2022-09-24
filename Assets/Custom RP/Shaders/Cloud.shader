Shader "JTRP/VolumeCloud"
{
   Properties
   {
       [MainTexture] _NoiseMap("NoiseMap", 2D) = "white" {}
       [MainColor] _CloudColor("CloudColor", Color) = (1.0, 1.0, 1.0, 0.8)
       _Length("Length", float) = 10
       _Width("Width", float) = 10
       _Height("Height", float) = 10
       _Step("Step", float) = 0.1
       [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", float) = 1
       [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", float) = 0
   }
   Subshader
   {
        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/Light.hlsl"
        #include "Assets/Custom RP/Tools/JTShaderProperties.hlsl"
        ENDHLSL
       Pass
       {
           Name "VolumeCloudPass"
           Tags {"LightMode" = "JTRPLit"}
           Blend [_SrcBlend] [_DstBlend]
           
           HLSLPROGRAM
           #pragma vertex VolumeCloudPassVertex
           #pragma fragment VolumeCloudPassFragment

           #include "VolumeCloudPass.hlsl"
           ENDHLSL
       }
   }
}
