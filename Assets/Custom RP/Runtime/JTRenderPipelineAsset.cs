using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[Serializable]
public struct Batching
{
    public bool GPUInstancing;
    public bool SRPBatching;
    public bool DynamicBatching;
}

// Create a GUI in "Project/RightMouseDown/Create/menuName"
[CreateAssetMenu(menuName = "Rendering/JT Render Pipeline")]
public class JTRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private Batching batching = new Batching
    {
        GPUInstancing = false,
        SRPBatching = true,
        DynamicBatching = false
    };
    [SerializeField] private ShadowProperties shadow = default;
    
    // Create an pipeline instance to render 
    protected override RenderPipeline CreatePipeline()
    {
        DirectionalShadowProperties directionalShadow = shadow.directional;
        OtherShadowProperties otherShadow = shadow.other;
        return new JTRenderPipeline(batching, directionalShadow, otherShadow);
    }
    
}