using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PostFX
{
    public bool active = false;
    public PFXSettings settings;
}

[CreateAssetMenu(menuName = "Rendering/PFXSettings")]
public class PFXSettings : ScriptableObject
{
    public Shader shader;
}