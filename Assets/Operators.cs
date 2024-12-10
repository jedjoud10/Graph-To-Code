using System;
using UnityEngine;

[Serializable]
public class DefineNode<T> : Variable<T> {
    public string value;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable(Utils.TypeOf<T>(), "c", $"{value}", true);
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