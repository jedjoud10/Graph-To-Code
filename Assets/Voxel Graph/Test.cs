using System;
using Unity.Mathematics;
using UnityEngine;

public class Test : VoxelGraph {
    public Inject<float> offset;
    public Inject<float> mul;
    public Inject<float> scale;
    public Inject<float> amplitude;
    public InlineTransform obj;

    public override void Execute(Variable<float3> position, out Variable<float> density) {
        var transformer = new ApplyTransformation(obj);
        var pos2 = transformer.Transform(position);
        var output = pos2.Swizzle<float>("y");
        var temp = pos2.Swizzle<float2>("xz");

        var simplex =  Noise.Simplex(temp, scale, amplitude);
        density = simplex + output;
        density *= mul;
    }
}