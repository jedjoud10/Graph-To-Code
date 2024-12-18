using System;
using Unity.Mathematics;
using UnityEngine;

public class Test2 : VoxelGraph {
    public InlineTransform transform1;
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> scale2;
    public Inject<float> amplitude2;
    public Inject<float> smoothing;

    public override void Execute(Variable<float3> position, out Variable<float> density, ref Variable<float3> color) {
        var transformer = new ApplyTransformation(transform1);
        var projected = transformer.Transform(position);
        var y = projected.Swizzle<float>("y");
        var xz = projected.Swizzle<float2>("xz");
        var evaluated = Noise.Simplex(xz, scale, amplitude).SmoothAbs(smoothing);

        density = y + evaluated;
        density += Noise.Simplex(projected, scale2, amplitude2);
        color = (evaluated / amplitude).Swizzle<float3>("xxx");
    }
}