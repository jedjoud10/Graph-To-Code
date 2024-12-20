using System;


public class RandomNode<I, O> : Variable<O> {
    public Variable<I> input;

    public override void HandleInternal(TreeContext ctx) {
        input.Handle(ctx);
        int inputDims = Utils.Dimensionality<I>();
        int outputDims = Utils.Dimensionality<O>();
        ctx.DefineAndBindNode<O>(this, "rng", $"hash{outputDims}{inputDims}({ctx[input]})");
    }
}

public class Hasher {
    public static Variable<O> Evaluate<I, O>(Variable<I> input) {
        return new RandomNode<I, O> {
            input = input,
        };
    }
}
