using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


public partial class CameraRenderer
{
    partial void DrawUnsupported();
    partial void DrawGizmos();
    // The following code will only be exe in UnityEditor, not in phone or PC, etc
#if UNITY_EDITOR
    private static ShaderTagId[] legacyShaderIDs =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    // Draw those Unsupported Shader with "_Bug purple"
    partial void DrawUnsupported()
    {
        var drawingSettings = new DrawingSettings(legacyShaderIDs[0], new SortingSettings(_camera))
        {
            overrideMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"))
        };
        for (int i = 1; i < legacyShaderIDs.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderIDs[i]);
        }
        
        var filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(_results, ref drawingSettings, ref filteringSettings);
    }
    partial void DrawGizmos()
    {
        if (_camera.cameraType == CameraType.SceneView)
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }
#endif

    
}
