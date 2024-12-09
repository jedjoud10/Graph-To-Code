using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public abstract class TreeNode {
    // Goes over the tree node before flattening the array
    public virtual void PreHandle(PreHandle context) { }

    // Outputs the name of the tree node
    public abstract string Handle(TreeContext context);
}

[Serializable]
public abstract class Variable<T> : TreeNode {

    public static implicit operator Variable<T>(T value) {
        return new DefineNode<T> { value = value.ToString() };
    }

    public static Variable<T> operator +(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "+" };
    }

    public static Variable<T> operator -(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "-" };
    }

    public static Variable<T> operator *(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "*" };
    }
    public static Variable<T> operator /(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "/" };
    }

    public static implicit operator Variable<T>(Inject<T> value) {
        return new InjectedNode<T> { calback = () => value.x };
    }
}

[Serializable]
public class DefineNode<T> : Variable<T> {
    public string value;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable(Utils.TypeOf<T>(), "c", $"{value}", true);
    }
}

[Serializable]
public class SimpleBinOpNode<T> : Variable<T> {
    [SerializeReference]
    public Variable<T> a, b;
    public string op;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable(Utils.TypeOf<T>(), $"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(a);
        context.RegisterDependency(b);
    }
}

[Serializable]
public abstract class AbstractNoiseNode<T> : Variable<float> {
    public Variable<float> amplitude;
    public Variable<float> scale;
    public Variable<T> position;

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(amplitude);
        context.RegisterDependency(scale);
        context.RegisterDependency(position);
    }
}

[Serializable]
public class SimplexNoiseNode<T> : AbstractNoiseNode<T> {
    public SimplexNoiseNode() {
        var type = Utils.TypeOf<T>();
        if (type != Utils.StrictType.Float2 && type != Utils.StrictType.Float3) {
            throw new Exception("Type not supported");
        }
    }

    public override string Handle(TreeContext context) {
        string inner = $"({context[position]}) * {context[scale]}";
        string value = $"(snoise({inner})) * {context[amplitude]}";
        return context.DefineVariable(Utils.StrictType.Float, $"{context[position]}_noised", value);
    }
}

[Serializable]
public class VoronoiNode<T> : AbstractNoiseNode<T> {

    public VoronoiNode() {
        var type = Utils.TypeOf<T>();
        if (type != Utils.StrictType.Float2 && type != Utils.StrictType.Float3) {
            throw new Exception("Type not supported");
        }
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
public class VoronoiseNode<T> : AbstractNoiseNode<T> {
    public VoronoiseNode() {
        var type = Utils.TypeOf<T>();
        if (type != Utils.StrictType.Float2) {
            throw new Exception("Type not supported");
        }
    }

    public Variable<float> lerpValue;
    public Variable<float> randomness;

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

public class Simplex {
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

    public Variable<float> Evaluate<T>(Variable<T> position) {
        return new SimplexNoiseNode<T>() {
            amplitude = amplitude,
            scale = scale,
            position = position,
        };
    }
}

public class Voronoi {
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

    public Variable<float> Evaluate<T>(Variable<T> position) {
        return new VoronoiNode<T>() {
            amplitude = amplitude,
            scale = scale,
            position = position,
            type = type,
        };
    }
}

[Serializable]
public class InjectedNode<T> : Variable<T> {
    public Func<object> calback;
    public Utils.StrictType type;

    public override string Handle(TreeContext ctx) {
        return ctx.Inject(type, "inj", calback);
    }
}

[Serializable]
public class Inject<T> {
    public T x;
}