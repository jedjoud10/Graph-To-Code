using Unity.Mathematics;

/*
public abstract class Noise {
    public Noise(Var<float> intensity, Var<float> scale) {
        this.amplitude = intensity;
        this.scale = scale;
    }

    public Noise() {
        this.amplitude = 1.0f;
        this.scale = 1.0f;
    }

    public Var<float> amplitude;
    public Var<float> scale;

    public abstract string Internal(string name);

    public abstract bool Supports3D { get; }

    // Evaluate the noise at the specific point
    public virtual Var<float> Evaluate<T>(Var<T> position) {
        if (position.Dimensionality == 3 && !Supports3D) {
            throw new System.Exception("Noise type does not support 3D");
        }

        return Var<float>.CreateFromName(position.name + "_noised", Internal(position.name));
    }
}

public class Simplex : Noise {
    public override bool Supports3D => true;

    public override string Internal(string name) {
        string inner = $"({name}) * {scale.name}";
        return $"(snoise({inner})) * {amplitude.name}";
    }
}

public class Voronoi : Noise {
    public override bool Supports3D => type != Type.Voronoise;

    public enum Type {
        F1,
        F2,
        Voronoise,
    }

    public Type type;
    public Var<float> voronoiseLerpValue;
    public Var<float> voronoiseRandomness;

    public Voronoi(Type type, Var<float> voronoiseLerpValue, Var<float> voronoiseRandomness) : base() {
        this.type = type;
        this.voronoiseLerpValue = voronoiseLerpValue;
        this.voronoiseRandomness = voronoiseRandomness;
    }

    public Voronoi() : base() {
        this.type = Type.F1;
        this.voronoiseLerpValue = 0.5f;
        this.voronoiseRandomness = 0.5f;
    }

    public override string Internal(string name) {
        ShaderManager.singleton.HashenateMaxx(type);
        string inner = $"({name}) * {scale.name}";
        string suffix = "";
        string fn = "";
        string extra = "";

        switch (type) {
            case Type.F1:
                fn = "cellular";
                suffix = ".x - 0.5";
                break;
            case Type.F2:
                fn = "cellular";
                suffix = ".y - 0.5";
                break;
            case Type.Voronoise:
                fn = "voronoise";
                extra = $", {voronoiseRandomness.name}, {voronoiseLerpValue.name}";
                break;
        }
        return $"({fn}({inner}{extra}){suffix}) * {amplitude.name}";
    }
}

public class Warper {
    private Noise noise;
    public Var<float3> scale;
    public float3 offsets_x = new float3(123.85441f, 32.223543f, -359.48534f);
    public float3 offsets_y = new float3(65.4238f, -551.15353f, 159.5435f);
    public float3 offsets_z = new float3(-43.85454f, -3346.234f, 54.7653f);

    public Warper(Noise noise) {
        this.noise = noise;
        this.scale = new float3(1.0f, 1.0f, 1.0f);
    }

    public Var<float2> Warp(Var<float2> position) {
        Var<float2> a_offseted = Var<float2>.CreateFromName(position.name + "_x_offset", $"({position.name} + float2({offsets_x.x}, {offsets_x.y}))");
        Var<float2> b_offseted = Var<float2>.CreateFromName(position.name + "_y_offset", $"({position.name} + float2({offsets_y.x}, {offsets_y.y}))");

        Var<float> a = Var<float>.CreateFromName(position.name + "_warped_x", $"({position.name}.x + {noise.Evaluate(a_offseted).name} * {scale.name}.x)");
        Var<float> b = Var<float>.CreateFromName(position.name + "_warped_y", $"({position.name}.y + {noise.Evaluate(b_offseted).name} * {scale.name}.y)");

        Var<float2> reconstructed = Var<float2>.CreateFromName(position.name + "_warped", $"float2({a.name}, {b.name})");
        return reconstructed;
    }
}



// Fractal noise is a type of noise that implement fBm (either Ridged, Billow, or Sum mode)
public class FractalNoise : Noise {
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

    public override bool Supports3D => noise.Supports3D;

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

    public override string Internal(string name) {
        ShaderManager.singleton.HashenateMaxx(mode);
        ShaderManager.singleton.HashenateMaxx(octaves);

        Var<float> sum = Var<float>.CreateFromName(name + "_noised_fbm", mode == FractalMode.Mul ? "1.0" : "0.0");
        Var<float> _s = Var<float>.CreateFromName(name + "_noised_fbm_scale", "1.0");
        Var<float> _a = Var<float>.CreateFromName(name + "_noised_fbm_amplitude", "1.0");

        ShaderManager.singleton.AddLine("[unroll]");
        ShaderManager.singleton.AddLine($"for(uint i = 0; i < {octaves}; i++) {{");
        ShaderManager.singleton.BeginIndentScope();
        string test = $"{name} * {_s.name} + hash31(float(i))";

        switch (mode) {
            case FractalMode.Billow:
                ShaderManager.singleton.AddLine($"{sum.name} += ({noise.amplitude.name} - abs({noise.Internal(test)})) * {_a.name};");
                break;
            case FractalMode.Ridged:
                ShaderManager.singleton.AddLine($"{sum.name} += abs({noise.Internal(test)}) * {_a.name};");
                break;
            case FractalMode.Sum:
                ShaderManager.singleton.AddLine($"{sum.name} += {noise.Internal(test)} * {_a.name};");
                break;
            case FractalMode.Mul:
                ShaderManager.singleton.AddLine($"{sum.name} *= {noise.Internal(test)} * {_a.name};");
                break;
        }

        ShaderManager.singleton.AddLine($"{_s.name} *= {lacunarity.name};");
        ShaderManager.singleton.AddLine($"{_a.name} *= {persistence.name};");
        ShaderManager.singleton.EndIndentScope();
        ShaderManager.singleton.AddLine("}");

        return sum.name;
    }
}
*/