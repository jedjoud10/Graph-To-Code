using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
[ExecuteInEditMode]
public abstract class VoxelGraph : MonoBehaviour {
    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Var<float3> position, out Var<float> density, out Var<uint> material);

    // This transpile the voxel graph into HLSL code that can be executed on the GPU
    // This can be done outside the editor, but shader compilation MUST be done in editor
    public string Transpile() {
        ShaderManager manager = new ShaderManager();
        ShaderManager.singleton = manager;
        Var<float3> position = Var<float3>.CreateFromName("position", "__");
        Execute(position, out Var<float> density, out Var<uint> material);


        ShaderManager.singleton = null;

        string output = "";

        foreach (var item in manager.lines) {
            output = output + item + "\n";
        }

        return output;
    }
}