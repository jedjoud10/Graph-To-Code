using System;
using UnityEngine;

[Serializable]
public class DefineNode<T> : Variable<T> {
    [SerializeField]
    public string value;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable<T>("c", $"{value}", true);
    }
}

[Serializable]
public class NoOP<T> : Variable<T> {
    public override string Handle(TreeContext ctx) {
        throw new Exception();
        return "";
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

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable<T>($"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(a);
        context.RegisterDependency(b);
    }
}

[Serializable]
public class SwizzleNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> a;
    [SerializeField]
    public string swizzleOp;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable<O>($"{ctx[a]}_swizzle", $"{ctx[a]}.{swizzleOp}");
    }

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(a);
    }
}

[Serializable]
public class InjectedNode<T> : Variable<T> {
    public Func<object> calback;

    public override string Handle(TreeContext ctx) {
        return ctx.Inject(Utils.TypeOf<T>(), "inj", calback);
    }
}

[Serializable]
public class Inject<T> {
    public T x;
}