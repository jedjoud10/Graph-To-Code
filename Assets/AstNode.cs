using System;
using UnityEngine;


[Serializable]
public abstract class TreeNode {
    public Utils.StrictType Type { get; set; }

    // Goes over the tree node before flattening the array
    public virtual void PreHandle(PreHandle context) { }

    // Outputs the name of the tree node
    public abstract string Handle(TreeContext context);
}

[Serializable]
public class DefineNode : TreeNode {
    public string value;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable(this.Type, "c", $"{value}", true);
    }
}

[Serializable]
public class SimpleBinOpNode : TreeNode {
    [SerializeReference]
    public TreeNode a, b;
    public string op;

    public override string Handle(TreeContext ctx) {
        this.Type = a.Type;
        return ctx.DefineVariable(a.Type, $"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(a);
        context.RegisterDependency(b);
    }
}

[Serializable]
public abstract class AbstractNoiseNode : TreeNode {
    public TreeNode amplitude;
    public TreeNode scale;
    public TreeNode position;
}

[Serializable]
public class SimplexNoiseNode : AbstractNoiseNode {
    public override string Handle(TreeContext context) {
        this.Type = Utils.StrictType.Float;
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(snoise({inner})) * {context[amplitude]}";
        return context.DefineVariable(Utils.StrictType.Float, $"{context[position]}_noised", value);
    }

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(amplitude);
        context.RegisterDependency(scale);
        context.RegisterDependency(position);
    }
}

public class Noise2 {
    public Variable<float> amplitude;
    public Variable<float> scale;

    public Variable<float> Evaluate<T>(Variable<T> position) {
        return new Variable<float> {
            symbol = new SimplexNoiseNode { position = position.symbol, amplitude = amplitude.symbol, scale = scale.symbol }
        };
    }
}

/*
public class SimplexNoiseNode : AbstractNoiseNode {
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
*/

/*
    public Variable<float> insanity;
    public Variable<float> scale;

    public Variable<float> Evaluate<T>(Variable<T> position) {
        return new Variable<float> {
            symbol = new Noise2 {
                position = position.symbol,
                insanity = insanity.symbol,
                scale = scale.symbol,
            },
        };
    }
*/

public static class NoiseUtils {
    public static Variable<float> Noise<T>(Variable<T> position, Variable<float> insanity, Variable<float> scale) {
        return null;
    }
}

[Serializable]
public class InjectedNode : TreeNode {
    public Func<object> calback;

    public override string Handle(TreeContext ctx) {
        return ctx.Inject(this.Type, "inj", calback);
    }
}

[Serializable]
public class Inject<T> {
    public T x;
}
