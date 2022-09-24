Shader "JTRP/Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}
        [MainColor] _BaseColor("BaseColor", Color) = (1., 1., 1., 1.)
        
        [Header(Emission)]
        [Space(5)]
        _EmissionMap("Emission Map", 2D) = "white"{}
        [HDR] _EmissionColor("Emission Color", Color) = (1., 1., 1., 1.)
        
        [Toggle(ENABLE_CLIPPING)] _Enable_Clipping ("Alpha Clipping", Float) = 0
        _AThreshold ("Alpha Threshold", range(0, 1)) = 0
        
        [Space(20)]
        [Header(Settings)]
        [Space(5)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", float) = 0
        [Enum(On, 1, Off, 0)]                   _EnableShadow("Shadow", float) = 1
    }
    
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
            Name "LitPass"
            Tags {"LightMode" = "JTRPLit"}
            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM
            #pragma target 3.5          // Update Graphics API
            #pragma shader_feature_local_fragment ENABLE_CLIPPING
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ ENABLE_SHADOW_MASK
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            #include "LitPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            // ShadowMap
            Name "ShadowPass"
            Tags {"LightMode" = "ShadowCaster"}
            ColorMask 0
            ZWrite [_EnableShadow]
            
            HLSLPROGRAM
            #pragma target 3.5          // Update Graphics API
            #pragma shader_feature_local_fragment ENABLE_CLIPPING
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            
            #include "ShadowPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            // Global illumination, bake 
            Name "MetaPass"
            Tags {"LightMode" = "Meta"}
            Cull off
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment

            #include "MetaPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "JTRPShaderGUI"
}
