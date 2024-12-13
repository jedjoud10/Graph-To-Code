using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static TreeContext;


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

    public Variable<T> Cached(int sizeReductionPower, Variable<float> scale, FilterMode filter = FilterMode.Trilinear, TextureWrapMode wrap = TextureWrapMode.Mirror) {
        return new Cached<T> { inner = this, sizeReductionPower = sizeReductionPower, wrap = wrap, filter = filter, scale = scale };
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

// TODO: dedupe stuff pls thx
[Serializable]
public class Cached<T> : Variable<T> {
    public Variable<T> inner;
    public int sizeReductionPower;
    public FilterMode filter;
    public TextureWrapMode wrap;
    public Variable<float> scale;

    // looks up all the dependencies of a and makes sure that they are 2D (could be xy, yx, xz, whatever)
    // clones those dependencies to a secondary compute kernel
    // create temporary texture that is written to by that kernel
    // read said texture with appropriate swizzles in the main kernel

    public override void Handle(TreeContext context) {
        context.Hash(sizeReductionPower);
        context.Hash(filter);
        context.Hash(wrap);
        scale.Handle(context);

        string scopeName = context.GenId($"CachedScope");
        string textureName = context.GenId($"_cached_texture");
        context.properties.Add($"RWTexture3D<{Utils.TypeOf<T>().ToStringType()}> {textureName}_write;");
        context.properties.Add($"Texture3D {textureName}_read;");
        context.properties.Add($"SamplerState sampler{textureName}_read;");

        int index = context.scopes.Count;
        int oldScopeIndex = context.currentScope;
        context.scopes.Add(new TreeContext.KernelScope(context.scopeDepth + 1) {
            name = scopeName,
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

        int frac = (1 << sizeReductionPower);

        context.DefineAndBindNode<T>(this, $"{tempName}_cached", $"{textureName}_read.SampleLevel(sampler{textureName}_read, (float3(id) / size) * {context[scale]}, 0).x");

        string compute = $@"
#pragma kernel CS{scopeName}
[numthreads(8, 8, 8)]
void CS{scopeName}(uint3 id : SV_DispatchThreadID) {{
    {textureName}_write[id] = {scopeName}((float3(id * {frac}) + offset) * scale, id);
}}";

        context.computeKernels.Add(compute);
        context.computeKernelNameAndDepth.Add(new ComputeKernelDispatch {
            name = $"CS{scopeName}",
            depth = context.scopeDepth + 1,
            sizeReductionPower = sizeReductionPower,
        });
        context.tempTextures.Add(new TreeContext.TempTexture {
            name = textureName,
            sizeReductionPower = sizeReductionPower,
            type = Utils.TypeOf<T>(),
            writeKernel = $"CS{scopeName}",
            filter = filter,
            wrap = wrap,
            readKernels = new List<string>() { $"CS{context.scopes[oldScopeIndex].name}" },
        });
    }
}