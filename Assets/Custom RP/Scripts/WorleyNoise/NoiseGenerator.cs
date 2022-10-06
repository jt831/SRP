using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class ConstNoiseValues
{
    public const int maxWorly = 4;
}
public class NoiseGenerator : MonoBehaviour 
{
    const int numThreads = 8;
    public const string worleyName = "WorleyNoise";
    // Inspector
    public WorleyNoiseSettings[] WorleySettingsArray;
    [Header ("Viewer Settings")]
    public bool enableViewer;
    [Range (0, 1)] public float sliceDepth;
    [Range (1, 5)] public float tileAmount = 1;
    [Range (0, 1)] public float viewerSize = 1;

    // Internal
    bool updateNoise = true;
    private int resolution = 1;
    private ComputeShader noiseCompute;
    private ComputeShader copyCompute;
    List<ComputeBuffer> buffersToRelease;
    [HideInInspector] public RenderTexture noiseTexture;
    [HideInInspector] public Vector4 channelMask = new Vector4(1, 0, 0, 0);
    [HideInInspector] public bool useStoredTex;
    
    public void ManualUpdate () 
    {
        updateNoise = true;
        UpdateNoise ();
    }
    private enum Kernel
    {
        GetWorley, Normalize
    }
    public void UpdateNoise () 
    {
        // Create or update worleyTexture
        ValidateParamaters ();
        CreateTexture (ref noiseTexture, resolution, worleyName);
        
        if (updateNoise && noiseCompute) 
        {
            updateNoise = false;
            buffersToRelease = new List<ComputeBuffer> ();
            
            var timer = System.Diagnostics.Stopwatch.StartNew ();
            var minMaxBuffer = CreateBuffer(new int[] {int.MaxValue, 0}, sizeof(int), "minMaxWorley");
            // Prepare values to getWorley
            noiseCompute.SetInt("numWorley", WorleySettingsArray.Length);
            noiseCompute.SetInt("resolution", resolution);
            noiseCompute.SetTexture((int)Kernel.GetWorley, "Result", noiseTexture);
            noiseCompute.SetVector("channelMask", channelMask);
            noiseCompute.SetBool("invertNoise", WorleySettingsArray[0].invert);
            for (int i = 0; i < WorleySettingsArray.Length; i++)
            {
                SetRandomPointsBuffer(WorleySettingsArray[i], "points" + i);
                noiseCompute.SetFloat("weight" + i, WorleySettingsArray[i].weight);
                noiseCompute.SetInt("pointsPerAxis" + i, WorleySettingsArray[i].pointsPerAxis);
            }
            for (int i = WorleySettingsArray.Length; i < ConstNoiseValues.maxWorly; i++)
            {
                SetRandomPointsBuffer(WorleySettingsArray[0], "points" + i);
                noiseCompute.SetFloat("weight" + i, 0);
                noiseCompute.SetInt("pointsPerAxis" + i, 0);
            }
                
            // Get worley
            int numGroups = Mathf.CeilToInt(resolution / (float)numThreads);
            noiseCompute.Dispatch((int)Kernel.GetWorley, numGroups, numGroups, numGroups);
            // Normalize worley
            noiseCompute.SetBuffer((int)Kernel.Normalize, "minMaxWorley", minMaxBuffer);
            noiseCompute.SetTexture((int)Kernel.Normalize, "Result", noiseTexture);
            noiseCompute.Dispatch((int)Kernel.Normalize, numGroups, numGroups, numGroups);
                
            // Get minmax data just to force main thread to wait until compute shaders are finished.
            // This allows us to measure the execution time.
            var minMax = new int[2];
            minMaxBuffer.GetData(minMax);
            Debug.Log($"Noise Generation: {timer.ElapsedMilliseconds}ms");
                
            // Add buffer to Release
            buffersToRelease.Add(minMaxBuffer);
            
            // Release buffers
            foreach (var buffer in buffersToRelease) {
                buffer.Release ();
            }
        }
    }
    void SetRandomPointsBuffer (WorleyNoiseSettings settings, string bufferName) 
    {
        var prng = new System.Random (settings.seed);
        int pointPerAxis = settings.pointsPerAxis;
        var points = new Vector3[pointPerAxis * pointPerAxis * pointPerAxis];
        float cellSize = 1f / pointPerAxis;

        for (int x = 0; x < pointPerAxis; x++) {
            for (int y = 0; y < pointPerAxis; y++) {
                for (int z = 0; z < pointPerAxis; z++) {
                    float randomX = (float) prng.NextDouble ();
                    float randomY = (float) prng.NextDouble ();
                    float randomZ = (float) prng.NextDouble ();
                    Vector3 randomOffset = new Vector3 (randomX, randomY, randomZ) * cellSize;
                    Vector3 cellCorner = new Vector3 (x, y, z) * cellSize;
                    
                    int index = z + pointPerAxis * (y + x * pointPerAxis);
                    points[index] = cellCorner + randomOffset;
                }
            }
        }
        CreateBuffer (points, sizeof(float) * 3, bufferName);
    }

    // Create buffer with some data, and set in shader. Also add to list of buffers to be released
    ComputeBuffer CreateBuffer (System.Array data, int stride, string bufferName, int kernel = 0) 
    {
        var buffer = new ComputeBuffer (data.Length, stride, ComputeBufferType.Structured);
        buffersToRelease.Add (buffer);
        buffer.SetData (data);
        noiseCompute.SetBuffer (kernel, bufferName, buffer);
        return buffer;
    }

    void CreateTexture (ref RenderTexture texture, int resolution, string name) 
    {
        // Create a 3DTexture with default format
        var format = GraphicsFormat.R32G32B32A32_SFloat;
        if (texture == null || !texture.IsCreated () || texture.width != resolution 
            || texture.height != resolution || texture.volumeDepth != resolution
            || texture.graphicsFormat != format) {

            if (texture != null) {
                texture.Release ();
            }
            texture = new RenderTexture (resolution, resolution, 0);
            texture.graphicsFormat = format;
            texture.volumeDepth = resolution;
            texture.enableRandomWrite = true;
            texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture.name = name;

            texture.Create ();
            //Load (name, ref texture);
        }
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
    }
    public void Load (string saveName, ref RenderTexture target) 
    {
        // Load saved texture and copy it to targetTexture
        RenderTexture savedTex = Resources.Load<RenderTexture>(saveName);
        if (savedTex != null && savedTex.width == target.width) target = savedTex;
    }
    void ValidateParamaters () 
    {
        for (int i = 0; i < WorleySettingsArray.Length; i++)
        {
            this.resolution = Mathf.Max(resolution, WorleySettingsArray[i].resolution);
        }
        this.noiseCompute = Resources.Load<ComputeShader>("NoiseGen");
        this.copyCompute  = Resources.Load<ComputeShader>("Copy");
    }
}