using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(JTRenderPipelineAsset))]
public class CustomLightEditor : LightEditor
{
   public override void OnInspectorGUI()
   {
      base.OnInspectorGUI();
      // Add innerAngle in SpotLight Inspector
      if (!settings.lightType.hasMultipleDifferentValues &&
          (LightType)settings.lightType.enumValueIndex == LightType.Spot)
      {
         settings.DrawInnerAndOuterSpotAngle();
         settings.ApplyModifiedProperties();
      }
   }
}
