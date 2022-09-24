using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ManyInstances : MonoBehaviour
{
    [SerializeField] private Mesh mesh = default;
    [SerializeField] private Material material = default;

    private Matrix4x4[] _matrix4X4s = new Matrix4x4[1023];
    private Vector4[] _colors= new Vector4[1023];
    private MaterialPropertyBlock _materialPropertyBlock;
    private void Awake()
    {
        for(int i = 0;i < _matrix4X4s.Length;i ++)
        {
            _matrix4X4s[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, Quaternion.identity, Vector3.one);
            _colors[i] = new Color(Random.value, Random.value, Random.value, 1);
        }

        if (_materialPropertyBlock == null) _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetVectorArray(Shader.PropertyToID("_BaseColor"), _colors);
    }

    private void Update()
    {
        Graphics.DrawMeshInstanced(mesh, 0, material, _matrix4X4s, _matrix4X4s.Length, _materialPropertyBlock);
    }
}
