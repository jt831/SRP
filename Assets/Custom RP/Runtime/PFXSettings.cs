using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

[Serializable]
public class PostProcessing
{
    public Bloom Bloom;
    public Clouds Clouds;
    public AwakeEyes AwakeEyes;
}

[Serializable]
public class Bloom
{
    // Menu
    [HideInInspector]public bool active = false;
    public Shader shader;
    [Range(0, 1)] public float threshold = 0.2f;
    [Range(0, 5)]public float intensity = 0.5f;
    [Range(0, 1)] public float scatter = 0.5f;
    [ColorUsage(true, true)] public Color color = Color.white;
    
    // Properties
    private Material _material;
    private CommandBuffer _buffer;
    private Camera _camera;
    private int _downSample;
    private int _blur = 4;
    private enum Pass
    {
        Default, Blur, Bloom, Dark
    }
    public void Render(CommandBuffer buffer, Camera camera, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        if (shader == null) return;
        _buffer = buffer;
        _camera = camera;
        _material = new Material(shader);
        Blur(camera.pixelWidth, camera.pixelHeight, src, dest);
    }
    private void Blur(int srcWidth, int srcHeight, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        _downSample = (int) (scatter * 10 + 1);
        var srcID = src;
        // Setup material //
        Vector4 texelSize = new Vector4(1f / srcWidth, 1f / srcHeight, 0, 0);
        _buffer.SetGlobalVector("_BloomTexelSize", texelSize);
        _buffer.SetGlobalVector("_BloomColor", color);
        _buffer.SetGlobalFloat("_BloomWeight", intensity);
        _buffer.SetGlobalFloat("_BloomDownSample", _downSample);
        _buffer.SetGlobalFloat("_BloomThreshold", threshold);
        // Dark srcRT //
        int ID_DarkMap = Shader.PropertyToID("DarkMap");
        _buffer.SetGlobalTexture("BloomDarkTex", src);
        _buffer.GetTemporaryRT(ID_DarkMap, srcWidth, srcHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        _buffer.Blit(src, ID_DarkMap, _material, (int)Pass.Dark);
        src = ID_DarkMap;
        // Blur srcRT //
        srcHeight /= _downSample;
        srcWidth /= _downSample;
        for (int i = 0; i < _blur; i++)
        {
            int ID_BlurMap = Shader.PropertyToID("BlurMap" + i);
            _buffer.SetGlobalTexture("BloomBlurTex", src);
            _buffer.GetTemporaryRT(ID_BlurMap, srcWidth, srcHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            _buffer.Blit(src, ID_BlurMap, _material, (int)Pass.Blur);
            src = ID_BlurMap;
        }
        srcHeight *= _downSample;
        srcWidth *= _downSample;
        // Bloom srcRT //
        _buffer.SetGlobalTexture("BloomSrcTex", srcID);
        _buffer.SetGlobalTexture("BloomBlurTex", src);
        int ID_BloomMap = Shader.PropertyToID("BloomMap");
        _buffer.GetTemporaryRT(ID_BloomMap, srcWidth, srcHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        _buffer.Blit(src, ID_BloomMap, _material, (int)Pass.Bloom);
        src = ID_BloomMap;
        // Release TemporalRT //
        for (int i = _blur - 1; i >= 0; i--) 
            _buffer.ReleaseTemporaryRT(Shader.PropertyToID("BlurMap" + i));
        // Render srcRT to Camera //
        _buffer.SetGlobalTexture("BloomSrcTex", src);
        _buffer.Blit(src, dest, _material, (int)Pass.Default);
    }
}

[Serializable]
public class Clouds
{
    // Menu
    [HideInInspector] public bool active = false;
    public Shader shader;
    [Serializable]
    public struct Container
    {
        public Vector3 position;
        public Vector3 localScale;
    }
    public Container container;
    
    // Private
    private CommandBuffer _buffer;
    private Camera _camera;
    private Material _material;
    public void Render(CommandBuffer buffer, Camera camera, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        if (shader == null) return;
        this._buffer = buffer;
        this._camera = camera;
        SetMaterial();
        _buffer.SetGlobalTexture("CloudsSrcTex", src);
       _buffer.Blit(src, dest, _material, 0);
    }
    private void SetMaterial()
    {
        _material = new Material(shader);
        _material.SetVector("minBoxPoint", container.position - container.localScale / 2);
        _material.SetVector("maxBoxPoint", container.position + container.localScale / 2);
    }
}

[Serializable]
public class AwakeEyes
{
    // Menu
    [HideInInspector]public bool active = false;
    public Shader shader;
    [Range(0, 1)]public float processing = 0;
    
    // Private
    private CommandBuffer _buffer;
    private Camera _camera;
    private Material _material;
    // Material Properties
    private float _upBound;
    private float _lowBound;
    private enum Pass
    {
        Default, Eyelid, Blur
    }

    private static int
        ID_DarkTex = Shader.PropertyToID("DarkTex"),
        ID_BlurTex = Shader.PropertyToID("BlurTex");
    public void Render(CommandBuffer buffer, Camera camera, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        if (shader == null) return;
        this._buffer = buffer;
        this._camera = camera;
        SetMaterial();
        // Dark non-visible area
        buffer.SetGlobalTexture("AwakeEyesSrcTex", src);
        buffer.GetTemporaryRT(ID_DarkTex, _camera.pixelWidth, _camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        buffer.Blit(src, ID_DarkTex, _material, (int)Pass.Eyelid);
        src = ID_DarkTex;
        // Blur visible area
        if (processing < 0.95)
        {
            buffer.SetGlobalTexture("AwakeEyesSrcTex", src);
            buffer.GetTemporaryRT(ID_BlurTex, _camera.pixelWidth, _camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            buffer.Blit(src, ID_BlurTex, _material, (int)Pass.Blur);
            src = ID_BlurTex;
        }
        // Render srcTex to camera
        buffer.SetGlobalTexture("AwakeEyesSrcTex", src);
        buffer.Blit(src, dest, _material, (int)Pass.Default);
    }
    private void SetMaterial()
    {
        _material = new Material(shader);
        _upBound = 0.5f + processing * 0.7f;
        _lowBound = 0.5f - processing * 0.7f;
        _material.SetFloat("upBound", _upBound);
        _material.SetFloat("lowBound", _lowBound);
        _material.SetFloat("pixelWidth", 1f / _camera.pixelWidth);
        _material.SetFloat("pixelHeight", 1f / _camera.pixelHeight);
    }
}