using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> lacunarity;
    public Inject<float> persistence;
    public Inject<float> lacunarity2;
    public Inject<float> persistence2;
    public InlineTransform transform1;
    public InlineTransform transform2;
    public Gradient gradient;
    public Gradient heightGradient;
    public Gradient spikeGradient2;
    public Inject<float> minRange;
    public Inject<float> maxRange;
    public Inject<float> minRange2;
    public Inject<float> maxRange2;
    public Inject<float> minRange3;
    public Inject<float> maxRange3;
    public Inject<float> scale2;
    public Inject<float> amplitude2;
    public Inject<float> spikeOffset;
    public FractalNoise.FractalMode mode;
    public int reduction;
    [Range(1, 10)]
    public int octaves;
    [Range(1, 10)]
    public int octaves2;

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
        var fractal2 = new FractalNoise(simplex, FractalNoise.FractalMode.Sum, lacunarity2, persistence2, octaves2).Evaluate(pos3.Swizzle<float2>("xz"));
        var diagonals = new Ramp<float>(spikeGradient2, minRange3, maxRange3).Evaluate(-(fractal2 - spikeOffset).Min(0.0f));

        

        density = cached + output - diagonals;

        var baseColor = new Ramp<float3>(heightGradient, minRange2, maxRange2, remapOutput: false).Evaluate(pos2.Swizzle<float>("y"));
        var otherColor = new float3(0.2);
        color = Variable<float3>.Lerp(baseColor, otherColor, diagonals.Swizzle<float3>("xxx"), true);
    }
}