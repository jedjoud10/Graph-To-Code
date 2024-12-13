using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using static TreeContext;
using UnityEngine.Profiling;
using static TreeEditor.TextureAtlas;
using UnityEngine.UIElements;
using System.Drawing;

[Serializable]
public class DefineNode<T> : Variable<T> {
    [SerializeField]
    public string value;
    public bool constant;

    public override void HandleInternal(TreeContext ctx) {
        ctx.DefineAndBindNode(this, Utils.TypeOf<T>(), "c", value, constant);
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

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        b.Handle(ctx);
        ctx.DefineAndBindNode<T>(this, $"{ctx[a]}_op_{ctx[b]}", $"{ctx[a]} {op} {ctx[b]}");
    }
}

[Serializable]
public class CastNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> a;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<O>(this, $"{ctx[a]}_casted", $"{ctx[a]}");
    }
}

[Serializable]
public class SwizzleNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> a;
    [SerializeField]
    public string swizzle;

    public override void HandleInternal(TreeContext ctx) {
        a.Handle(ctx);
        ctx.DefineAndBindNode<O>(this, $"{ctx[a]}_swizzled", $"{ctx[a]}.{swizzle}");
    }
}

[Serializable]
public class ConstructNode<I, O> : Variable<O> {
    [SerializeReference]
    public Variable<I> x;
    [SerializeReference]
    public Variable<I> y;
    [SerializeReference]
    public Variable<I> z;
    [SerializeReference]
    public Variable<I> w;



    public override void HandleInternal(TreeContext ctx) {
        string C(Variable<I> variable) {
            if (variable == null) {
                return "0.0";
            } else {
                return ctx[variable];
            }
        }

        x?.Handle(ctx);
        y?.Handle(ctx);
        z?.Handle(ctx);
        w?.Handle(ctx);

        switch (Utils.TypeOf<O>()) {
            case Utils.StrictType.Float2:
                ctx.DefineAndBindNode<O>(this, $"f2_ctor", $"float2({C(x)},{C(y)})");
                break;
            case Utils.StrictType.Float3:
                ctx.DefineAndBindNode<O>(this, $"f3_ctor", $"float3({C(x)},{C(y)},{C(z)})");
                break;
            case Utils.StrictType.Float4:
                ctx.DefineAndBindNode<O>(this, $"f4_ctor", $"float4({C(x)},{C(y)},{C(z)},{C(w)})");
                break;
            default:
                throw new Exception();
                break;
        }
    }
}

[Serializable]
public class InjectedNode<T> : Variable<T> {
    public Inject<T> a;
    public override void HandleInternal(TreeContext ctx) {
        ctx.Inject<T>(this, "inj", () => a.x);
    }
}

public class GradientNode<T> : Variable<T> {
    public Gradient gradient;
    public Variable<float> mixer;
    public Variable<T> inputMin;
    public Variable<T> inputMax;
    public int size;

    public override void Handle(TreeContext context) {
        if (!context.Contains(this)) {
            HandleInternal(context);
        } else {
            string name = context[this];
            context.gradientTextures[name].readKernels.Add($"CS{context.scopes[context.currentScope].name}");
        }
    }

    public override void HandleInternal(TreeContext context) {
        inputMin.Handle(context);
        inputMax.Handle(context);
        mixer.Handle(context);
        context.Hash(size);

        string textureName = context.GenId($"_gradient_texture");
        context.properties.Add($"Texture1D {textureName}_read;");
        context.properties.Add($"SamplerState sampler{textureName}_read;");

        context.Inject2("inj", (compute, textures) => {
            Texture2D tex = (Texture2D)textures[textureName];

            Color32[] colors = new Color32[size];
            for (int i = 0; i < size; i++) {
                float t = (float)i / size;
                colors[i] = gradient.Evaluate(t);
            }
            tex.SetPixels32(colors);
            tex.Apply();
        });

        Variable<T> firstRemap = context.AssignTempVariable<T>($"{context[mixer]}_gradient_remapped", $"Remap({context[mixer]}, {context[inputMin]}, {context[inputMax]}, 0.0, 1.0)");
        Variable<T> sample = context.AssignTempVariable<T>($"{textureName}_gradient", $"{textureName}_read.SampleLevel(sampler{textureName}_read, {context[firstRemap]}, 0)");
        Variable<T> secondRemap = context.AssignTempVariable<T>($"{context[mixer]}_gradient_second_remapped", $"Remap({context[sample]}, 0.0, 1.0, {context[inputMin]}, {context[inputMax]})");
        context.DefineAndBindNode<float4>(this, $"{textureName}_gradient_sampled", context[secondRemap]);

        context.gradientTextures.Add($"{textureName}_gradient", new TreeContext.GradientTexture {
            name = textureName,
            size = size,
            readKernels = new List<string>() { $"CS{context.scopes[context.currentScope].name}" },
        });
    }
}

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

public class Ramp<T> {
    public Gradient gradient;
    public int size = 32;

    public Variable<T> inputMin = Utils.Zero<T>();
    public Variable<T> inputMax = Utils.One<T>();

    public Ramp(Gradient gradient, int size = 32) {
        this.size = size;
        this.gradient = gradient;
    }

    public Ramp(Gradient gradient, Variable<T> inputMin, Variable<T> inputMax, int size = 32) {
        this.size = size;
        this.gradient = gradient;
        this.inputMin = inputMin;
        this.inputMax = inputMax;
    }

    public Variable<T> Evaluate(Variable<float> mixer) {
        return new GradientNode<T> {
            gradient = gradient,
            mixer = mixer,
            size = size,
            inputMin = inputMin,
            inputMax = inputMax,
        };
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

public class FiniteDiffer<I, O> {
    public Variable<O> FiniteDiffThatThang(Variable<I> input, float4 epsilon) {
        return null;
    }
}

// Create a function with this an input and output as function in/out, where the function is called 3 or 4 times (depending on dimensionality)
// Small changes in the input variable along those axis (with specified epsilon) and calculating the final changes in the output variable
public class FiniteDifferenciatedNode<I, O> : Variable<O> {
    public Variable<I> input;
    public Variable<O> output;
    public float4 diff;

    public override void HandleInternal(TreeContext context) {
        throw new NotImplementedException();
    }
}

[Serializable]
public class Inject<T> {
    public T x;

    public Inject(T a) {
        this.x = a;
    }
}