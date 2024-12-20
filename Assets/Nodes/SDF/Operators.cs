using static SdfOps;

public static class SdfOps {
    public static OpSdfBuilder Union(params Variable<float>[] vars) {
        return new OpSdfBuilder { variables = vars, op = "Union" };
    }

    public static OpSdfBuilder Intersection(params Variable<float>[] vars) {
        return new OpSdfBuilder { variables = vars, op = "Intersection" };
    }

    public static OpSdfBuilder Subtraction(params Variable<float>[] vars) {
        return new OpSdfBuilder { variables = vars, op = "Subtraction" };
    }

    public static Variable<float> Distance<T>(Variable<T> a, Variable<T> b, DistanceFunction mode = DistanceFunction.Euclidean) {
        //return abs(a.x - b.x) + abs(a.y - b.y);
        //return max(abs(a.x - b.x), abs(a.y - b.y));
        //return distance(a, b);
        return new DistanceOp<T>() { a = a, b = b, mode = mode };
    }

    public enum DistanceFunction {
        Euclidean,
        Manhattan,
        ManhattanMaxxed,
    }
}

public class OpSdfBuilder {
    public Variable<float>[] variables;
    public string op;
    private Variable<float> smooth = null;

    public static implicit operator OpSdfOp(OpSdfBuilder value) {
        return new OpSdfOp {
            op = value.op,
            smooth = value.smooth,
            variables = value.variables
        };
    }

    public OpSdfBuilder Smoothenation(Variable<float> smooth) {
        this.smooth = smooth;
        return this;
    }
}

public class DistanceOp<T> : Variable<float> {
    public Variable<T> a;
    public Variable<T> b;
    public DistanceFunction mode;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);

        string func = "";

        switch (mode) {
            case DistanceFunction.Euclidean:
                func = $"distance({ctx[a]}, {ctx[b]})";
                break;
            case DistanceFunction.Manhattan:
                func = $"dot(abs({ctx[a]} - {ctx[b]}), 1.0)";
                break;
            case DistanceFunction.ManhattanMaxxed:
                //Variable<T> temp = ctx.AssignTempVariable<T>("distance_maxx_bruh", "abs({ctx[a]} - {ctx[b]})");
                //func = $"max({ctx[temp]}.x, {ctx[temp]}.y, {ctx[temp]}.z)";
                throw new System.Exception();
                break;
            default:
                throw new System.Exception();
        }

        ctx.DefineAndBindNode<float>(this, $"distance_maxx", func);
    }
}

public class OpSdfOp : Variable<float> {
    public Variable<float>[] variables;
    public Variable<float> smooth = null;
    public string op;

    public override void HandleInternal(TreeContext ctx) {
        foreach (var v in variables) {
            v.Handle(ctx);
        }

        if (smooth != null) {
            smooth.Handle(ctx);

            Variable<float> temp =  variables[0];
            for (var i = 1; i < variables.Length; i++) {
                temp = ctx.AssignTempVariable<float>("sdf_smooth_temp", $"opSmooth{op}({ctx[temp]}, {ctx[variables[i]]}, {ctx[smooth]})");
            }

            ctx.DefineAndBindNode<float>(this, $"sdf_smooth_{op.ToLower()}", $"{ctx[temp]}");
        } else {
            Variable<float> temp = variables[0];
            for (var i = 1; i < variables.Length; i++) {
                temp = ctx.AssignTempVariable<float>("sdf_temp", $"op{op}({ctx[temp]}, {ctx[variables[i]]})");
            }

            ctx.DefineAndBindNode<float>(this, $"sdf_{op.ToLower()}", $"{ctx[temp]}");
        }
    }
}