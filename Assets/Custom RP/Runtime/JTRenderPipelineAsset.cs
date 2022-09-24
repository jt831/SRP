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
    [SerializeField] private ShadowProperties shadow1 = default;


    // Create an pipeline instance to render 
    protected override RenderPipeline CreatePipeline()
    {
        DirectionalShadowProperties shadow = shadow1.directional;
        Vector3 cascadeRatios = new Vector3(shadow.cascade.ratio1, shadow.cascade.ratio2, shadow.cascade.ratio3);
        return new JTRenderPipeline(batching, shadow);
    }
    
}