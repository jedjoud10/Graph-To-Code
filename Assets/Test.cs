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
        Simplex simplex = new Simplex { amplitude = amplitude, scale = scale };
        //Voronoi voronoi = new Voronoi { amplitude = amplitude, scale = scale, type = type };
        //FractalNoise fractal = new FractalNoise(voronoi, mode, octaves);
        var test = simplex.Evaluate(temp) * 0.25f;
        var cached = test.Cached("A");
        cached += 0.235f;
        var doubleCached = cached.Cached("B");


        //density = cached + doubleCached + output + offset;
        density = output + doubleCached;
        density *= mul;
    }

}