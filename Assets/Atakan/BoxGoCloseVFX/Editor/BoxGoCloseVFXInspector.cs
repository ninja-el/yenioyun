using UnityEditor;
using UnityEngine;
using BoxGo.VFX;

[CustomEditor(typeof(BoxGoCloseVFX))]
public class BoxGoCloseVFXInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Assign Box Visual, then enter Play Mode to preview. Keep the effect outside the box hierarchy. Box Width is in world units.",MessageType.Info);
        var fx=(BoxGoCloseVFX)target;
        using(new EditorGUI.DisabledScope(!Application.isPlaying || fx.IsPlaying))
            if(GUILayout.Button("PLAY CLOSE + POP + EXIT")) fx.Play();
    }
}
