
using Unity.Mathematics;

public class TestVoxelGraph : VoxelGraph {
    public override void Execute(Var<float3> position, out Var<float> density, out Var<uint> material) {
        Noise noise = new Noise(1.0f, 0.02f) {
            type = Noise.Type.Simplex
        };
        density = position.y() + noise.Evaluate(position);
        material = 0;
    }
}

