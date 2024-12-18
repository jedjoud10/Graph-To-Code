using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> lacunarity;
    public Inject<float> persistence;
    public InlineTransform transform1;
    public InlineTransform transform2;
    public Gradient gradient;
    public Gradient heightGradient;
    public Inject<float> minRange;
    public Inject<float> maxRange;
    public Inject<float> minRange2;
    public Inject<float> maxRange2;
    public Inject<float> scale2;
    public Inject<float> amplitude2;
    public FractalNoise.FractalMode mode;
    public int reduction;
    [Range(1, 10)]
    public int octaves;

    public override void Execute(Variable<float3> position, out Variable<float> density, ref Variable<float3> color) {
        var transformer = new ApplyTransformation(transform1);
        var pos2 = transformer.Transform(position);
        var output = pos2.Swizzle<float>("y");
        var temp = pos2.Swizzle<float2>("xz");

        // Simple cached 2D base layer
        var voronoi = new Voronoi(scale, amplitude);
        var fractal = new FractalNoise(voronoi, mode, lacunarity, persistence, octaves).Evaluate(temp);
        var ramp = new Ramp<float>(gradient, minRange, maxRange);
        var cached = ramp.Evaluate(fractal).Cached(reduction, "xz");

        // 3D secondary transformed layer
        var transformer2 = new ApplyTransformation(transform2);
        var pos3 = transformer2.Transform(pos2);
        var simplex = new Simplex(scale2, amplitude2);
        var diagonals = simplex.Evaluate(pos3.Swizzle<float2>("xz")).Max(0.0f);

        density = cached + output + diagonals;
        color = new Ramp<float3>(heightGradient, minRange2, maxRange2, remapOutput: false).Evaluate(pos2.Swizzle<float>("y"));
    }
}