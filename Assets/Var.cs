using System;
using Unity.Mathematics;


[Serializable]
public abstract class TreeNode {
    // Goes over the tree node before flattening the array
    public virtual void PreHandle(PreHandle context) { }

    // Outputs the name of the tree node
    public abstract string Handle(TreeContext context);
}

[Serializable]
public abstract class Variable<T> : TreeNode {

    public static implicit operator Variable<T>(T value) {
        return new DefineNode<T> { value = Utils.ToDefinableString(value), constant = true };
    }

    public static Variable<T> operator +(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "+" };
    }

    public static Variable<T> operator -(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "-" };
    }

    public static Variable<T> operator *(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "*" };
    }
    public static Variable<T> operator /(Variable<T> a, Variable<T> b) {
        return new SimpleBinOpNode<T> { a = a, b = b, op = "/" };
    }

    public static implicit operator Variable<T>(Inject<T> value) {
        return new InjectedNode<T> { a = value };
    }

    public Variable<U> Swizzle<U>(string swizzle) {
        return new SwizzleNode<T, U> { a = this, swizzleOp = swizzle };
    } 
}

/*
public class Var<T> {

    // TODO: Keep track of the inputs used for this variable so that we can use a 2d texture instead of a 3d one each time
    // TODO: Must create a different compute shader with required variables
    // TODO: Must create texture and create a variable that reads from it in the OG shader
    // sub-TODO: can squish multiple cached calls into a single RGBA texture (of the same size) to help performance
    public CachedVar<T> Cached(int sizeReduction = 1) {
        return null;
    }
}

public class CachedVar<T> {
    public Var<T> var;

    // Central difference gradient approximation
    public Var<float3> ApproxGradent() {
        return float3.zero;
    }
}
*/