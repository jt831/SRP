/*
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudMaster : MonoBehaviour
{
    [Header("-----Main-----")]
    public Shader _shader;
    public Transform _container;
    public Camera mainCamera;
    private RenderTexture _renderTex;
    private Material _material;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            _renderTex = new RenderTexture(mainCamera.pixelWidth, mainCamera.pixelHeight, 16);
        }
        else
        {
            Debug.Log("MainCamera is NULL !");
        }
    }
    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }
    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == mainCamera)
        {
            Debug.Log("EndCameraRendering");
            SetupMaterial();
            Graphics.Blit(_renderTex, mainCamera.targetTexture, _material);
        }
    }
    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }
    void SetupMaterial()
    {
        if (_material == null || _material.shader != _shader)
        {
            _material = new Material(_shader);
        }
        _material.SetVector("maxBoxPoint", _container.position + _container.localScale / 2);
        _material.SetVector("minBoxPoint", _container.position - _container.localScale / 2);
    }
}
*/
