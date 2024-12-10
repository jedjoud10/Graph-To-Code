using System;
using UnityEngine;

[Serializable]
public class DefineNode<T> : Variable<T> {
    [SerializeField]
    public string value;
    public bool constant;

    public override string Handle(TreeContext ctx) {
        return ctx.DefineVariable<T>("c", $"{value}", constant);
    }

    /*
    public static DefineNode<T> Define(T val, bool constant = true) {
        return new DefineNode<T> {
            value = Utils.ToDefinableString(val),
            constant = constant
        };
    }
    */
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
    public Inject<T> a;
    public override string Handle(TreeContext ctx) {
        return ctx.Inject(Utils.TypeOf<T>(), "inj", () => a.x);
    }
}

[Serializable]
public class Cached<T> : Variable<T> {
    public Variable<T> a;


    // looks up all the dependencies of a and makes sure that they are 2D (could be xy, yx, xz, whatever)
    // clones those dependencies to a secondary compute kernel
    // create temporary texture that is written to by that kernel
    // read said texture with appropriate swizzles in the main kernel

    public override string Handle(TreeContext context) {
        return "";
        //throw new NotImplementedException();
    }

    public override void PreHandle(PreHandle context) {
        //context.RegisterDependency(a);
    }
}

[Serializable]
public class Inject<T> {
    public T x;
}