public static class SdfOps {
    public static Variable<float> Union(Variable<float> a, Variable<float> b, Variable<float> smoothing = null) {
        return new OpSdfOp { a = a, b = b, smooth = smoothing, op = "Union" };
    }

    public static Variable<float> Intersection(Variable<float> a, Variable<float> b, Variable<float> smoothing = null) {
        return new OpSdfOp { a = a, b = b, smooth = smoothing, op = "Intersection" };
    }

    public static Variable<float> Subtraction(Variable<float> a, Variable<float> b, Variable<float> smoothing = null) {
        return new OpSdfOp { a = a, b = b, smooth = smoothing, op = "Subtraction" };
    }
}

public class OpSdfOp : Variable<float> {
    public Variable<float> a;
    public Variable<float> b;
    public Variable<float> smooth = null;
    public string op;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);

        if (smooth != null) {
            smooth.Handle(ctx);
            ctx.DefineAndBindNode<float>(this, $"{ctx[a]}_smooth_{op}_{ctx[b]}", $"opSmooth{op}({ctx[a]}, {ctx[b]}, {ctx[smooth]})");
        } else {
            ctx.DefineAndBindNode<float>(this, $"{ctx[a]}_{op}_{ctx[b]}", $"op{op}({ctx[a]}, {ctx[b]})");
        }
    }
}