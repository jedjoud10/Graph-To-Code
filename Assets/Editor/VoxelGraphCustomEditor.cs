using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(VoxelGraph), true)]
public class CustomVoxelGraphEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var script = (VoxelGraph)target;

        if (GUILayout.Button("Recompile")) {
            script.Compile();
        }
    }

    private void OnSceneViewGUI(SceneView sv) {
        var script = (VoxelGraph)target;

        if (!script.preview)
            return;

        script.ExecuteShader();
        if (Application.isPlaying || script.texture == null) return;

        Handles.matrix = Matrix4x4.Scale(Vector3.one * 64.0f);
        Handles.DrawTexture3DVolume(script.texture, script.previewOpacity, script.previewQuality, useColorRamp: true, customColorRamp: script.previewGradient, filterMode: FilterMode.Point);
    }

    void OnEnable() {
        SceneView.duringSceneGui += OnSceneViewGUI;
    }
}