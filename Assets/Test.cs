using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> offset;
    public Inject<float> mul;
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> scale2;
    public Inject<float> amplitude2;
    public Voronoi.Type type;
    public FractalNoise.FractalMode mode;
    public int octaves;

    [Range(0, 3)]
    public int reduction;

    public override void Execute(Variable<float3> position, out Variable<float> density) {
        var output = position.Swizzle<float>("y");
        var temp = position.Swizzle<float2>("xz");

        Simplex simplex = new Simplex { amplitude = amplitude, scale = scale };
        FractalNoise fractal = new FractalNoise(simplex, mode, octaves);

        // cache the result of the first noise (diff. compute kernel -> lower res texture)
        var test = fractal.Evaluate(temp);
        var cached = test.Cached(reduction, 1.0f);

        // high-res noise (applied at base level)
        Simplex simplex2 = new Simplex { amplitude = amplitude2, scale = scale2 };
        cached += simplex2.Evaluate(position);

        density = cached + output;
        density *= mul;
    }

}