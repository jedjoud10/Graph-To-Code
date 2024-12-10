using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;



[Serializable]
public abstract class AbstractNoiseNode<I, O> : Variable<O>, ICloneable {
    [SerializeReference]
    public Variable<float> amplitude;
    [SerializeReference]
    public Variable<float> scale;
    [SerializeReference]
    public Variable<I> position;

    public abstract object Clone();

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(amplitude);
        context.RegisterDependency(scale);
        if (position != null) {
            context.RegisterDependency(position);
        }
    }
}

[Serializable]
public class SimplexNoiseNode<T> : AbstractNoiseNode<T, float> {
    public override object Clone() {
        return new SimplexNoiseNode<T> {
            amplitude = this.amplitude,
            scale = this.scale,
            position = this.position
        };
    }

    public override string Handle(TreeContext context) {
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(snoise({inner})) * {context[amplitude]}";
        return context.DefineVariable(Utils.StrictType.Float, $"{context[position]}_noised", value);
    }
}

[Serializable]
public class VoronoiNode<T> : AbstractNoiseNode<T, float> {
    public override object Clone() {
        return new VoronoiNode<T> {
            amplitude = this.amplitude,
            scale = this.scale,
            position = this.position,
            type = this.type,
        };
    }

    public Voronoi.Type type;

    public override string Handle(TreeContext context) {
        string suffix = "";
        string fn = "";

        switch (type) {
            case Voronoi.Type.F1:
                fn = "cellular";
                suffix = ".x - 0.5";
                break;
            case Voronoi.Type.F2:
                fn = "cellular";
                suffix = ".y - 0.5";
                break;
        }

        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"({fn}({inner}){suffix}) * {context[amplitude]}";
        return context.DefineVariable(Utils.StrictType.Float, $"{context[position]}_noised", value);
    }
}

[Serializable]
public class VoronoiseNode<T> : AbstractNoiseNode<T, float> {
    [SerializeReference]
    public Variable<float> lerpValue;
    [SerializeReference]
    public Variable<float> randomness;

    public override object Clone() {
        return new VoronoiseNode<T> {
            amplitude = this.amplitude,
            scale = this.scale,
            position = this.position,
            lerpValue = this.lerpValue,
            randomness = this.randomness
        };
    }

    public override string Handle(TreeContext context) {
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(voronoise({inner}, {context[randomness]}, {context[lerpValue]})) * {context[amplitude]}";
        return context.DefineVariable(Utils.StrictType.Float, $"{context[position]}_noised", value);
    }

    public override void PreHandle(PreHandle context) {
        base.PreHandle(context);
        context.RegisterDependency(lerpValue);
        context.RegisterDependency(randomness);
    }
}

[Serializable]
public class WarperNode : Variable<float2> {
    [SerializeReference]
    public AbstractNoiseNode<float2, float> toClone;
    [SerializeReference]
    public Variable<float2> warpingScale2;
    [SerializeReference]
    public Variable<float2> warpingScale;
    [SerializeReference]
    public Variable<float2> position;

    public override void PreHandle(PreHandle context) {
        toClone.PreHandle(context);
        context.RegisterDependency(warpingScale2);
        context.RegisterDependency(warpingScale);
        context.RegisterDependency(position);
    }

    public float3 offsets_x = new float3(123.85441f, 32.223543f, -359.48534f);
    public float3 offsets_y = new float3(65.4238f, -551.15353f, 159.5435f);
    public float3 offsets_z = new float3(-43.85454f, -3346.234f, 54.7653f);

    public override string Handle(TreeContext context) {
        Variable<float2> a_offsetted = context.DefineVariableNoOp<float2>($"{context[position]}_x_offset", $"(({context[position]} + float2({offsets_x.x}, {offsets_x.y})) * {context[warpingScale]}.x)");
        Variable<float2> b_offsetted = context.DefineVariableNoOp<float2>($"{context[position]}_y_offset", $"(({context[position]} + float2({offsets_y.x}, {offsets_y.y})) * {context[warpingScale]}.y)");

        var a = (AbstractNoiseNode<float2, float>)toClone.Clone();
        a.position = a_offsetted;
        var b = (AbstractNoiseNode<float2, float>)toClone.Clone();
        b.position = b_offsetted;

        Variable<float> a1 = context.Bind<float>(a.Handle(context));
        Variable<float> b1 = context.Bind<float>(a.Handle(context));


        Variable<float> a2 = context.DefineVariableNoOp<float>($"{context[position]}_warped_x", $"({context[position]}.x + {context[a1]} * {context[warpingScale2]}.x)");
        Variable<float> b2 = context.DefineVariableNoOp<float>($"{context[position]}_warped_y", $"({context[position]}.y + {context[b1]} * {context[warpingScale2]}.y)");
        return context.DefineVariable<float2>($"{context[position]}_warped", $"float2({context[a2]}, {context[b2]})");
    }
}


/*

public class Warper {
    private Noise noise;
    


    public Warper(Noise noise) {
        this.noise = noise;
        this.scale = new float3(1.0f, 1.0f, 1.0f);
    }

    public Var<float2> Warp(Var<float2> position) {
        
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