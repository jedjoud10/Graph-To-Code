public class RemapNode<T> : Variable<T> {
    public Variable<T> inputMin;
    public Variable<T> inputMax;
    public Variable<T> outputMin;
    public Variable<T> outputMax;
    public Variable<T> mixer;

    public override void HandleInternal(TreeContext context) {
        mixer.Handle(context);
        inputMin.Handle(context);
        inputMax.Handle(context);
        outputMin.Handle(context);
        outputMax.Handle(context);

        context.DefineAndBindNode<T>(this, $"{context[mixer]}_remapped", $"Remap({context[mixer]}, {context[inputMin]}, {context[inputMax]}, {context[outputMin]}, {context[outputMax]})");
    }
}

public class Remapper<T> {
    public Variable<T> inputMin = Utils.Zero<T>();
    public Variable<T> inputMax = Utils.One<T>();
    public Variable<T> outputMin = Utils.Zero<T>();
    public Variable<T> outputMax = Utils.One<T>();

    public Variable<T> Remap(Variable<T> mixer) {
        return new RemapNode<T> {
            mixer = mixer,
            inputMin = inputMin,
            inputMax = inputMax,
            outputMin = outputMin,
            outputMax = outputMax,
        };
    }
}

