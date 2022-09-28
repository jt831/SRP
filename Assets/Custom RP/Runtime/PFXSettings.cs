using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
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

    // Properties
    private Material _material;

    public void Render(CommandBuffer buffer, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        SetMaterial();
        buffer.SetGlobalTexture("GlobalTex", src);
        buffer.SetRenderTarget(dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
    }

    private void SetMaterial()
    {
        _material = new Material(Shader);
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
    public void Render(Camera camera, CommandBuffer buffer, RenderTargetIdentifier src, RenderTargetIdentifier dest)
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
        _material.SetVector("camPos", _camera.transform.position);
        _material.SetVector("camDir", _camera.transform.forward);
    }
}