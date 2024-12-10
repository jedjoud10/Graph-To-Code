using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> offset;
    public Inject<float> mul;
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Voronoi.Type type;
    public FractalNoise.FractalMode mode;
    public int octaves;

    public override void Execute(Variable<float3> position, out Variable<float> density) {
        var output = position.Swizzle<float>("y");
        var temp = position.Swizzle<float2>("xz");
        Voronoi voronoi = new Voronoi { amplitude = amplitude, scale = scale, type = type };
        FractalNoise fractal = new FractalNoise(voronoi, mode, octaves);
        output += fractal.Evaluate(temp);
        density = output + offset;
        density *= mul;
    }

}