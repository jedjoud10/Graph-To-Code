using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public float offset;

    [Range(0, 1)]
    public float scale;

    [Range(0, 1)]
    public float scale2;

    [Range(-10, 10)]
    public float amplitude;

    [Range(1, 5)]
    public int octaves;
    
    [Range(0, 2)]
    public float persistence;

    [Range(0, 2)]
    public float lacunarity;

    public FractalNoise.FractalMode mode;
    public Noise.Type type;

    public override void Execute(Var<float3> position, out Var<float> density, out Var<uint> material) {
        Noise noise = new Noise() {
            type = type,
            scale = Var<float>.Inject(() => scale),
            amplitude = Var<float>.Inject(() => amplitude),
        };

        FractalNoise fbm = new FractalNoise(noise, mode, Var<float>.Inject(() => lacunarity), Var<float>.Inject(() => persistence), octaves);

        // creates a texture caching this result?? maybe we can use this to calculate implicit diffs
        // fbm.Cached()

        Var<float> injected = Var<float>.Inject(() => offset);
        var temp = position.y() - 10.0f + fbm.Evaluate(position.Swizzle2(Utils.Swizzle2Mode.XZ)) + injected;

        density = (new Noise(1.0f, Var<float>.Inject(() => scale2)).Evaluate(position) * 0.5f + 0.5f).Mix(temp, 0.0f);
        material = 0;
    }

    public override int RecompilationHash() {
        return System.HashCode.Combine(octaves.GetHashCode(), mode.GetHashCode(), type.GetHashCode());
    }
}