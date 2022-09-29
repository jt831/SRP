using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
    public bool Active = false;
    public Shader Shader;
    [Range(0, 8)]
    public int Blur = 5;
    // Properties
    private Material _material;
    private CommandBuffer _buffer;
    private Camera _camera;

    public void Render(CommandBuffer buffer, Camera camera, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        _buffer = buffer;
        _camera = camera;
        _material = new Material(Shader);
        BlurSrcTexture(camera.pixelWidth, camera.pixelHeight, src, dest);
    }
    private void BlurSrcTexture(int srcWidth, int srcHeight, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        Vector4 texelSize = new Vector4(1 / srcWidth, 1 / srcHeight, 0, 0);
        _buffer.SetGlobalVector("_TexelSize", texelSize);
        
        int ID_MipMap = Shader.PropertyToID("MipMap" + 0);
        _buffer.GetTemporaryRT(ID_MipMap, srcWidth, srcHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
        _buffer.Blit(src, ID_MipMap, _material, 0);
        // Down the resolution of srcTex
        for (int i = 1; i < Blur; i++)
        {
            srcHeight /= 2;
            srcWidth /= 2;
            _buffer.SetGlobalTexture("BloomSrcTex", src);
            ID_MipMap = Shader.PropertyToID("MipMap" + i);
            _buffer.GetTemporaryRT(ID_MipMap, srcWidth, srcHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
            _buffer.SetRenderTarget(ID_MipMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            //_buffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
            _buffer.Blit(src, ID_MipMap, _material, 1);
            src = ID_MipMap;
        }
        _buffer.SetGlobalTexture("BloomSrcTex", src);
        _buffer.Blit(src, dest, _material, 0);

        for (int i = Blur - 1; i >= 0; i--)
        {
            _buffer.ReleaseTemporaryRT(Shader.PropertyToID("MipMap" + i));
        }
    }
}

[Serializable]
public class Clouds
{
    // Menu
    public bool Active = false;
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