using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
/*
 * 命名规则
 * 1._驼峰命名法：传入shader中的属性。如_TransformWorldToShadowMapMatrices
 * 2._小写命名法：在该class中定义的属性
 */
static class ConstNumber
{
    public const int MAXLights = 64;
    public const int MAXDirectionalLights = 4;
    public const int MAXPointLights = 60;
    public const int MAXSpotLights = 10;
    public const int MAXShadowedLights = 4;
    public const int MAXCascades = 4;
}
class Shadow
{
    /*
     * lightCount = min(visibleLights, MAXLights)
     * _dirLightCount + _spotLightCount + _pointLightCount = lightCount
     * '_dirShadowLightCount' is 'Number of Shadowed Directional Light', judged by IsShadowedLight().
     * '_lightIndex' is 'Index of lightCount, from 0 to lightCount'
     * 'shadowedLightIndex' , If a light which _lightIndex == 3 is a shadowedLight, then there's a shadowedLightIndex == 3
     * 
     */
    private static string shadowBufferName = "ShadowBuffer";
    private CommandBuffer _shadowBuffer = new CommandBuffer() {name = shadowBufferName};
    private ScriptableRenderContext _context;
    private CullingResults _results;
    
    private DirectionalShadowProperties _dirShadowProperties;
    private OtherShadowProperties _otherShadowProperties;
    private Vector3 _cascadeRatios;
    private Light _light;
    private int _shadowLightCount = 0;
    private int _dirLightCount = 0;
    private int _spotLightCount = 0;
    private int _pointLightCount = 0;
    private int _dirShadowLightCount = 0;
    private int _otherShadowLightCount = 0;
    private int _lightIndex;
    private int _splitNum;  // split shadowMap into splitNum * splitNum
    private int _splitSize;
    private int _dirLightSplitCount;
    private bool _enableShadowMask = false;

    public Matrix4x4[] _TransformWorldToShadowMapMatrices = new Matrix4x4[ConstNumber.MAXDirectionalLights * ConstNumber.MAXCascades];
    public Vector4[] _DirectionalShadowData = new Vector4[ConstNumber.MAXDirectionalLights];
    public Vector4[] _DirectionalCascadeSphere = new Vector4[ConstNumber.MAXDirectionalLights * ConstNumber.MAXCascades];
    public Vector4[] _SpotShadowData = new Vector4[ConstNumber.MAXSpotLights];
    public Vector4[] _PointShadowData = new Vector4[ConstNumber.MAXPointLights];
    public Matrix4x4[] _OtherTransformWorldToShadowMapMatrices = new Matrix4x4[16];

    private static int
        ID_DirectionalShadowAtlas = Shader.PropertyToID("_DirectionalShadowAtlas"),
        ID_DirectionalLightCascadeCount = Shader.PropertyToID("_DirectionalLightCascadeCount"),
        ID_DirectionalCascadeSphere = Shader.PropertyToID("_DirectionalCascadeSphere"),
        ID_TransformWorldToShadowMapMatrices = Shader.PropertyToID("_TransformWorldToShadowMapMatrices"),
        ID_OtherShadowAtlas = Shader.PropertyToID("_OtherShadowAtlas"),
        ID_OtherTransformWorldToShadowMapMatrices = Shader.PropertyToID("_OtherTransformWorldToShadowMapMatrices");

