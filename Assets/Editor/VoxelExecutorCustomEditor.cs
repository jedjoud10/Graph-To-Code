using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(VoxelGraphExecutor), true)]
public class VoxelExecutorCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var script = (VoxelGraphExecutor)target;

        if (GUILayout.Button("Randomize Seed")) {
            script.RandomizeSeed();
        }
    }
}