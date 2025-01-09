using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelGraphExecutor), true)]
public class VoxelGraphExecutorCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var script = (VoxelGraphExecutor)target;

        if (GUILayout.Button("Randomize Seed")) {
            script.RandomizeSeed();
        }
    }
}