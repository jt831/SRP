Shader "JTRP/Unilt"
{
    Properties
    {
        [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}
        [MainColor][HDR] _BaseColor("BaseColor", Color) = (1., 1., 1., 1.)
        _AThreshold ("Threshold", range(0, 1)) = 0
        [Toggle(ENABLE_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", float) = 0
    }
    
    Subshader
    {
        Pass
        {
            Tags{"LightMode" = "JTRPUnlit"}
            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM
            #pragma shader_feature_local_fragment ENABLE_CLIPPING
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
}
