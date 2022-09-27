using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class JTRenderPipeline : RenderPipeline
{
    private Batching _batching;
    private PostFX _pfxSettings;
    private DirectionalShadowProperties _dirShadowProperties;
    private OtherShadowProperties _otherShadowProperties;
    
    public JTRenderPipeline(Batching batching, PostFX pfxSettings,
        DirectionalShadowProperties dirShadowProperties, OtherShadowProperties otherShadowProperties)
    {
        // Enable Srp Batching & GPU Instancing & Dynamic Batching 
        GraphicsSettings.useScriptableRenderPipelineBatching = batching.SRPBatching;
        this._batching.DynamicBatching = batching.DynamicBatching;
        this._batching.GPUInstancing = batching.GPUInstancing;
        this._dirShadowProperties = dirShadowProperties;
        this._pfxSettings = pfxSettings;
        this._otherShadowProperties = otherShadowProperties;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }
    // Render scene per camera
    private CameraRenderer _cameraRender = new CameraRenderer();
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            _cameraRender.Render(context, camera, _batching, _pfxSettings, _dirShadowProperties, _otherShadowProperties);
            EndCameraRendering(context, camera);
        }
    }
}