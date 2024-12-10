using System;
using UnityEngine;

[Serializable]
public class DefineNode : Variable {
    public string value;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable(this.type, "c", $"{value}", true);
    }
}

[Serializable]
public class NoOP : Variable {
    public override string Handle(TreeContext ctx) {
        throw new Exception();
        return "";
    }
}

[Serializable]
public class SimpleBinOpNode : Variable {
    [SerializeReference]
    public Variable a, b;
    public string op;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable(this.type, $"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }

    public override void PreHandle(PreHandle context) {
        context.RegisterDependency(a);
        context.RegisterDependency(b);
    }
}

[Serializable]
public class InjectedNode : Variable {
    public Func<object> calback;

    public override string Handle(TreeContext ctx) {
        return ctx.Inject(this.type, "inj", calback);
    }
}

[Serializable]
public class Inject<T> {
    public T x;
}