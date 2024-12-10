using System;
using Unity.Mathematics;

public abstract class Noise<O> {
    public abstract AbstractNoiseNode<I, O> CreateAbstractYetToEval<I>();
    public abstract Variable<O> Evaluate<T>(Variable<T> position);
}

public class Simplex : Noise<float> {
    public Variable<float> amplitude;
    public Variable<float> scale;

    public Simplex() {
        amplitude = 1.0f;
        scale = 0.01f;
    }

    public Simplex(float amplitude = 1.0f, float scale = 0.01f) {
        this.amplitude = amplitude;
        this.scale = scale;
    }

    public override AbstractNoiseNode<I, float> CreateAbstractYetToEval<I>() {
        return new SimplexNoiseNode<I>() {
            amplitude = amplitude,
            scale = scale,
            position = null,
        };
    }

    public override Variable<float> Evaluate<T>(Variable<T> position) {
        var type = Utils.TypeOf<T>();
        if (type != Utils.StrictType.Float2 && type != Utils.StrictType.Float3) {
            throw new Exception("Type not supported");
        }

        AbstractNoiseNode<T, float> a = CreateAbstractYetToEval<T>();
        a.position = position;
        return a;
    }
}

public class Voronoi : Noise<float> {
    public Variable<float> amplitude;
    public Variable<float> scale;
    public Type type;

    public enum Type {
        F1,
        F2,
    }

    public Voronoi() {
        this.amplitude = 1.0f;
        this.scale = 0.01f;
        this.type = Type.F1;
    }

    public Voronoi(float amplitude = 1.0f, float scale = 0.01f, Type type = Type.F1) {
        this.amplitude = amplitude;
        this.scale = scale;
        this.type = type;
    }

    public override Variable<float> Evaluate<T>(Variable<T> position) {
        var type2 = Utils.TypeOf<T>();
        if (type2 != Utils.StrictType.Float2 && type2 != Utils.StrictType.Float3) {
            throw new Exception("Type not supported");
        }

        AbstractNoiseNode<T, float> a = CreateAbstractYetToEval<T>();
        a.position = position;
        return a;
    }

    public override AbstractNoiseNode<I, float> CreateAbstractYetToEval<I>() {
        return new VoronoiNode<I>() {
            amplitude = amplitude,
            scale = scale,
            position = null,
            type = type,
        };
    }
}


public class Voronoise : Noise<float> {
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

    public override AbstractNoiseNode<I, float> CreateAbstractYetToEval<I>() {
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

        AbstractNoiseNode<T, float> a = CreateAbstractYetToEval<T>();
        a.position = position;
        return a;
    }
}

public class Warper {
    public Noise<float> noisinator;
    public Variable<float2> warpingScale2;
    public Variable<float2> warpingScale;

    public Warper() {
        this.warpingScale = new float2(1.0f);
        this.warpingScale2 = new float2(1.0f);
    }

    public Variable<float2> Warpinate(Variable<float2> position) {
        return new WarperNode {
            toClone = noisinator.CreateAbstractYetToEval<float2>(),
            warpingScale = warpingScale,
            warpingScale2 = warpingScale2,
            position = position,
        };
    }
}

public class FractalNoise {
    public Noise<float> noise;
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

    public FractalNoise(Noise<float> noise, FractalMode mode, Variable<float> lacunarity, Variable<float> persistence, int octaves) {
        this.noise = noise;
        this.lacunarity = lacunarity;
        this.persistence = persistence;
        this.mode = mode;
        this.octaves = octaves;
    }

    public FractalNoise(Noise<float> noise, FractalMode mode, int octaves) {
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