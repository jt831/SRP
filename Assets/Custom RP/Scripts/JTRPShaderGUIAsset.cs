using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VarMaterial : MonoBehaviour
{
    [SerializeField] [ColorUsage(true, true)]
    private Color _Basecolor;

    [SerializeField] private float _AThreshold;

    private int BaseColorID = Shader.PropertyToID("_BaseColor");
    private int AThresholdID = Shader.PropertyToID("_AThreshold");
    private static MaterialPropertyBlock _materialPropertyBlock;

    void Active()
    {
        if (_materialPropertyBlock == null) _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetColor(BaseColorID, _Basecolor);
        _materialPropertyBlock.SetFloat(AThresholdID, _AThreshold);
        GetComponent<Renderer>().SetPropertyBlock(_materialPropertyBlock);
    } 
}
