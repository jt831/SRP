using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[Serializable]
public class PostProcessing
{
    public Bloom Bloom;
    public Clouds Clouds;
}

[Serializable]
public class Bloom
{
    // Menu
    [HideInInspector]public bool active = false;
    public Shader shader;
    [Range(0, 1)] public float threshold = 0.2f;
    public float intensity = 0.5f;
    [Range(0, 1)] public float scatter = 0.5f;
    [ColorUsage(true, true)] public Color color = Color.white;
    
    // Properties
    private Material _material;
    private CommandBuffer _buffer;
    private Camera _camera;
    [Range(1, Single.MaxValue)] private int _downSample = 4;
    private enum Pass
    {
        Default, Blur, Bloom, Dark
    }
    public void Render(CommandBuffer buffer, Camera camera, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        _buffer = buffer;
        _camera = camera;
        _material = new Material(shader);
        Blur(camera.pixelWidth, camera.pixelHeight, src, dest);
    }
    private void Blur(int srcWidth, int srcHeight, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        int blur = (int) (scatter * 10);
        var srcID = src;
        // Setup material //
        Vector4 texelSize = new Vector4(1f / srcWidth, 1f / srcHeight, 0, 0);
        _buffer.SetGlobalVector("_BloomTexelSize", texelSize);
        _buffer.SetGlobalVector("_BloomColor", color);
        _buffer.SetGlobalFloat("_BloomWeight", intensity);
        _buffer.SetGlobalFloat("_BloomDownSample", _downSample);
        _buffer.SetGlobalFloat("_BloomThreshold", threshold);
        // Down sample //
        srcHeight /= _downSample;
        srcWidth /= _downSample;
        // Dark srcRT //
        int ID_DarkMap = Shader.PropertyToID("DarkMap");
        _buffer.SetGlobalTexture("BloomDarkTex", src);
        _buffer.GetTemporaryRT(ID_DarkMap, srcWidth, srcHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        _buffer.Blit(src, ID_DarkMap, _material, (int)Pass.Dark);
        src = ID_DarkMap;
        // Blur srcRT //
        for (int i = 0; i < blur; i++)
        {
            int ID_BlurMap = Shader.PropertyToID("BlurMap" + i);
            _buffer.SetGlobalTexture("BloomBlurTex", src);
            _buffer.GetTemporaryRT(ID_BlurMap, srcWidth, srcHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            _buffer.Blit(src, ID_BlurMap, _material, (int)Pass.Blur);
            src = ID_BlurMap;
        }
        // Bloom srcRT //
        _buffer.SetGlobalTexture("BloomSrcTex", srcID);
        _buffer.SetGlobalTexture("BloomBlurTex", src);
        int ID_BloomMap = Shader.PropertyToID("BloomMap");
        _buffer.GetTemporaryRT(ID_BloomMap, srcWidth, srcHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        _buffer.Blit(src, ID_BloomMap, _material, (int)Pass.Bloom);
        src = ID_BloomMap;
        // Release TemporalRT //
        /*for (int i = blur - 1; i >= 0; i--) 
            _buffer.ReleaseTemporaryRT(Shader.PropertyToID("BlurMap" + i));*/
        // Render srcRT to Camera //
        _buffer.SetGlobalTexture("BloomSrcTex", src);
        _buffer.Blit(src, dest, _material, (int)Pass.Default);
    }
}

[Serializable]
public class Clouds
{
    // Menu
    public bool active = false;
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
        this._buffer = buffer;
        this._camera = camera;
        SetMaterial();
        _buffer.SetGlobalTexture("GlobalTex", src);
        _buffer.SetRenderTarget(dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
    }
    private void SetMaterial()
    {
        _material = new Material(shader);
        _material.SetVector("minBoxPoint", container.position - container.localScale / 2);
        _material.SetVector("maxBoxPoint", container.position + container.localScale / 2);
    }
}