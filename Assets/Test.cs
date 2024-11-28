using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public override void Execute(Var<float3> position, out Var<float> density, out Var<uint> material) {
        Noise noise = new Noise(1.0f, 0.02f) {
            type = Noise.Type.Simplex
        };
        density = position.y();
        var a = noise.Evaluate(position);
        material = 0;
    }

    void Start() {
        string output = Transpile();
        Debug.Log(output);
    }
}