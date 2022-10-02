using System;
using TMPro;
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
    // 记得与shader那边的constNumber同步更新
    public const int MAXLights = 20;
    public const int MAXDirectionalLights = 4;
    public const int MAXPointLights = 8;
    public const int MAXSpotLights = 6;
    public const int MAXDirectionalShadowedLights = 4;
    public const int MAXPointShadowedLights = 6;
    public const int MAXSpotShadowedLights = 6;
    public const int MAXCascades = 4;
    public const int OtherShadowMapSplitNum = 6;
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
    private int _spotShadowLightCount = 0;
    private int _pointShadowLightCount = 0;
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
    public Vector4[] _PointShadowData2 = new Vector4[ConstNumber.MAXPointLights];
    public Matrix4x4[] _SpotTransformWorldToShadowMapMatrices = new Matrix4x4[ConstNumber.MAXSpotLights];
    public Matrix4x4[] _PointTransformWorldToShadowMapMatrices = new Matrix4x4[ConstNumber.MAXPointLights * 6];

    private static int
        ID_DirectionalShadowMap = Shader.PropertyToID("_DirectionalShadowMap"),
        ID_DirectionalLightCascadeCount = Shader.PropertyToID("_DirectionalLightCascadeCount"),
        ID_DirectionalCascadeSphere = Shader.PropertyToID("_DirectionalCascadeSphere"),
        ID_TransformWorldToShadowMapMatrices = Shader.PropertyToID("_TransformWorldToShadowMapMatrices"),
        ID_OtherShadowMap = Shader.PropertyToID("_OtherShadowMap"),
        ID_SpotTransformWorldToShadowMapMatrices = Shader.PropertyToID("_SpotTransformWorldToShadowMapMatrices"),
        ID_PointTransformWorldToShadowMapMatrices = Shader.PropertyToID("_PointTransformWorldToShadowMapMatrices");

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
                        _spotShadowLightCount, _light.bakingOutput.occlusionMaskChannel);
                    _spotShadowLightCount++;
                    break;
                case LightType.Point:
                    _PointShadowData[_pointLightCount++] = new Vector4(_light.shadowStrength, _lightIndex,
                        _pointShadowLightCount, _light.bakingOutput.occlusionMaskChannel);
                    _pointShadowLightCount++;
                    this._splitNum = ConstNumber.OtherShadowMapSplitNum;
                    this._splitSize = (int) _otherShadowProperties.resolution / _splitNum;
                    float texelSize = 2f /_splitSize;
                    float filterSize = texelSize * ((float)_otherShadowProperties.Fliter + 1f);
                    _PointShadowData2[_pointLightCount].x = _light.shadowNormalBias * filterSize * 1.4142136f;
                    break;
            }
            _shadowLightCount++;
        }
        else
        {
            switch (_light.type)
            {
                case LightType.Directional:
                    _DirectionalShadowData[_dirLightCount++] = new Vector4(0, -1, -1, 0);
                    break;
                case LightType.Spot:
                    _SpotShadowData[_spotLightCount++] = new Vector4(0, -1, -1, 0);
                    break;
                case LightType.Point:
                    _PointShadowData[_pointLightCount++] = new Vector4(0, -1, -1, 0);
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
        bool outofShadowLightCount = false;
        switch (_light.type)
        {
            case LightType.Directional:
                outofShadowLightCount = _dirShadowLightCount > ConstNumber.MAXDirectionalShadowedLights;
                break;
            case LightType.Spot:
                outofShadowLightCount = _spotShadowLightCount > ConstNumber.MAXSpotShadowedLights;
                break;
            case LightType.Point:
                outofShadowLightCount = _pointShadowLightCount > ConstNumber.MAXPointShadowedLights;
                break;
        }
        return !outofShadowLightCount && _light.shadows != LightShadows.None && 
               _light.shadowStrength > 0f && _results.GetShadowCasterBounds(_lightIndex, out Bounds bounds);
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
            _shadowBuffer.GetTemporaryRT(ID_DirectionalShadowMap, shadowMapResolution, shadowMapResolution,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            // Set RenderTarget
            _shadowBuffer.SetRenderTarget(ID_DirectionalShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
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
             * We claim a TemporalRenderTexture("_DirectionalShadowMap") as a ShadowMap when there exist ShadowedLight.
             * And if there isn't ShadowLight, it looks like we shouldn't claim such TemporalRenderTexture
             * But if we don't claim it, it's Sampler can't find his Texture, and program would be failed
             * So what we do here is claim a tiny Texture just to satisfy Sampler
             */
            _shadowBuffer.GetTemporaryRT(ID_DirectionalShadowMap, 1, 1,
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
        int otherShadowLightCount = _spotShadowLightCount + _pointShadowLightCount;
        if (otherShadowLightCount > 0)
        {
            // divide OtherShadowMap into 36 for point light need 6 split per light, so JTRP can support upto 6 pointLights' realtimeShadow
            this._splitNum = ConstNumber.OtherShadowMapSplitNum;
            this._splitSize = (int) _otherShadowProperties.resolution / _splitNum;
            int shadowMapResolution = (int)_otherShadowProperties.resolution;
            // Create a RenderTexture as a ShadowMap
            _shadowBuffer.GetTemporaryRT(ID_OtherShadowMap, shadowMapResolution, shadowMapResolution,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            // Set RenderTarget
            _shadowBuffer.SetRenderTarget(ID_OtherShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            // Clear RenderTarget
            _shadowBuffer.ClearRenderTarget(true, false, Color.clear);

            _shadowBuffer.BeginSample(shadowBufferName);
            ExecuteBuffer();
            for (int i = 0; i < _pointLightCount; i++)
            {
                int lightIndex = (int)_PointShadowData[i].y;
                int pointShadowLightIndex = (int)_PointShadowData[i].z;
                if (lightIndex < 0) break;
                DrawPointShadow(lightIndex, pointShadowLightIndex);
            }
            for (int i = 0; i < _spotLightCount; i++)
            {
                int lightIndex = (int)_SpotShadowData[i].y;
                int spotShadowLightIndex = (int)_SpotShadowData[i].z;
                if (lightIndex < 0) break;
                DrawSpotShadow(lightIndex, spotShadowLightIndex);
            }
            SetGlobalValue(ID_PointTransformWorldToShadowMapMatrices, _PointTransformWorldToShadowMapMatrices);
            SetGlobalValue(ID_SpotTransformWorldToShadowMapMatrices, _SpotTransformWorldToShadowMapMatrices);
            _shadowBuffer.EndSample(shadowBufferName);
            ExecuteBuffer();
        }
        else
        {
            _shadowBuffer.GetTemporaryRT(ID_OtherShadowMap, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }
    private void DrawPointShadow(int lightIndex, int pointShadowLightIndex)
    {
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(_results, lightIndex);
        int splitOffset = pointShadowLightIndex * 6;
        for (int i = 0; i < 6; i++)
        {
            int splitIndex = splitOffset + i;
            _results.ComputePointShadowMatricesAndCullingPrimitives(lightIndex, (CubemapFace)i, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData);
            /*viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;*/
            Matrix4x4 m = projMatrix * viewMatrix;
            Vector2 offset = SetupSplit(splitIndex);
            _PointTransformWorldToShadowMapMatrices[splitIndex] = TransformWorldToShadowMapMatrix(m, offset, _splitNum);
            _shadowBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            shadowSettings.splitData = splitData;
            ExecuteBuffer();
            _context.DrawShadows(ref shadowSettings);
        }
    }
    private void DrawSpotShadow(int lightIndex, int spotShadowLightIndex)
    {
        var shadowSettings = new ShadowDrawingSettings(_results, lightIndex);
        _results.ComputeSpotShadowMatricesAndCullingPrimitives(lightIndex, 
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData);
        Matrix4x4 m = projMatrix * viewMatrix;
        // 'splitOffset' only useful when spotLights draw after pointLights in OtherShadowMap
        int splitOffset = _pointShadowLightCount * 6 + spotShadowLightIndex;
        Vector2 offset = SetupSplit(splitOffset);
        _SpotTransformWorldToShadowMapMatrices[spotShadowLightIndex] = TransformWorldToShadowMapMatrix(m, offset, _splitNum);
        _shadowBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
        shadowSettings.splitData = splitData;
        ExecuteBuffer();
        _context.DrawShadows(ref shadowSettings);
    }
    private Vector2 SetupSplit(int splitIndex)
    {
        /*
        * Split the entire ShadowMap to _splitNum * _splitNum
        * for each ShadowedLight draw it's own part of ShadowMap to avoid overDraw
        */
        Vector2 offset = SetupOffset(splitIndex);
        _shadowBuffer.SetViewport(new Rect(offset.x * _splitSize, offset.y * _splitSize, _splitSize, _splitSize));
        return offset;
    }
    private Vector2 SetupOffset(int splitIndex)
    {
        Vector2 offset = new Vector2(splitIndex % _splitNum, splitIndex / _splitNum);
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
        _shadowBuffer.ReleaseTemporaryRT(ID_DirectionalShadowMap);
        _shadowBuffer.ReleaseTemporaryRT(ID_OtherShadowMap);
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
        ID_PointShadowData2 = Shader.PropertyToID("_PointShadowData2"),
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
        this._SpotLightDirection = new Vector4[ConstNumber.MAXSpotLights];
        this._SpotLightPosition = new Vector4[ConstNumber.MAXSpotLights];
        this._SpotLightAngle = new Vector4[ConstNumber.MAXSpotLights];
        
        _lightBuffer.BeginSample(lightBufferName);
        // 1.Get and set visibleLights
        NativeArray<VisibleLight> visibleLights = _results.visibleLights;
        // 2.Calculate shadows per visibleLight
        int bound = Math.Min(visibleLights.Length, ConstNumber.MAXDirectionalShadowedLights);
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
        _lightBuffer.SetGlobalVectorArray(ID_PointShadowData2, _shadow._PointShadowData2);
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
    private Camera _camera;
    private Batching _batching;
    private DirectionalShadowProperties _dirShadowProperties;
    private OtherShadowProperties _otherShadowProperties;
    private Lighting _lighting;
    private Shadow _shadow;
    private PostProcessing _pp;
    private float _shadowDistance;

    private static int
        ID_ViewDirection = Shader.PropertyToID("_ViewDirection"),
        ID_ShadowMapResolution = Shader.PropertyToID("_ShadowMapResolution"),
        ID_MaxShadowDistance = Shader.PropertyToID("_MaxShadowDistance"),
        ID_Fade = Shader.PropertyToID("_Fade"),
        ID_SampleBlockerDepthRadius = Shader.PropertyToID("_SampleBlockerDepthRadius"),
        ID_LightWidth = Shader.PropertyToID("_LightWidth"),
        ID_FrameBuffer = Shader.PropertyToID("_CameraFrameBuffer"),
        ID_FrameBuffer1 = Shader.PropertyToID("_CameraFrameBuffer1");
    public static string[] filterKeywords =
    {
        "_DIRECTIONAL_PCSS",
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };
    public void Render(ScriptableRenderContext context, Camera camera, 
        Batching batching, PostProcessing pp, 
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
        this._pp = pp;
        
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
        ExecuteBuffer(_buffer);
        _lighting.RenderLights(_context, _results, _dirShadowProperties, _otherShadowProperties);
        _buffer.EndSample(BufferName);
    }
    private void Setup()
    {
        // Setup camera
        _context.SetupCameraProperties(_camera);
        // Setup pfxStack and Get FrameBuffer as the input of pfxStack
        if (EnablePostProcessing())
        {
            _buffer.GetTemporaryRT(ID_FrameBuffer, _camera.pixelWidth, _camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
            _buffer.SetRenderTarget(ID_FrameBuffer, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            _buffer.GetTemporaryRT(ID_FrameBuffer1, _camera.pixelWidth, _camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
        }
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
        
        // PostProcessing
        ApplyPostProcessing();
        
        // Release temporalRT(As a shadowMap) before Submit
        _shadow.Cleanup();
    }
    private void Submit()
    {
        // Submit context
        _buffer.EndSample(BufferName);
        ExecuteBuffer(_buffer);
        _context.Submit();
    }
    private void ExecuteBuffer(CommandBuffer buffer)
    {
        // 将commandBuffer参数注册到context要执行的命令列表中，然后清空commandBuffer
        _context.ExecuteCommandBuffer(buffer);
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
        ExecuteBuffer(_buffer);
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
    private void CleanUp()
    {
        _buffer.ReleaseTemporaryRT(ID_FrameBuffer);
    }

    private bool EnablePostProcessing()
    {
        if (_pp.Bloom.shader == Shader.Find("Bloom") ) _pp.Bloom.active = true;
        if (_pp.Clouds.shader == Shader.Find("Clouds") ) _pp.Clouds.active = true;
        if (_pp.AwakeEyes.shader == Shader.Find("AwakeEyes") ) _pp.AwakeEyes.active = true;

        return _pp.Clouds.active || _pp.Bloom.active || _pp.AwakeEyes.active;
    }
    private void ApplyPostProcessing()
    {
        // Apply Post Effects
        if (_pp.Clouds.active)
        {
            CommandBuffer buffer = new CommandBuffer() {name = "Clouds"};
            buffer.BeginSample("Clouds");
            _pp.Clouds.Render(buffer,_camera,  ID_FrameBuffer, BuiltinRenderTextureType.CameraTarget);
            buffer.EndSample("Clouds");
            ExecuteBuffer(buffer);
        }
        if (_pp.Bloom.active)
        {
            CommandBuffer buffer = new CommandBuffer() {name = "Bloom"};
            buffer.BeginSample("Bloom");
            _pp.Bloom.Render(buffer, _camera, ID_FrameBuffer, BuiltinRenderTextureType.CameraTarget);
            buffer.EndSample("Bloom");
            ExecuteBuffer(buffer);
        }
        if (_pp.AwakeEyes.active)
        {
            CommandBuffer buffer = new CommandBuffer() {name = "AwakeEyes"};
            buffer.BeginSample("AwakeEyes");
            _pp.AwakeEyes.Render(buffer, _camera, ID_FrameBuffer, BuiltinRenderTextureType.CameraTarget);
            buffer.EndSample("AwakeEyes");
            ExecuteBuffer(buffer);
        }
        CleanUp();
    }
}