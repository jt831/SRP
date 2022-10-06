using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/WorleyNoiseSettings")]
public class WorleyNoiseSettings : ScriptableObject {

    public int seed = 1;
    [HideInInspector] public int resolution = 128;
    [Range (1, 50)] public int pointsPerAxis = 5;
    [Range (0, 1)] public float weight = 0.5f;
    public bool invert = true;

}