using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelGraphPreview), true)]
public class VoxelGraphPreviewCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
    }

    private void OnSceneViewGUI(SceneView sv) {
        var script = (VoxelGraphPreview)target;

        if (!script.previewing || Application.isPlaying || target == null)
            return;

        var executor = script.GetComponent<VoxelGraphExecutor>();
        executor.ExecuteShader(script.size);

        Handles.matrix = Matrix4x4.Scale(Vector3.one * script.size);
        Handles.DrawTexture3DVolume(executor.VoxelTexture, script.opacity, script.quality, useColorRamp: true, customColorRamp: script.gradient, filterMode: script.filterMode);
    }

    void OnEnable() {
        SceneView.duringSceneGui += OnSceneViewGUI;
    }
}