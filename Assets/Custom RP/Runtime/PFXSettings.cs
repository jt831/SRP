using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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
    [Range(0, 1)] public float threshold = 0.2f;
    [Range(0, 5)]public float intensity = 0.5f;
    [Range(0, 1)] public float scatter = 0.5f;
    [ColorUsage(true, true)] public Color color = Color.white;
    
    // Properties
    private Material _material;
    [HideInInspector] public Shader shader;
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
        _buffer = buffer;
        _camera = camera;
        shader = Resources.Load<Shader>("Bloom");
        _material = new Material(shader);
        Blur(camera.pixelWidth, camera.pixelHeight, src, dest);
    }
    private void Blur(int srcWidth, int srcHeight, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        if (shader != null)
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
        }
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
    public float cloudScale = 1.0f;

    [Serializable]
    public struct Container
    {
        public Vector3 position;
        public Vector3 localScale;
    }

    public Container container;
    public int numStepsLight = 8;
    public float rayOffsetStrength;
    public Texture2D blueNoise;


    public float densityMultiplier = 1;
    public float densityOffset;
    public Vector3 shapeOffset;
    public Vector2 heightOffset;
    public Vector4 shapeNoiseWeights;


    public float detailNoiseScale = 10;
    public float detailNoiseWeight = .1f;
    public Vector3 detailNoiseWeights;
    public Vector3 detailOffset;



    public float lightAbsorptionThroughCloud = 1;
    public float lightAbsorptionTowardSun = 1;
    [Range(0, 1)] public float darknessThreshold = .2f;
    [Range(0, 1)] public float forwardScattering = .83f;
    [Range(0, 1)] public float backScattering = .3f;
    [Range(0, 1)] public float baseBrightness = .8f;
    [Range(0, 1)] public float phaseFactor = .15f;


    public float timeScale = 1;
    public float baseSpeed = 1;
    public float detailSpeed = 2;


    public Color colA;
    public Color colB;

    // Internal
    [HideInInspector] public Material material;
    [HideInInspector] public Shader shader;
    // Private
    private CommandBuffer _buffer;
    private Camera _camera;
    private Material _material;
    

    public void Render(CommandBuffer buffer, Camera camera, NativeArray<VisibleLight> visibleLights, RenderTargetIdentifier src, RenderTargetIdentifier dest)
    {
        if (shader == null) return;
        this._buffer = buffer;
        this._camera = camera;
        shader = Resources.Load<Shader>("Clouds");
        // Validate inputs
        if (material == null || material.shader != shader)
        {
            material = new Material(shader);
        }
        numStepsLight = Mathf.Max(1, numStepsLight);
        // Noise
        var noiseGenerator = GameObject.Find("NoiseGenerator").gameObject.GetComponent<NoiseGenerator>();
        noiseGenerator.UpdateNoise ();
        buffer.SetGlobalTexture("_MainTex", src);
        material.SetTexture("NoiseTex", noiseGenerator.noiseTexture);
        material.SetTexture ("DetailNoiseTex", noiseGenerator.noiseTexture);
        material.SetTexture ("BlueNoise", blueNoise);
        

        Vector3 size = container.localScale;
        int width = Mathf.CeilToInt (size.x);
        int height = Mathf.CeilToInt (size.y);
        int depth = Mathf.CeilToInt (size.z);

        material.SetFloat ("scale", cloudScale);
        material.SetFloat ("densityMultiplier", densityMultiplier);
        material.SetFloat ("densityOffset", densityOffset);
        material.SetFloat ("lightAbsorptionThroughCloud", lightAbsorptionThroughCloud);
        material.SetFloat ("lightAbsorptionTowardSun", lightAbsorptionTowardSun);
        material.SetFloat ("darknessThreshold", darknessThreshold);
        material.SetFloat ("rayOffsetStrength", rayOffsetStrength);

        material.SetFloat ("detailNoiseScale", detailNoiseScale);
        material.SetFloat ("detailNoiseWeight", detailNoiseWeight);
        material.SetVector ("shapeOffset", shapeOffset);
        material.SetVector ("detailOffset", detailOffset);
        material.SetVector ("detailWeights", detailNoiseWeights);
        material.SetVector ("shapeNoiseWeights", shapeNoiseWeights);
        material.SetVector ("phaseParams", new Vector4 (forwardScattering, backScattering, baseBrightness, phaseFactor));

        material.SetVector ("boundsMin", container.position - container.localScale / 2);
        material.SetVector ("boundsMax", container.position + container.localScale / 2);

        material.SetInt ("numStepsLight", numStepsLight);

        material.SetVector ("mapSize", new Vector4 (width, height, depth, 0));

        material.SetFloat ("timeScale", (Application.isPlaying) ? timeScale : 0);
        material.SetFloat ("baseSpeed", baseSpeed);
        material.SetFloat ("detailSpeed", detailSpeed);

        // Set debug params
        SetDebugParams ();

        material.SetColor ("colA", colA);
        material.SetColor ("colB", colB);

        // Bit does the following:
        // - sets _MainTex property on material to the source texture
        // - sets the render target to the destination texture
        // - draws a full-screen quad
        // This copies the src texture to the dest texture, with whatever modifications the shader makes
        buffer.Blit (src, dest, material, 0);
    }
    void SetDebugParams () {

        var noise = GameObject.Find("NoiseGenerator").gameObject.GetComponent<NoiseGenerator>();
        
        material.SetInt ("debugViewMode", noise.enableViewer ? 1 : 0);
        material.SetFloat ("debugNoiseSliceDepth", noise.sliceDepth);
        material.SetFloat ("debugTileAmount", noise.tileAmount);
        material.SetFloat ("viewerSize", noise.viewerSize);
        material.SetVector ("debugChannelWeight", noise.channelMask);
        material.SetInt ("debugGreyscale", 1);
    }
}

