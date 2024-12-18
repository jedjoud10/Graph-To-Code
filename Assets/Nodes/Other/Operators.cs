using System;

public class DefineNode<T> : Variable<T> {
    public string value;
    public bool constant;

    public override void HandleInternal(TreeContext ctx) {
        ctx.Hash(value);
        ctx.DefineAndBindNode(this, Utils.TypeOf<T>(), "c", value, constant);
    }
}

public class NoOp<T> : Variable<T> {
    public override void HandleInternal(TreeContext context) {
    }
}

public class SimpleBinOpNode<T> : Variable<T> {
    public Variable<T> a;
    public Variable<T> b;
    public string op;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, $"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }
}

public class SimpleBinFuncNode<T> : Variable<T> {
    public Variable<T> a;
    public Variable<T> b;
    public string func;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, $"{ctx[a]}_func_{ctx[b]}", $"{func}({ctx[a]},{ctx[b]})");
    }
}

public class NegateNode<T> : Variable<T> {
    public Variable<T> a;
    
    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, $"{ctx[a]}_negated", $"(-{ctx[a]})");
    }
}

public class LerpNode<T> : Variable<T> {
    public Variable<T> a;
    public Variable<T> b;
    public Variable<T> t;
    public bool clamp;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);
        t.Handle(ctx);
        ctx.Hash(clamp);

        string mixer = clamp ? $"clamp({ctx[t]}, 0.0, 1.0)" : ctx[t];

        ctx.DefineAndBindNode<T>(this, $"{ctx[a]}_lerp_{ctx[b]}", $"lerp({ctx[a]},{ctx[b]},{mixer})");
    }
}

public class CastNode<I, O> : Variable<O> {
    public Variable<I> a;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<O>(this, $"{ctx[a]}_casted", $"{ctx[a]}");
    }
}


public class SwizzleNode<I, O> : Variable<O> {
    public Variable<I> a;
    public string swizzle;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<O>(this, $"{ctx[a]}_swizzled", $"{ctx[a]}.{swizzle}");
    }
}

public class ConstructNode<I, O> : Variable<O> {
    public Variable<I> x;
    public Variable<I> y;
    public Variable<I> z;
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