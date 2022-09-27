using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class PostFX
{
    public bool active = false;
    public PFXSettings settings;
}

[CreateAssetMenu(menuName = "Rendering/PfxSettings")]
public class PFXSettings : ScriptableObject
{
    public Shader shader;
}