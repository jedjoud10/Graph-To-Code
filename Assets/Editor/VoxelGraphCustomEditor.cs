using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelGraph), true)]
public class VoxelGraphCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        var script = (VoxelGraph)target;

        if (GUILayout.Button("Recompile")) {
            script.Compile();
        }

        /*
        if (GUILayout.Button("Randomize Seed")) {
            //script.RandomizeSeed();
            //script.SoftRecompile();
        }
        */
    }
}