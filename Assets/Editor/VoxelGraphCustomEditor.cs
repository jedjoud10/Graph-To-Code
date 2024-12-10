using Palmmedia.ReportGenerator.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(VoxelGraph), true)]
public class VoxelGraphCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var script = (VoxelGraph)target;

        if (GUILayout.Button("Recompile")) {
            script.Compile();
        }
    }
}