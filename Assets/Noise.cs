
// Simple noise class that allows us to write and combine noise types to evaluate them
using Unity.Mathematics;
using UnityEngine.UIElements;

public class Noise {
    public enum Type {
        Simplex,
        Perlin,
        VoronoiF1,
        VoronoiF2,
        Erosion
    }

    public Noise(Var<float> intensity, Var<float> scale) {
        this.amplitude = intensity;
        this.scale = scale;
    }

    public Noise() {
        this.amplitude = 1.0f;
        this.scale = 1.0f;
    }

    public Type type;
    public Var<float> amplitude;
    public Var<float> scale;

    public string Internal(string name) {
        string inner = $"({name}) * {scale.name}";
        string suffix = "";
        string fn = "";

        switch (type) {
            case Type.Simplex:
                fn = "snoise";
                break;
            case Type.Perlin:
                fn = "pnoise";
                break;
            case Type.VoronoiF1:
                fn = "cellular";
                suffix = ".x - 0.5";
                break;
            case Type.VoronoiF2:
                fn = "cellular";
                suffix = ".y - 0.5";
                break;
            case Type.Erosion:
                break;
        }
        return $"({fn}({inner}){suffix}) * {amplitude.name}";
    }

    // Evaluate the noise at the specific point
    public virtual Var<float> Evaluate<T>(Var<T> position) {
        return Var<float>.CreateFromName(position.name + "_noised", Internal(position.name));
    }
}

public class Coherent : Noise {
    public enum Type {
        Simplex,
        Perlin,
    }
}

public class Voronoi : Noise {

}

// Fractal noise is a type of noise that implement fBm (either Ridged, Billow, or Sum mode)
public class FractalNoise {
    public enum FractalMode {
        Ridged,
        Billow,
        Sum,
        Mul,
    }


    public FractalMode mode;
    public Var<float> lacunarity;
    public Var<float> persistence;
    public int octaves;
    private Noise noise;

    public FractalNoise(Noise noise, FractalMode mode, Var<float> lacunarity, Var<float> persistence, int octaves) {
        this.noise = noise;
        this.lacunarity = lacunarity;
        this.persistence = persistence;
        this.mode = mode;
        this.octaves = octaves;
    }

    public FractalNoise(Noise noise, FractalMode mode, int octaves) {
        this.noise = noise;
        this.lacunarity = 2.0f;
        this.persistence= 0.5f;
        this.octaves = octaves;
        this.mode = mode;
    }

    public Var<float> Evaluate<T>(Var<T> position) {
        Var<float> sum = Var<float>.CreateFromName(position.name + "_noised_fbm", mode == FractalMode.Mul ? "1.0" : "0.0");
        Var<float> _s = Var<float>.CreateFromName(position.name + "_noised_fbm_scale", "1.0");
        Var<float> _a = Var<float>.CreateFromName(position.name + "_noised_fbm_amplitude", "1.0");

        ShaderManager.singleton.AddLine($@"
[unroll]
for(uint i = 0; i < {octaves}; i++) {{
");
        string test = $"{position.name} * {_s.name} + hash31(float(i))";

        switch (mode) {
            case FractalMode.Billow:
                ShaderManager.singleton.AddLine($"    {sum.name} += ({noise.amplitude.name} - abs({noise.Internal(test)})) * {_a.name};");
                break;
            case FractalMode.Ridged:
                ShaderManager.singleton.AddLine($"    {sum.name} += abs({noise.Internal(test)}) * {_a.name};");
                break;
            case FractalMode.Sum:
                ShaderManager.singleton.AddLine($"    {sum.name} += {noise.Internal(test)} * {_a.name};");
                break;
            case FractalMode.Mul:
                ShaderManager.singleton.AddLine($"    {sum.name} *= {noise.Internal(test)} * {_a.name};");
                break;
        }

        ShaderManager.singleton.AddLine($@"
    {_s.name} *= {lacunarity.name};
    {_a.name} *= {persistence.name};
");
        ShaderManager.singleton.AddLine("}");

        return sum;
    }
}