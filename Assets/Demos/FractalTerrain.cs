using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class FractalTerrain : VoxelGraph {
    // Main transform
    public InlineTransform transform1;

    // Noise parameter for the simplex 2D noise
    public Inject<float> scale;
    public Inject<float> amplitude;
    public Inject<float> persistence;
    public Inject<float> lacunarity;
    public Fractal<float2>.FractalMode mode;
    public Gradient gradient;
    [Range(1, 10)]
    public int octaves;

    public override void Execute(Variable<float3> position, Variable<uint3> id, out Variable<float> density, out Variable<float3> color) {
        // Project the position using the main transformation
        var transformer = new ApplyTransformation(transform1);
        var projected = transformer.Transform(position);

        // Split components
        var y = projected.Swizzle<float>("y");
        var xz = projected.Swizzle<float2>("xz");

        // Create fractal 2D simplex noise
        Fractal<float2> fractal = new Fractal<float2>(new Simplex(scale, amplitude), mode, lacunarity, persistence, octaves);
        density = y + new Ramp<float>(gradient, -(Variable<float>)amplitude, amplitude).Evaluate(fractal.Evaluate(xz));

        // Simple color based on height uwu
        color = ((y / amplitude) * 0.5f + 0.5f).Broadcast<float3>();
    }
}