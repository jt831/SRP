using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (NoiseGenerator))]
public class NoiseGenEditor : Editor 
{

    NoiseGenerator noise;
    Editor noiseSettingsEditor;

    void OnEnable () {
        noise = (NoiseGenerator) target;
    }
    public override void OnInspectorGUI () 
    {
        DrawDefaultInspector ();

        if (GUILayout.Button ("Update")) {
            noise.ManualUpdate ();
            EditorApplication.QueuePlayerLoopUpdate ();
        }

        if (GUILayout.Button ("Save"))
        {
            string pass = "Assets/Resources/" + NoiseGenerator.worleyName + ".asset";
            RenderTexture worley = Resources.Load<RenderTexture>(NoiseGenerator.worleyName);
            if (worley != null) AssetDatabase.DeleteAsset(pass);
            AssetDatabase.CreateAsset(noise.noiseTexture, pass);
            /*Save3D save3D = new Save3D();
            save3D.Save(noise.noiseTexture, NoiseGenerator.worleyName);*/
        }

        if (GUILayout.Button ("Load")) {
            noise.Load (NoiseGenerator.worleyName, ref noise.noiseTexture);
            EditorApplication.QueuePlayerLoopUpdate ();
        }
    }
}