using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelGraph), true)]
public class VoxelGraphCustomEditor : Editor {
    private bool temp = false;
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var script = (VoxelGraph)target;

        if (GUILayout.Button("Recompile")) {
            script.Compile();
        }
    }

    private void OnSceneViewGUI(SceneView sv) {
        var script = (VoxelGraph)target;

        if (Application.isPlaying || target == null)
            return;

        var executor = script.GetComponent<VoxelGraphExecutor>();
        var visualizer = script.GetComponent<DensityVisualizer>();
        visualizer.ConvertToMeshAndRender(executor.VoxelTexture);
    }

    void OnEnable() {
        var script = (VoxelGraph)target;
        var executor = script.GetComponent<DensityVisualizer>();
        executor.InitializeForSize();

        if (!temp) {
            SceneView.duringSceneGui += OnSceneViewGUI;
            temp = true;
        }
    }

    void OnDisable() {
        SceneView.duringSceneGui -= OnSceneViewGUI;
        var script = (VoxelGraph)target;
        var executor = script.GetComponent<DensityVisualizer>();
        executor.Killnate();
    }
}