using System;
using UnityEngine;

public class FractalNoiseNode<T> : AbstractNoiseNode<T> {
    public FractalNoise.FractalMode mode;
    public Variable<float> lacunarity;
    public Variable<float> persistence;
    public int octaves;

    public AbstractNoiseNode<T> noise;

    public override object Clone() {
        return new FractalNoiseNode<T> {
            noise = this.noise,
            lacunarity = this.lacunarity,
            persistence = this.persistence,
            octaves = this.octaves,
            mode = this.mode,
        };
    }

    public override void HandleInternal(TreeContext context) {
        lacunarity.Handle(context);
        position.Handle(context);
        persistence.Handle(context);
        context.Hash(mode);

        int actualOctaves = Mathf.Max(octaves, 0);
        context.Hash(actualOctaves);

        Variable<float> sum = context.AssignTempVariable<float>($"{context[position]}_fbm", mode == FractalNoise.FractalMode.Mul ? "1.0" : "0.0");
        Variable<float> fbm_scale = context.AssignTempVariable<float>($"{context[position]}_fbm_scale", "1.0");
        Variable<float> fbm_amplitude = context.AssignTempVariable<float>($"{context[position]}_fbm_amplitude", "1.0");

        context.AddLine("[unroll]");
        context.AddLine($"for(uint i = 0; i < {actualOctaves}; i++) {{");
        context.Indent++;

        Variable<T> fbmed = context.AssignTempVariable<T>($"{context[position]}_fmb_pos", $"{context[position]} * {context[fbm_scale]} + hash31(float(i)) * 1000.0");

        var new_noise = (AbstractNoiseNode<T>)noise.Clone();
        new_noise.position = fbmed;
        new_noise.Handle(context);


        switch (mode) {
            case FractalNoise.FractalMode.Billow:
                context.AddLine($"{context[sum]} += ({context[noise.amplitude]} - abs({context[new_noise]})) * {context[fbm_amplitude]};");
                break;
            case FractalNoise.FractalMode.Ridged:
                context.AddLine($"{context[sum]} += abs({context[new_noise]}) * {context[fbm_amplitude]};");
                break;
            case FractalNoise.FractalMode.Sum:
                context.AddLine($"{context[sum]} += {context[new_noise]} * {context[fbm_amplitude]};");
                break;
            case FractalNoise.FractalMode.Mul:
                context.AddLine($"{context[sum]} *= {context[new_noise]} * {context[fbm_amplitude]};");
                break;
        }

        context.AddLine($"{context[fbm_scale]} *= {context[lacunarity]};");
        context.AddLine($"{context[fbm_amplitude]} *= {context[persistence]};");

        context.Indent--;
        context.AddLine("}");

        context.DefineAndBindNode<float>(this, $"{context[position]}_fbm", context[sum]);
    }
}

public class FractalNoise {
    public Noise noise;
    public Variable<float> persistence;
    public Variable<float> lacunarity;
    public int octaves;
    public FractalMode mode;

    public enum FractalMode {
        Ridged,
        Billow,
        Sum,
        Mul,
    }

    public FractalNoise(Noise noise, FractalMode mode, Variable<float> lacunarity, Variable<float> persistence, int octaves) {
        this.noise = noise;
        this.lacunarity = lacunarity;
        this.persistence = persistence;
        this.mode = mode;
        this.octaves = octaves;
    }

    public FractalNoise(Noise noise, FractalMode mode, int octaves) {
        this.noise = noise;
        this.lacunarity = 2.0f;
        this.persistence = 0.5f;
        this.octaves = octaves;
        this.mode = mode;
    }

    public Variable<float> Evaluate<T>(Variable<T> position) {
        var type = Utils.TypeOf<T>();
        if (type != Utils.StrictType.Float2 && type != Utils.StrictType.Float3) {
            throw new Exception("Type not supported");
        }

        return new FractalNoiseNode<T> {
            amplitude = 1.0f,
            scale = 1.0f,
            lacunarity = lacunarity,
            persistence = persistence,
            octaves = octaves,
            mode = mode,
            noise = noise.CreateAbstractYetToEval<T>(),
            position = position
        };
    }
}