    // 'SetupShadow' is in for loop
    public void SetupShadow(ScriptableRenderContext context, CullingResults results, 
        DirectionalShadowProperties dirShadowProperties, OtherShadowProperties otherShadowProperties, 
        Light light, int lightIndex)
    {
        this._context = context;
        this._results = results;
        this._dirShadowProperties = dirShadowProperties;
        this._otherShadowProperties = otherShadowProperties;
        this._light = light;
        this._lightIndex = lightIndex;      // Index in visibleLights
        this._dirLightSplitCount = _dirLightCount * _dirShadowProperties.cascade.count;
        
        // Check if shadowMask is enabled
        if (_light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed
            && _light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
            this._enableShadowMask = true;
        // Setup shadowData per Light
        if (IsShadowedLight())
        {
            switch (_light.type)
            {
                case LightType.Directional:
                    _DirectionalShadowData[_dirLightCount++] = new Vector4(_light.shadowStrength, _lightIndex, 
                        _dirShadowLightCount, _light.bakingOutput.occlusionMaskChannel);
                    _dirShadowLightCount++;
                    break;
                case LightType.Spot:
                    _SpotShadowData[_spotLightCount++] = new Vector4(_light.shadowStrength, _lightIndex,
                        _otherShadowLightCount, _light.bakingOutput.occlusionMaskChannel);
                    _otherShadowLightCount++;
                    break;
                case LightType.Point:
                    _PointShadowData[_pointLightCount++] = new Vector4(_light.shadowStrength, _lightIndex,
                        _otherShadowLightCount, _light.bakingOutput.occlusionMaskChannel);
                    _otherShadowLightCount++;
                    break;
            }
            _shadowLightCount++;
        }
        else
        {
            switch (_light.type)
            {
                case LightType.Directional:
                    _DirectionalShadowData[_dirLightCount++] = new Vector4(_light.shadowStrength, -1, -1, 0);
                    break;
                case LightType.Spot:
                    _SpotShadowData[_spotLightCount++] = new Vector4(_light.shadowStrength, -1, -1, 0);
                    break;
                case LightType.Point:
                    _PointShadowData[_pointLightCount++] = new Vector4(_light.shadowStrength, -1, -1, 0);
                    break;
            }
        }
        SetKeywords();
    }
    private bool IsShadowedLight()
    {
        /*
         * A light shouldn't be seen as a valid ShadowedLight in such case below:
         * 1. The count of ShadowedLight is equal to maxCount
         * 2. Such Light's shadow model has been set to none
         * 3. Such Light's shadow strength has been set to 0 or smaller
         * 4. Such Light don't effect any objs for some reasons
         */
        return _shadowLightCount < ConstNumber.MAXShadowedLights
               && _light.shadows != LightShadows.None
               && _light.shadowStrength > 0f
               && _results.GetShadowCasterBounds(_lightIndex, out Bounds bounds);
    }
    public void DrawShadow()
    {
        DrawDirectionalShadow();
        DrawOtherShadow();
    }
    public void DrawDirectionalShadow()
    {
        if (_dirShadowLightCount > 0)
        {
            this._splitNum = _dirShadowLightCount * _dirShadowProperties.cascade.count <= 1 ? 1 :
                _dirShadowLightCount * _dirShadowProperties.cascade.count <= 4 ? 2 : 4;
            this._cascadeRatios = 
                new Vector3(_dirShadowProperties.cascade.ratio1, _dirShadowProperties.cascade.ratio2, _dirShadowProperties.cascade.ratio3);
            this._splitSize = (int) _dirShadowProperties.resolution / _splitNum;
            int shadowMapResolution = (int)_dirShadowProperties.resolution;
            // Create a RenderTexture as a ShadowMap
            _shadowBuffer.GetTemporaryRT(ID_DirectionalShadowAtlas, shadowMapResolution, shadowMapResolution,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            // Set RenderTarget
            _shadowBuffer.SetRenderTarget(ID_DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            // Clear RenderTarget
            _shadowBuffer.ClearRenderTarget(true, false, Color.clear);

            _shadowBuffer.BeginSample(shadowBufferName);
            ExecuteBuffer();
            // Draw shadows foreach DirectionalShadowedLight
            for (int i = 0; i < _dirLightCount; i++)
            {
                int lightIndex = (int)_DirectionalShadowData[i].y;
                // the ith Directional Shadowed Light
                int dirShadowLightIndex = (int) _DirectionalShadowData[i].z;
                if (lightIndex < 0) continue;
                DrawDirectionalShadow(lightIndex, dirShadowLightIndex);
            }
            SetGlobalValue(ID_DirectionalLightCascadeCount, _dirShadowProperties.cascade.count);
            SetGlobalValue(ID_TransformWorldToShadowMapMatrices, _TransformWorldToShadowMapMatrices);
            SetGlobalValue(ID_DirectionalCascadeSphere, _DirectionalCascadeSphere);
            _shadowBuffer.EndSample(shadowBufferName);
            ExecuteBuffer();
        }
        else
        {
            /*
             * We claim a TemporalRenderTexture("_DirectionalShadowAtlas") as a ShadowMap when there exist ShadowedLight.
             * And if there isn't ShadowLight, it looks like we shouldn't claim such TemporalRenderTexture
             * But if we don't claim it, it's Sampler can't find his Texture, and program would be failed
             * So what we do here is claim a tiny Texture just to satisfy Sampler
             */
            _shadowBuffer.GetTemporaryRT(ID_DirectionalShadowAtlas, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }
    private void DrawDirectionalShadow(int lightIndex, int dirShadowLightIndex)
    {
        // Initialize ShadowDrawingSettings
        ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(_results, lightIndex);
        // Draw ShadowMap for each cascade per shadowedLight
        int splitOffset = dirShadowLightIndex * _dirShadowProperties.cascade.count;
        for (int j = 0; j < _dirShadowProperties.cascade.count; j++)
        {
            int splitIndex = splitOffset + j;
            _results.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                lightIndex, j, _dirShadowProperties.cascade.count,
                _cascadeRatios, (int)_dirShadowProperties.resolution, _light.shadowNearPlane,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
            // Try to cull some surface from large cascade if it can be render in smaller cascade
            shadowSplitData.shadowCascadeBlendCullingFactor = 1f;
            shadowDrawingSettings.splitData = shadowSplitData;
            // Transform the WorldSpace coordination to ShadowMapSpace to sample shadowMap
            Matrix4x4 m = projMatrix * viewMatrix;
            Vector2 offset = SetupSplit(splitIndex);
            _DirectionalCascadeSphere[splitIndex] = shadowSplitData.cullingSphere;
            _DirectionalCascadeSphere[splitIndex].w = Mathf.Pow(_DirectionalCascadeSphere[splitIndex].w, 2);
            _TransformWorldToShadowMapMatrices[splitIndex] = TransformWorldToShadowMapMatrix(m, offset, _splitNum);
            _shadowBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            // 'DrawShadows' only render objs with Pass with "ShadowCaster" Tags
            ExecuteBuffer();
            _shadowBuffer.SetGlobalDepthBias(0, _dirShadowProperties.shadowSlopBias);
            _context.DrawShadows(ref shadowDrawingSettings);
            _shadowBuffer.SetGlobalDepthBias(0, 0);
        }
    }
    public void DrawOtherShadow()
    {
        int otherLightCount = _spotLightCount + _pointLightCount;
        if (_otherShadowLightCount > 0)
        {
            this._splitNum = _otherShadowLightCount <= 1 ? 1 : 2;
            this._splitSize = (int) _otherShadowProperties.resolution / _splitNum;
            int shadowMapResolution = (int)_otherShadowProperties.resolution;
            // Create a RenderTexture as a ShadowMap
            _shadowBuffer.GetTemporaryRT(ID_OtherShadowAtlas, shadowMapResolution, shadowMapResolution,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            // Set RenderTarget
            _shadowBuffer.SetRenderTarget(ID_OtherShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            // Clear RenderTarget
            _shadowBuffer.ClearRenderTarget(true, false, Color.clear);

            _shadowBuffer.BeginSample(shadowBufferName);
            ExecuteBuffer();
            // Draw shadows foreach DirectionalShadowedLight
            for (int i = 0; i < _spotLightCount; i++)
            {
                int lightIndex = (int)_SpotShadowData[i].y;
                int otherShadowLightIndex = (int)_SpotShadowData[i].z;
                if (lightIndex < 0) break;
                DrawOtherShadow(lightIndex, otherShadowLightIndex);
            }
            for (int i = 0; i < _pointLightCount; i++)
            {
                int lightIndex = (int)_PointShadowData[i].y;
                int otherShadowLightIndex = (int)_PointShadowData[i].z;
                if (lightIndex < 0) break;
                DrawOtherShadow(lightIndex, otherShadowLightIndex);
            }
            SetGlobalValue(ID_OtherTransformWorldToShadowMapMatrices, _OtherTransformWorldToShadowMapMatrices);
            _shadowBuffer.EndSample(shadowBufferName);
            ExecuteBuffer();
        }
        else
        {
            _shadowBuffer.GetTemporaryRT(ID_OtherShadowAtlas, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }
    private void DrawOtherShadow(int lightIndex, int otherShadowLightIndex)
    {
        var shadowSettings = new ShadowDrawingSettings(_results, lightIndex);
        _results.ComputeSpotShadowMatricesAndCullingPrimitives(lightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
        );
        _shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        shadowSettings.splitData = splitData;
        ExecuteBuffer();
        _context.DrawShadows(ref shadowSettings);
    }
    private Vector2 SetupSplit(int i)
    {
        /*
        * Split the entire ShadowMap to _splitNum * _splitNum
        * for each ShadowedLight draw it's own part of ShadowMap to avoid overDraw
        */
        Vector2 offset = SetOffset(i, _splitNum);
        _shadowBuffer.SetViewport(new Rect(offset.x * _splitSize, offset.y * _splitSize, _splitSize, _splitSize));
        return offset;
    }
    private Vector2 SetOffset(int i, int splitNum)
    {
        Vector2 offset = new Vector2(i % splitNum, i / splitNum);
        return offset;
    }
    private Matrix4x4 TransformWorldToShadowMapMatrix(Matrix4x4 m, Vector2 offset, int splitNum)
    {
        if (SystemInfo.usesReversedZBuffer) {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / splitNum;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_shadowBuffer);
        _shadowBuffer.Clear();
    }
    private void SetGlobalValue(int ID_GlobalValue, Matrix4x4[] GlobalMatrixArray)
    {
        _shadowBuffer.SetGlobalMatrixArray(ID_GlobalValue, GlobalMatrixArray);
        ExecuteBuffer();
    }
    private void SetGlobalValue(int ID_GlobalValue, int GlobalInt)
    {
        _shadowBuffer.SetGlobalInt(ID_GlobalValue, GlobalInt);
        ExecuteBuffer();
    }
    private void SetGlobalValue(int ID_GlobalValue, Vector4[] GlobalVectorArray)
    {
        _shadowBuffer.SetGlobalVectorArray(ID_GlobalValue, GlobalVectorArray);
        ExecuteBuffer();
    }
    private void SetKeywords()
    {
        if (_enableShadowMask) _shadowBuffer.EnableShaderKeyword("ENABLE_SHADOW_MASK");
    }
    public void Cleanup()
    {
        _shadowBuffer.ReleaseTemporaryRT(ID_DirectionalShadowAtlas);
    }
}
class Lighting
{
    private static string lightBufferName = "LightBuffer";
    private CommandBuffer _lightBuffer = new CommandBuffer() {name = lightBufferName};
    private CullingResults _results;
    private ScriptableRenderContext _context;
    private Shadow _shadow;
    
    private Vector4[] _DirectionalLightColors;
    private Vector4[] _DirectionalLightDirections;
    private Vector4[] _PointLightColors;
    private Vector4[] _PointLightPosition;
    private Vector4[] _SpotLightPosition;
    private Vector4[] _SpotLightColors;
    private Vector4[] _SpotLightDirection;
    private Vector4[] _SpotLightAngle;

    private static int
        ID_LightCount = Shader.PropertyToID("_LightCount"),
        // Directional Light
        ID_DirectionalShadowData = Shader.PropertyToID("_DirectionalShadowData"),
        ID_DirectionalLightCount = Shader.PropertyToID("_DirectionalLightCount"),
        ID_DirectionalLightColors = Shader.PropertyToID("_DirectionalLightColors"),
        ID_DirectionalLightDirection = Shader.PropertyToID("_DirectionalLightDirection"),
        // Point Light
        ID_PointShadowData = Shader.PropertyToID("_PointShadowData"),
        ID_PointLightCount = Shader.PropertyToID("_PointLightCount"),
        ID_PointLightColors = Shader.PropertyToID("_PointLightColors"),
        ID_PointLightPosition = Shader.PropertyToID("_PointLightPosition"),
        // Spot Light
        ID_SpotShadowData = Shader.PropertyToID("_SpotShadowData"),
        ID_SpotLightCount = Shader.PropertyToID("_SpotLightCount"),
        ID_SpotLightColors = Shader.PropertyToID("_SpotLightColors"),
        ID_SpotLightPosition = Shader.PropertyToID("_SpotLightPosition"),
        ID_SpotLightDirection = Shader.PropertyToID("_SpotLightDirection"),
        ID_SpotLightAngle = Shader.PropertyToID("_SpotLightAngle");
        
    public void RenderLights(ScriptableRenderContext context, CullingResults results, 
        DirectionalShadowProperties dirShadowProperties, OtherShadowProperties otherShadowProperties)
    {
        /*
         * 1.Get and set visibleLights
         * 2.Calculate shadows per visibleLight
         * 3.Ending
         */
        this._results = results;
        this._context = context;
        this._shadow = new Shadow();
        this._DirectionalLightColors = new Vector4[ConstNumber.MAXDirectionalLights];
        this._DirectionalLightDirections = new Vector4[ConstNumber.MAXDirectionalLights];
        this._PointLightPosition = new Vector4[ConstNumber.MAXPointLights];
        this._PointLightColors = new Vector4[ConstNumber.MAXPointLights];
        this._SpotLightColors = new Vector4[ConstNumber.MAXSpotLights];
        this._SpotLightDirection= new Vector4[ConstNumber.MAXSpotLights];
        this._SpotLightPosition= new Vector4[ConstNumber.MAXSpotLights];
        this._SpotLightAngle= new Vector4[ConstNumber.MAXSpotLights];
        
        _lightBuffer.BeginSample(lightBufferName);
        // 1.Get and set visibleLights
        NativeArray<VisibleLight> visibleLights = _results.visibleLights;
        // 2.Calculate shadows per visibleLight
        int bound = Math.Min(visibleLights.Length, ConstNumber.MAXShadowedLights);
        Debug.Log("VisibleLights' count = " + bound);
        for (int i = 0; i < bound; i++)
        {
            _shadow.SetupShadow(_context, _results, dirShadowProperties, otherShadowProperties, visibleLights[i].light, i);
        }
        _shadow.DrawShadow();
        // 3. Ending
        SetGlobalValue(ref visibleLights);
        _lightBuffer.EndSample(lightBufferName);
        ExecuteBuffer();
    }
    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_lightBuffer);
        _lightBuffer.Clear();
    }
    private void SetGlobalValue(ref NativeArray<VisibleLight> visibleLights)
    {
        int lightCount = Math.Min(visibleLights.Length, ConstNumber.MAXLights);
        int directionalLightCount = 0;
        int pointLightCount = 0;
        int spotLightCount = 0;
        for (int i = 0; i < lightCount; i++)
        {
            if (visibleLights[i].lightType == LightType.Directional && directionalLightCount < ConstNumber.MAXDirectionalLights)
            {
                _DirectionalLightColors[directionalLightCount] = visibleLights[i].finalColor;
                _DirectionalLightDirections[directionalLightCount] = -visibleLights[i].localToWorldMatrix.GetColumn(2);
                directionalLightCount++;
            }
            else if (visibleLights[i].lightType == LightType.Point && pointLightCount < ConstNumber.MAXPointLights)
            {
                _PointLightColors[pointLightCount] = visibleLights[i].finalColor;
                _PointLightPosition[pointLightCount] = visibleLights[i].localToWorldMatrix.GetColumn(3);
                _PointLightPosition[pointLightCount].w = visibleLights[i].range;
                pointLightCount++;
            }
            else if (visibleLights[i].lightType == LightType.Spot && pointLightCount < ConstNumber.MAXSpotLights)
            {
                _SpotLightColors[spotLightCount] = visibleLights[i].finalColor;
                _SpotLightDirection[spotLightCount] = -visibleLights[i].localToWorldMatrix.GetColumn(2);
                _SpotLightPosition[spotLightCount] = visibleLights[i].localToWorldMatrix.GetColumn(3);
                _SpotLightPosition[spotLightCount].w = visibleLights[i].range;
                float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLights[i].light.innerSpotAngle);
                float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLights[i].spotAngle);
                float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
                _SpotLightAngle[spotLightCount] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
                spotLightCount++;
            }
        }
        _lightBuffer.SetGlobalInt(ID_LightCount, lightCount);
        _lightBuffer.SetGlobalInt(ID_DirectionalLightCount, directionalLightCount);
        _lightBuffer.SetGlobalInt(ID_PointLightCount, pointLightCount);
        _lightBuffer.SetGlobalInt(ID_SpotLightCount, spotLightCount);
        _lightBuffer.SetGlobalVectorArray(ID_PointLightColors, _PointLightColors);
        _lightBuffer.SetGlobalVectorArray(ID_PointLightPosition, _PointLightPosition);
        _lightBuffer.SetGlobalVectorArray(ID_SpotLightPosition, _SpotLightPosition);
        _lightBuffer.SetGlobalVectorArray(ID_SpotLightColors, _SpotLightColors);
        _lightBuffer.SetGlobalVectorArray(ID_SpotLightDirection, _SpotLightDirection);
        _lightBuffer.SetGlobalVectorArray(ID_SpotLightAngle, _SpotLightAngle);
        _lightBuffer.SetGlobalVectorArray(ID_DirectionalLightColors, _DirectionalLightColors);
        _lightBuffer.SetGlobalVectorArray(ID_DirectionalLightDirection,_DirectionalLightDirections);
        _lightBuffer.SetGlobalVectorArray(ID_DirectionalShadowData, _shadow._DirectionalShadowData);
        _lightBuffer.SetGlobalVectorArray(ID_PointShadowData, _shadow._PointShadowData);
        _lightBuffer.SetGlobalVectorArray(ID_SpotShadowData, _shadow._SpotShadowData);
        
        ExecuteBuffer();
    }
}
public partial class CameraRenderer
{
    private const string BufferName = "RenderCamera";
    private CommandBuffer _buffer = new CommandBuffer(){name = BufferName};
    private CullingResults _results;
    private ScriptableRenderContext _context;
    private UnityEngine.Camera _camera;
    private Batching _batching;
    private DirectionalShadowProperties _dirShadowProperties;
    private OtherShadowProperties _otherShadowProperties;
    private Lighting _lighting;
    private Shadow _shadow;
    private float _shadowDistance;

    private static int
        ID_ViewDirection = Shader.PropertyToID("_ViewDirection"),
        ID_ShadowMapResolution = Shader.PropertyToID("_ShadowMapResolution"),
        ID_MaxShadowDistance = Shader.PropertyToID("_MaxShadowDistance"),
        ID_Fade = Shader.PropertyToID("_Fade"),
        ID_SampleBlockerDepthRadius = Shader.PropertyToID("_SampleBlockerDepthRadius"),
        ID_LightWidth = Shader.PropertyToID("_LightWidth");
    public static string[] filterKeywords =
    {
        "_DIRECTIONAL_PCSS",
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };
    public void Render(ScriptableRenderContext context, UnityEngine.Camera camera, Batching batching, 
        DirectionalShadowProperties dirShadowProperties, OtherShadowProperties otherShadowProperties)
    {
        /* 0. Render objects' shadows
         * 1. Setup properties
         * 2. Draw scene
         * 3. Submit context
         */
        this._context = context;
        this._camera = camera;
        this._batching.DynamicBatching = batching.DynamicBatching;
        this._batching.GPUInstancing = batching.GPUInstancing;
        this._dirShadowProperties = dirShadowProperties;
        this._otherShadowProperties = otherShadowProperties;
        this._lighting = new Lighting();
        this._shadow = new Shadow();
        PrepareBuffer();
        if (!Culling()) return;
        RenderShadows();
        Setup();
        Draw();
        Submit();
    }
    // 'ShaderTagId' is the value of 'LightMode' in Shader/SubShader/Pass/Tags
    private static ShaderTagId[] _supportShaderTagIds =
    {
        new ShaderTagId(name: "JTRPUnlit"),
        new ShaderTagId(name: "JTRPLit"),
    };
    private bool Culling()
    {
        
        // Do culling and get cullingResults
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
        {
            // Set ShadowDistance
            this._shadowDistance = Math.Min(_dirShadowProperties.distance, _camera.farClipPlane);
            parameters.shadowDistance = _shadowDistance;
            this._results = _context.Cull(ref parameters);
            return true;
        }
        return false;
    }
    private void RenderShadows()
    {
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
        _lighting.RenderLights(_context, _results, _dirShadowProperties, _otherShadowProperties);
        _buffer.EndSample(BufferName);
    }
    private void Setup()
    {
        // Setup camera
        _context.SetupCameraProperties(_camera);
        // Setup CommandBuffer
        _buffer.ClearRenderTarget(true, true, Color.clear);
        _buffer.BeginSample(BufferName);
        SetGlobalValue(_camera);
        SetKeywords();
    }
    private void Draw()
    {
        // Draw Opaque Objs
        var sortingSettings = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(_supportShaderTagIds[0], sortingSettings)
        {
            enableInstancing = _batching.GPUInstancing,
            enableDynamicBatching = _batching.DynamicBatching,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume 
                            | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe
            
        };
        for (int i = 1; i < _supportShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, _supportShaderTagIds[i]);
        }

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        _context.DrawRenderers(_results, ref drawingSettings, ref filteringSettings);
        
        // Draw Skybox
        _context.DrawSkybox(_camera);
        
        // Draw Transparent Objs
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        _context.DrawRenderers(_results, ref drawingSettings, ref filteringSettings);
        
        // Draw Objs with unsupported materials
        DrawUnsupported();
        
        // Draw Gizmos
        DrawGizmos();
        
        // Release temporalRT(As a shadowMap) before Submit
        _shadow.Cleanup();
    }
    private void Submit()
    {
        // Submit context
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
        _context.Submit();
    }
    private void ExecuteBuffer()
    {
        // 将commandBuffer参数注册到context要执行的命令列表中，然后清空commandBuffer
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    private void SetGlobalValue(UnityEngine.Camera camera)
    {
        Vector3 cameraDirection = camera.transform.forward;
        _buffer.SetGlobalVector(ID_ViewDirection, -cameraDirection);
        _buffer.SetGlobalFloat(ID_MaxShadowDistance, _dirShadowProperties.distance);
        _buffer.SetGlobalFloat(ID_Fade, _dirShadowProperties.fade);
        _buffer.SetGlobalFloat(ID_SampleBlockerDepthRadius, _dirShadowProperties.sampleBlockerDepthRadius);
        _buffer.SetGlobalFloat(ID_LightWidth, _dirShadowProperties.lightWidth);
        _buffer.SetGlobalVector(ID_ShadowMapResolution, 
            new Vector4((float)_dirShadowProperties.resolution, 1.0f / (float)_dirShadowProperties.resolution));
        ExecuteBuffer();
    }
    private void SetKeywords()
    {
        int filterMode = (int)_dirShadowProperties.Fliter - 1;
        for (int i = 0; i < filterKeywords.Length; i++)
        {
            if (i == filterMode) _buffer.EnableShaderKeyword(filterKeywords[i]);
            else _buffer.DisableShaderKeyword(filterKeywords[i]);
        }
    }
    void PrepareBuffer()
    {
        _buffer.name = _camera.name;
    }
}