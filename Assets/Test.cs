using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> offset;
    public Inject<float> mul;
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float2> warpingScale;
    public Inject<float2> warpingScale2;
    public Inject<float> scale2;
    public Inject<float> amplitude2;

    public override void Execute(Variable<float3> position, out Variable<float> density) {
        Simplex simplex = new Simplex { amplitude = amplitude, scale = scale };
        Simplex simplex2 = new Simplex { amplitude = amplitude2, scale = scale2 };
        Warper warper = new Warper() { noisinator = simplex, warpingScale = warpingScale, warpingScale2 = warpingScale2 };
        var output = position.Swizzle<float>("y");

        var temp = position.Swizzle<float2>("xz");
        output += simplex2.Evaluate(warper.Warpinate(temp));

        
        density = output + offset;
        density *= mul;
        /*
        density = null;
        material = null;
        Simplex noise = new Simplex() {
            scale = Var<float>.Inject(() => scale),
            amplitude = Var<float>.Inject(() => amplitude),
        };

        Simplex warperNoise = new Simplex() {
            scale = scale5,
            amplitude = warperNoiseAmplitude,
        };

        FractalNoise fbm = new FractalNoise(noise, mode, Var<float>.Inject(() => lacunarity), Var<float>.Inject(() => persistence), octaves);

        // creates a texture caching this result?? maybe we can use this to calculate implicit diffs
        Var<float> injected = Var<float>.Inject(() => offset);

        Var<float2> owo = position.Swizzle2(Utils.Swizzle2Mode.XZ);
        Warper warper = new Warper(warperNoise);
        Var<float2> nyaa = warper.Warp(owo);


        Voronoi noise2 = new Voronoi(type, 0.5f, 0.5f) { scale = Var<float>.Inject(() => scale), amplitude = scale23, };
        var temp = position.Y() - 10.0f + fbm.Evaluate(nyaa) + injected + noise2.Evaluate(owo);

        SdfBox box = new SdfBox(Var<float3>.Inject(() => bounds.center), Var<float3>.Inject(() => bounds.extents));

        //density = box.Evaluate(position);
        density = temp;
        material = 0;
        */
    }

}