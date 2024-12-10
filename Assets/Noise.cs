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

    public override void Handle(TreeContext context) {
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(snoise({inner})) * {context[amplitude]}";
        context.DefineAndBindNode<float>(this, $"{context[position]}_noised", value);
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

    public override void PreHandle(PreHandle context) {
        base.PreHandle(context);
        context.Hash(type);
    }

    public override void Handle(TreeContext context) {
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
        context.DefineAndBindNode<float>(this, $"{context[position]}_noised", value);
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

    public override void Handle(TreeContext context) {
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(voronoise({inner}, {context[randomness]}, {context[lerpValue]})) * {context[amplitude]}";
        context.DefineAndBindNode<float>(this, $"{context[position]}_noised", value);
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

    public override void Handle(TreeContext context) {
        Variable<float2> a_offsetted = context.AssignTempVariable<float2>($"{context[position]}_x_offset", $"(({context[position]} + float2({offsets_x.x}, {offsets_x.y})) * {context[warpingScale]}.x)");
        Variable<float2> b_offsetted = context.AssignTempVariable<float2>($"{context[position]}_y_offset", $"(({context[position]} + float2({offsets_y.x}, {offsets_y.y})) * {context[warpingScale]}.y)");

        var a = (AbstractNoiseNode<float2, float>)toClone.Clone();
        a.position = a_offsetted;
        var b = (AbstractNoiseNode<float2, float>)toClone.Clone();
        b.position = b_offsetted;

        a.Handle(context);
        b.Handle(context);


        Variable<float> a2 = context.AssignTempVariable<float>($"{context[position]}_warped_x", $"({context[position]}.x + {context[a]} * {context[warpingScale2]}.x)");
        Variable<float> b2 = context.AssignTempVariable<float>($"{context[position]}_warped_y", $"({context[position]}.y + {context[b]} * {context[warpingScale2]}.y)");
        context.DefineAndBindNode<float2>(this, $"{context[position]}_warped", $"float2({context[a2]}, {context[b2]})");
    }
}


[Serializable]
public class FractalNoiseNode<T> : AbstractNoiseNode<T, float> {
    public FractalNoise.FractalMode mode;
    public Variable<float> lacunarity;
    public Variable<float> persistence;
    public int octaves;

    [SerializeReference]
    public AbstractNoiseNode<T, float> noise;

    public override object Clone() {
        return new FractalNoiseNode<T> {
            noise = this.noise,
            lacunarity = this.lacunarity,
            persistence = this.persistence,
            octaves = this.octaves,
            mode = this.mode,
        };
    }

    public override void Handle(TreeContext context) {
        Variable<float> sum = context.AssignTempVariable<float>($"{context[position]}_fbm", mode == FractalNoise.FractalMode.Mul ? "1.0" : "0.0");
        Variable<float> fbm_scale = context.AssignTempVariable<float>($"{context[position]}_fbm_scale", "1.0");
        Variable<float> fbm_amplitude = context.AssignTempVariable<float>($"{context[position]}_fbm_amplitude", "1.0");


        context.Lines.Add("[unroll]");
        context.Lines.Add($"for(uint i = 0; i < {octaves}; i++) {{");


        Variable<T> fbmed = context.AssignTempVariable<T>($"{context[position]}_fmb_pos", $"{context[position]} * {context[fbm_scale]} + hash31(float(i))");

        var new_noise = (AbstractNoiseNode<T, float>)noise.Clone();
        new_noise.position = fbmed;
        new_noise.Handle(context);


        switch (mode) {
            case FractalNoise.FractalMode.Billow:
                context.Lines.Add($"{context[sum]} += ({context[noise.amplitude]} - abs({context[new_noise]})) * {context[fbm_amplitude]};");
                break;
            case FractalNoise.FractalMode.Ridged:
                context.Lines.Add($"{context[sum]} += abs({context[new_noise]}) * {context[fbm_amplitude]};");
                break;
            case FractalNoise.FractalMode.Sum:
                context.Lines.Add($"{context[sum]} += {context[new_noise]} * {context[fbm_amplitude]};");
                break;
            case FractalNoise.FractalMode.Mul:
                context.Lines.Add($"{context[sum]} *= {context[new_noise]} * {context[fbm_amplitude]};");
                break;
        }

        context.Lines.Add("}");

        context.DefineAndBindNode<float>(this, $"{context[position]}_fbm", context[sum]);
    }

    public override void PreHandle(PreHandle context) {
        base.PreHandle(context);
        noise.PreHandle(context);
        context.RegisterDependency(lacunarity);
        context.RegisterDependency(persistence);
        context.Hash(mode);
        context.Hash(octaves);
    }
}