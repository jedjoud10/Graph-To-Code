using System;
using UnityEngine;

public abstract class AbstractNoiseNode<I> : Variable<float>, ICloneable {
    [SerializeReference]
    public Variable<float> amplitude;
    [SerializeReference]
    public Variable<float> scale;
    [SerializeReference]
    public Variable<I> position;

    public abstract object Clone();

    public override void HandleInternal(TreeContext context) {
        amplitude.Handle(context);
        scale.Handle(context);
        position.Handle(context);
    }
}

public abstract class Noise {
    public abstract AbstractNoiseNode<I> CreateAbstractYetToEval<I>();
    public abstract Variable<float> Evaluate<T>(Variable<T> position);

    public static Variable<float> Simplex() {
        return null;
    }
}
