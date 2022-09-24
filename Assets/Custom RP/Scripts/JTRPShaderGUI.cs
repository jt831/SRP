using UnityEngine;
using UnityEditor;

public class JTRPShaderGUI : ShaderGUI
{
    private MaterialEditor _editor;
    private Object[] _materials;
    private MaterialProperty[] _properties;
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        this._editor = materialEditor;
        this._properties = properties;
        this._materials = materialEditor.targets;
        BakeEmission();
    }

    bool Clipping {
        set => SetToggle( "_CLIPPING", value);
    }

    void SetProperties(string name, float value)
    {
        FindProperty(name, _properties).floatValue = value;
    }

    void SetToggle(string keyword, bool enable)
    {
        if (enable)
        {
            foreach (Material material in _materials)
            {
                material.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material material in _materials)
            {
                material.DisableKeyword(keyword);
            }
        }
    }

    void BakeEmission()
    {
        EditorGUI.BeginChangeCheck();
        _editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck()) {
            foreach (Material m in _editor.targets) {
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
}
