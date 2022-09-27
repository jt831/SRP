using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PFXStack
{
    private const string bufferName = "PFX";
    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private ScriptableRenderContext _context;
    private Camera _camera;
    public PFXSettings pfxSettings;

    private Material _material;
    public PFXStack(ScriptableRenderContext context, Camera camera, PFXSettings settings)
    {
        this._context = context;
        this._camera = camera;
        this.pfxSettings = camera.cameraType <= CameraType.SceneView ? settings : null;

        _material = new Material(settings.shader);
    }

    private static int
        ID_GlobalTexture = Shader.PropertyToID("GlobalTex");
    public void Render(ref CommandBuffer buffer, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        buffer.SetGlobalTexture(ID_GlobalTexture, src);
        buffer.SetRenderTarget(dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
    }
    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
}
