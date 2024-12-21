using System;
using UnityEditor;
using UnityEngine;

public class FractalNode<T> : Variable<float> {
    public Variable<T> position;
    public Fractal<T>.Fold fold;
    public Fractal<T>.Inner inner;
    public Fractal<T>.PreFoldRemap remap;
    public Variable<float> lacunarity;
    public Variable<float> persistence;
    public int octaves;

    public override void HandleInternal(TreeContext context) {
        lacunarity.Handle(context);
        position.Handle(context);
        persistence.Handle(context);

        int actualOctaves = Mathf.Max(octaves, 0);
        context.Hash(actualOctaves);

        Variable<float> sum = context.AssignTempVariable<float>($"{context[position]}_fbm", "0.0");
        Variable<float> fbm_scale = context.AssignTempVariable<float>($"{context[position]}_fbm_scale", "1.0");
        Variable<float> fbm_amplitude = context.AssignTempVariable<float>($"{context[position]}_fbm_amplitude", "1.0");

        context.AddLine("[unroll]");
        context.AddLine($"for(uint i = 0; i < {actualOctaves}; i++) {{");
        context.Indent++;

        int dimensionality = Utils.TypeOf<T>() == Utils.StrictType.Float2 ? 2 : 3;
        string hashOffset = $"hash{dimensionality}1(float(i) * 6543.26912) * 2366.5437";

        Variable<T> fbmed = context.AssignTempVariable<T>($"{context[position]}_fmb_pos", $"{context[position]} * {context[fbm_scale]} + {hashOffset}");
        Variable<float> innerVar = inner(fbmed * fbm_scale.Broadcast<T>());
        Variable<float> remapped = remap(innerVar);
        Variable<float> foldedVar = fold(sum, remapped * fbm_amplitude);
        foldedVar.Handle(context);
        context.AddLine($"{context[sum]} = {context[foldedVar]};");

        context.AddLine($"{context[fbm_scale]} *= {context[lacunarity]};");
        context.AddLine($"{context[fbm_amplitude]} *= {context[persistence]};");

        context.Indent--;
        context.AddLine("}");

        context.DefineAndBindNode<float>(this, $"{context[position]}_fbm", context[sum]);
    }
}

class CreatePreFoldRemapFromModeNode<T> : Variable<float> {
    public Fractal<T>.FractalMode mode;
    public Variable<float> upperBound;
    public Variable<float> current;

    public override void HandleInternal(TreeContext ctx) {
        ctx.Hash(mode);
        upperBound.Handle(ctx);
        current.Handle(ctx);

        string huh = "";
        switch (mode) {
            case Fractal<T>.FractalMode.Ridged:
                huh = $"2 * abs({ctx[current]}) - abs({ctx[upperBound]})";
                break;
            case Fractal<T>.FractalMode.Billow:
                huh = $"-(2 * abs({ctx[current]}) - abs({ctx[upperBound]}))";
                break;
            case Fractal<T>.FractalMode.Sum:
                huh = $"{ctx[current]}";
                break;
        }

        ctx.DefineAndBindNode<float>(this, "huh", huh);
    }
}

public class Fractal<T> {
    public delegate Variable<float> Inner(Variable<T> position);
    public delegate Variable<float> Fold(Variable<float> last, Variable<float> current);
    public delegate Variable<float> PreFoldRemap(Variable<float> current);

    public Inner inner;
    public Fold fold;
    public PreFoldRemap preFoldRemap;

    public Variable<float> persistence;
    public Variable<float> lacunarity;
    public int octaves;

    public enum FractalMode {
        Ridged,
        Billow,
        Sum,
    }

    public static PreFoldRemap CreatePreFoldRemapFromNoise(Noise noise, FractalMode mode) {
        return (current) => new CreatePreFoldRemapFromModeNode<T>() { current = current, mode = mode, upperBound = noise.CreateAbstractYetToEval<T>().amplitude };
    }

    public Fractal(Noise noise, FractalMode mode, Variable<float> lacunarity, Variable<float> persistence, int octaves) {
        this.lacunarity = lacunarity;
        this.persistence = persistence;
        this.inner = (Variable<T> position) => { return noise.Evaluate(position); };
        this.preFoldRemap = CreatePreFoldRemapFromNoise(noise, mode);
        this.fold = (last, current) => last + current;
        this.octaves = octaves;
    }

    public Fractal(Inner inner, Variable<float> lacunarity, Variable<float> persistence, int octaves) {
        this.lacunarity = lacunarity;
        this.persistence = persistence;
        this.inner = inner;
        this.preFoldRemap = (current) => current;
        this.fold = (last, current) => last + current;
        this.octaves = octaves;
    }

    public Fractal(Noise noise, FractalMode mode, int octaves) {
        this.inner = (Variable<T> position) => { return noise.Evaluate(position); };
        this.lacunarity = 2.0f;
        this.persistence = 0.5f;
        this.octaves = octaves;
        this.preFoldRemap = CreatePreFoldRemapFromNoise(noise, mode);
        this.fold = (last, current) => last + current;
    }

    public Variable<float> Evaluate(Variable<T> position) {
        return new FractalNode<T> {
            fold = fold,
            inner = inner,
            remap = preFoldRemap,
            lacunarity = lacunarity,
            persistence = persistence,
            octaves = octaves,
            position = position
        };
    }
}
