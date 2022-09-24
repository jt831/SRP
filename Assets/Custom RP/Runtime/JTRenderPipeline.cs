using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class JTRenderPipeline : RenderPipeline
{
    private Batching _batching;
    private ShadowProperties _shadowProperties;

    public JTRenderPipeline(Batching batching, ShadowProperties shadowProperties)
    {
        // Enable Srp Batching & GPU Instancing & Dynamic Batching 
        GraphicsSettings.useScriptableRenderPipelineBatching = batching.SRPBatching;
        this._batching.DynamicBatching = batching.DynamicBatching;
        this._batching.GPUInstancing = batching.GPUInstancing;
        this._shadowProperties = shadowProperties;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }
    // Render scene per camera
    private CameraRenderer _cameraRender = new CameraRenderer();
    protected override void Render(ScriptableRenderContext context, UnityEngine.Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            _cameraRender.Render(context, camera, _batching, _shadowProperties);
            EndCameraRendering(context, camera);
        }
    }
}