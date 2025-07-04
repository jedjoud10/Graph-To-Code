using System;

public class VoronoiseNode<T> : AbstractNoiseNode<T> {
    public Variable<float> lerpValue;
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

    public override void HandleInternal(TreeContext context) {
        lerpValue.Handle(context);
        randomness.Handle(context);
        base.HandleInternal(context);
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(voronoise({inner}, {context[randomness]}, {context[lerpValue]})) * {context[amplitude]}";
        context.DefineAndBindNode<float>(this, $"{context[position]}_noised", value);
    }
}
public class Voronoise : Noise {
    public Variable<float> amplitude;
    public Variable<float> scale;
    public Variable<float> lerpValue;
    public Variable<float> randomness;

    public Voronoise() {
        this.amplitude = 1.0f;
        this.scale = 0.01f;
        this.lerpValue = 0.5f;
        this.randomness = 0.5f;
    }

    public Voronoise(float amplitude = 1.0f, float scale = 0.01f, float lerpValue = 0.5f, float randomness = 0.5f) {
        this.amplitude = amplitude;
        this.scale = scale;
        this.lerpValue = lerpValue;
        this.randomness = randomness;
    }

    public override AbstractNoiseNode<I> CreateAbstractYetToEval<I>() {
        return new VoronoiseNode<I>() {
            amplitude = amplitude,
            scale = scale,
            position = null,
            randomness = randomness,
            lerpValue = lerpValue,
        };
    }

    public override Variable<float> Evaluate<T>(Variable<T> position) {
        var type = Utils.TypeOf<T>();
        if (type != Utils.StrictType.Float2) {
            throw new Exception("Type not supported");
        }

        AbstractNoiseNode<T> a = CreateAbstractYetToEval<T>();
        a.position = position;
        return a;
    }
}