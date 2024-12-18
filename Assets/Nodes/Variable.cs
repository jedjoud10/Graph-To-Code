using System;

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

    public static implicit operator Variable<T>(CustomCode<T> value) {
        return value.DoStuff();
    }

    public Variable<U> Swizzle<U>(string swizzle) {
        return new SwizzleNode<T, U> { a = this, swizzle = swizzle };
    }

    public Variable<U> Cast<U>() {
        return new CastNode<T, U> { a = this };
    }

    public Variable<T> Cached(int sizeReductionPower, string swizzle = "xyz") {
        return new CachedNode<T> { inner = this, sizeReductionPower = sizeReductionPower, sampler = new CachedSampler(), swizzle = swizzle };
    }

    public Variable<T> Min(Variable<T> other) {
        return new SimpleBinFuncNode<T> { a = this, b = other, func = "min" };
    }

    public Variable<T> Max(Variable<T> other) {
        return new SimpleBinFuncNode<T> { a = this, b = other, func = "max" };
    }
}