[Serializable]
public class AwakeEyes
{
    // Menu
    [HideInInspector] public bool active = false;
    [Range(0, 1)] public float processing = 0;

    // Private
    private CommandBuffer _buffer;
    private Camera _camera;

    private Material _material;
    [HideInInspector] public Shader shader;
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
        this._buffer = buffer;
        this._camera = camera;
        SetMaterial();
        if (shader != null)
        {
            // Dark non-visible area
            buffer.SetGlobalTexture("AwakeEyesSrcTex", src);
            buffer.GetTemporaryRT(ID_DarkTex, _camera.pixelWidth, _camera.pixelHeight, 0, FilterMode.Bilinear,
                RenderTextureFormat.Default);
            buffer.Blit(src, ID_DarkTex, _material, (int) Pass.Eyelid);
            src = ID_DarkTex;
            // Blur visible area
            if (processing < 0.95)
            {
                buffer.SetGlobalTexture("AwakeEyesSrcTex", src);
                buffer.GetTemporaryRT(ID_BlurTex, _camera.pixelWidth, _camera.pixelHeight, 0, FilterMode.Bilinear,
                    RenderTextureFormat.Default);
                buffer.Blit(src, ID_BlurTex, _material, (int) Pass.Blur);
                src = ID_BlurTex;
            }
        }
        // Render srcTex to camera
        buffer.SetGlobalTexture("AwakeEyesSrcTex", src);
        buffer.Blit(src, dest, _material, (int) Pass.Default);
    }

    private void SetMaterial()
    {
        shader = Resources.Load<Shader>("AwakeEyes");
        _material = new Material(shader);
        _upBound = 0.5f + processing * 0.7f;
        _lowBound = 0.5f - processing * 0.7f;
        _material.SetFloat("upBound", _upBound);
        _material.SetFloat("lowBound", _lowBound);
        _material.SetFloat("processing", processing);
        _material.SetFloat("pixelWidth", 1f / _camera.pixelWidth);
        _material.SetFloat("pixelHeight", 1f / _camera.pixelHeight);
    }
}