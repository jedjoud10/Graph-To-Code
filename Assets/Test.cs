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

    public float warperNoiseScale;
    public float warperNoiseAmplitude;
    public float scale23;

    public FractalNoise.FractalMode mode;
    public Voronoi.Type type;

    public override void Execute(Var<float3> position, out Var<float> density, out Var<uint> material) {
        Simplex noise = new Simplex() {
            scale = Var<float>.Inject(() => scale),
            amplitude = Var<float>.Inject(() => amplitude),
        };

        Simplex warperNoise = new Simplex() {
            scale = Var<float>.Inject(() => warperNoiseScale),
            amplitude = Var<float>.Inject(() => warperNoiseAmplitude),
        };

        FractalNoise fbm = new FractalNoise(noise, mode, Var<float>.Inject(() => lacunarity), Var<float>.Inject(() => persistence), octaves);

        // creates a texture caching this result?? maybe we can use this to calculate implicit diffs
        Var<float> injected = Var<float>.Inject(() => offset);

        Var<float2> owo = position.Swizzle2(Utils.Swizzle2Mode.XZ);
        Warper warper = new Warper(warperNoise);
        Var<float2> nyaa = warper.Warp(owo);


        Voronoi noise2 = new Voronoi(type, 0.5f, 0.5f) { scale = Var<float>.Inject(() => scale), amplitude = Var<float>.Inject(() => scale23), };
        var temp = position.Y() - 10.0f + fbm.Evaluate(nyaa) + injected + noise2.Evaluate(owo);
        density = temp;
        material = 0;
    }
}