using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> lacunarity;
    public Inject<float> persistence;
    public InlineTransform obj;
    public Gradient gradient;
    public Inject<float> minRange;
    public Inject<float> maxRange;
    public FractalNoise.FractalMode mode;
    public int reduction;
    [Range(0, 10)]
    public int octaves;

    public override void Execute(Variable<float3> position, out Variable<float> density) {
        var transformer = new ApplyTransformation(obj);
        var pos2 = transformer.Transform(position);
        var output = pos2.Swizzle<float>("y");
        var temp = pos2.Swizzle<float2>("xz");

        var simplex = new Voronoi(scale, amplitude);
        var fractal = new FractalNoise(simplex, mode, lacunarity, persistence, octaves).Evaluate(temp);
        var ramp = new Ramp<float>(gradient, minRange, maxRange);
        var asdfa = ramp.Evaluate(fractal).Cached(reduction, "xz");

        density = asdfa + output;
    }
}