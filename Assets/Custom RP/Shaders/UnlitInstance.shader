Shader "JTRP/UnlitInstance"
{
    Properties
    {
        [MainTexture]_BaseMap("BaseMap", 2D) = "white" {}
        [HDR]_BaseColor("BaseColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _AThreshold("Alpha Threshold", range(0, 1)) = 0
        [Toggle(ENABLE_ALPHA_CLIPPING)] enableClip("Enable Alpha Clip",float) = 0
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode" = "JTRPUnlit"}
            HLSLPROGRAM
            #pragma vertex UnlitPassInstanceVertex
            #pragma fragment UnlitPassInstanceFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment ENABLE_ALPHA_CLIPPING
            #include "UnlitInstancePass.hlsl"
            ENDHLSL
        }
    }
}
