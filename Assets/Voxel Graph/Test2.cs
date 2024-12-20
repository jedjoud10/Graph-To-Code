using Unity.Mathematics;

public class Test2 : VoxelGraph {
    // Main transform
    public InlineTransform transform1;

    // Noise parameter for the ridged 2D noise
    public Inject<float> scale;
    public Inject<float> amplitude;

    // 3D noise parameters
    public Inject<float> scale2;
    public Inject<float> amplitude2;

    // Smoothing factor for the smooth abs function
    public Inject<float> smoothing;

    public override void Execute(Variable<float3> position, Variable<uint3> id, out Variable<float> density, out Variable<float3> color) {
        // Project the position using the main transformation
        var transformer = new ApplyTransformation(transform1);
        var projected = transformer.Transform(position);

        // Split components
        var y = projected.Swizzle<float>("y");
        var xz = projected.Swizzle<float2>("xz");

        // Calculate simple 2D noise
        var evaluated = Noise.Simplex(xz, scale, amplitude).SmoothAbs(smoothing);
        var overlay = Noise.Simplex(projected, scale2, amplitude2);

        // Test
        var distances = new CellularTiler<float2>().Tile(xz);

        // Sum!!!
        density = y + SdfOps.Union(overlay + y + evaluated - (Variable<float>)amplitude / 2.0f, 0.0f);
        density = SdfOps.Union(density, distances);
        color = (evaluated / amplitude).Swizzle<float3>("xxx");
    }
}