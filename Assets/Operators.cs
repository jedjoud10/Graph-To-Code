using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class DefineNode<T> : Variable<T> {
    [SerializeField]
    public string value;
    public bool constant;

    public override void HandleInternal(TreeContext ctx) {
        ctx.DefineAndBindNode(this, Utils.TypeOf<T>(), "c", value, constant);
    }
}

[Serializable]
public class SimpleBinOpNode<T> : Variable<T> {
    [SerializeReference]
    public Variable<T> a;
    [SerializeReference]
    public Variable<T> b;
    [SerializeField]
    public string op;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, $"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }
}

[Serializable]
public class CastNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> a;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<O>(this, $"{ctx[a]}_casted", $"{ctx[a]}");
    }
}

[Serializable]
public class SwizzleNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> a;
    [SerializeField]
    public string swizzle;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<O>(this, $"{ctx[a]}_swizzled", $"{ctx[a]}.{swizzle}");
    }
}

[Serializable]
public class ConstructNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> x;
    [SerializeReference]
    public Variable<I> y;
    [SerializeReference]
    public Variable<I> z;
    [SerializeReference]
    public Variable<I> w;



    public override void HandleInternal(TreeContext ctx) {
        string C(Variable<I> variable) {
            if (variable == null) {
                return "0.0";
            } else {
                return ctx[variable];
            }
        }

        x?.Handle(ctx);
        y?.Handle(ctx);
        z?.Handle(ctx);
        w?.Handle(ctx);

        switch (Utils.TypeOf<O>()) {
            case Utils.StrictType.Float2:
                ctx.DefineAndBindNode<O>(this, $"f2_ctor", $"float2({C(x)},{C(y)})");
                break;
            case Utils.StrictType.Float3:
                ctx.DefineAndBindNode<O>(this, $"f3_ctor", $"float3({C(x)},{C(y)},{C(z)})");
                break;
            case Utils.StrictType.Float4:
                ctx.DefineAndBindNode<O>(this, $"f4_ctor", $"float4({C(x)},{C(y)},{C(z)},{C(w)})");
                break;
            default:
                throw new Exception();
                break;
        }
    }
}

[Serializable]
public class InjectedNode<T> : Variable<T> {
    public Inject<T> a;
    public override void HandleInternal(TreeContext ctx) {
        ctx.Inject<T>(this, "inj", () => a.x);
    }
}

public class FiniteDiffer<I, O> {
    public Variable<O> FiniteDiffThatThang(Variable<I> input, float4 epsilon) {
        return null;
    }
}

// Create a function with this an input and output as function in/out, where the function is called 3 or 4 times (depending on dimensionality)
// Small changes in the input variable along those axis (with specified epsilon) and calculating the final changes in the output variable
public class FiniteDifferenciatedNode<I, O> : Variable<O> {
    public Variable<I> input;
    public Variable<O> output;
    public float4 diff;

    public override void HandleInternal(TreeContext context) {
        throw new NotImplementedException();
    }
}

[Serializable]
public class Inject<T> {
    public T x;

    public Inject(T a) {
        this.x = a;
    }
}