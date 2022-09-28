/*using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PFXStack
{
    private const string bufferName = "PFX";
    private CommandBuffer _buffer = new CommandBuffer() {name = bufferName};

    private ScriptableRenderContext _context;
    private Camera _camera;
    public PFXSettings pfxSettings;

    private Material _material;
    public PFXStack(ScriptableRenderContext context, Camera camera, PFXSettings settings)
    {
        this._context = context;
        this._camera = camera;
        this.pfxSettings = camera.cameraType <= CameraType.SceneView ? settings : null;
        
        if (pfxSettings != null) _material = new Material(pfxSettings.shader);
    }
    private static int ID_GlobalTexture = Shader.PropertyToID("GlobalTex");
    public void Render(RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        _buffer.SetGlobalTexture(ID_GlobalTexture, src);
        _buffer.SetRenderTarget(dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
        ExecuteBuffer();
    }
    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
}*/