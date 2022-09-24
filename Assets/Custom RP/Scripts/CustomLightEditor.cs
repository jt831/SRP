using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VarMaterial : MonoBehaviour
{
    [SerializeField] [ColorUsage(true, true)]
    private Color _Basecolor;
    private static MaterialPropertyBlock _materialPropertyBlock;

    void OnValidate()
    {
        if (_materialPropertyBlock == null) _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetColor(Shader.PropertyToID("_BaseColor"), _Basecolor);
        GetComponent<Renderer>().SetPropertyBlock(_materialPropertyBlock);
    } 
}
