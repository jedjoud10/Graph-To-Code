using System;
using Unity.Mathematics;
using UnityEngine;

public class Test2 : VoxelGraph {
    public InlineTransform transform1;
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> smoothing;

    public override void Execute(Variable<float3> position, out Variable<float> density, ref Variable<float3> color) {
        var transformer = new ApplyTransformation(transform1);
        var projected = transformer.Transform(position);
        var y = projected.Swizzle<float>("y");
        var xz = projected.Swizzle<float2>("xz");
        var simplex = new Simplex(scale, amplitude);
        var evaluated = simplex.Evaluate(xz).SmoothAbs(smoothing);

        density = y + evaluated;
        color = (evaluated / amplitude).Swizzle<float3>("xxx");
    }
}