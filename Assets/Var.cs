using System;
using Unity.Mathematics;


[Serializable]
public abstract class TreeNode {
    // RECURSIVE!!!
    public abstract void Handle(TreeContext context);
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
        return new SwizzleNode<T, U> { a = this, swizzle = swizzle };
    }

    public Variable<U> Cast<U>() {
        return new CastNode<T, U> { a = this };
    }

    public Variable<T> Cached(string name) {
        return new Cached<T> { inner = this, name = name };
    }
}

public class NoOp<T> : Variable<T> {
    public override void Handle(TreeContext context) {
    }
}

public class AssignOnly<T> : Variable<T> {
    public string name;
    public Variable<T> inner;

    public override void Handle(TreeContext ctx) {
        inner.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, name, ctx[inner], false, false, true);
    }
}

public class AssignOnly2<T> : Variable<T> {
    public string value;
    public Variable<T> inner;

    public override void Handle(TreeContext ctx) {
        inner.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, ctx[inner], value, false, false, true);
    }
}


// TODO: Keep track of the inputs used for this variable so that we can use a 2d texture instead of a 3d one each time
// technically no need for this since we can just always pass the given position (since we always branch off of there)

// TODO: Must create a different compute shader with required variables
// done, this now does the recursive handling within another scope so it's fine

// TODO: Must create texture and create a variable that reads from it in the OG shader
// sub-TODO: can squish multiple cached calls into a single RGBA texture (of the same size) to help performance
[Serializable]
public class Cached<T> : Variable<T> {
    public Variable<T> inner;
    public string name;

    // looks up all the dependencies of a and makes sure that they are 2D (could be xy, yx, xz, whatever)
    // clones those dependencies to a secondary compute kernel
    // create temporary texture that is written to by that kernel
    // read said texture with appropriate swizzles in the main kernel

    public override void Handle(TreeContext context) {
        int index = context.scopes.Count;
        int oldScopeIndex = context.currentScope;
        context.scopes.Add(new TreeContext.KernelScope(context.scopeDepth + 1) {
            name = name,
            output = (Utils.TypeOf<T>(), inner),
        });

        var startNode = context[context.start];

        // ENTER NEW SCOPE!!!
        context.currentScope = index;
        context.scopeDepth++;

        // Add the start node (position node) to the new scope
        context.scopes[index].namesToNodes.TryAdd(context.start, startNode);

        // Call the recursive handle function within the indented scope
        inner.Handle(context);

        // Copy of the name of the inner variable
        var tempName = context[inner];

        // EXIT SCOPE!!!
        context.scopeDepth--;
        context.currentScope = oldScopeIndex;

        context.DefineAndBindNode<T>(this, $"{tempName}_cached", $"{name}(position)");
    }
}