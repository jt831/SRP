using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum TextureSize
{
    _256 = 256, _512 = 512, _1024 = 1024,
    _2048 = 2048, _4096 = 4096, _8192 = 8192
}

public enum PCFMode
{
    HardShadow, PCSS, PCF3x3, PCF5x5, PCF7x7
}

[Serializable]
public class ShadowProperties
{
    public DirectionalShadowProperties directional;
    public OtherShadowProperties other;
}

[Serializable]
public class DirectionalShadowProperties
{
    [Min(0f)] public float distance = 100f;
    public TextureSize resolution = TextureSize._1024;
    public PCFMode Fliter = PCFMode.HardShadow;
    [Range(0.01f, 1f)]public float fade = 0.5f;
    [Range(0.01f, 3f)]public float shadowSlopBias = 0.5f;
    [Range(0.001f, 1f)]public float sampleBlockerDepthRadius = 0.001f;
    [Range(0f, 0.01f)]public float lightWidth = 0.001f;
    
    [Serializable]
    public struct Cascade
    {
        [Range(1, 4)] public int count;
        [Range(0.0f, 1.0f)] public float ratio1, ratio2, ratio3;
    }

    public Cascade cascade = new Cascade()
    {
        count = 1,
        ratio1 = 0.1f,
        ratio2 = 0.3f,
        ratio3 = 0.7f
    };
}

[Serializable]
public class OtherShadowProperties
{
    [Min(0f)] public float distance = 100f;
    public TextureSize resolution = TextureSize._1024;
    public PCFMode Fliter = PCFMode.HardShadow;
    [Range(0.01f, 1f)]public float fade = 0.5f;
    /*[Range(0.01f, 3f)]public float shadowSlopBias = 0.5f;
    [Range(0.001f, 1f)]public float sampleBlockerDepthRadius = 0.001f;
    [Range(0f, 0.01f)]public float lightWidth = 0.001f;*/
    
    [Serializable]
    public struct Cascade
    {
        [Range(1, 4)] public int count;
        [Range(0.0f, 1.0f)] public float ratio1, ratio2, ratio3;
    }

    public Cascade cascade = new Cascade()
    {
        count = 1,
        ratio1 = 0.1f,
        ratio2 = 0.3f,
        ratio3 = 0.7f
    };
}

