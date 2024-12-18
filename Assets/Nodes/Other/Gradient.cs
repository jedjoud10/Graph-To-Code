using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GradientNode<T> : Variable<T> {
    public Gradient gradient;
    public Variable<float> mixer;
    public Variable<float> inputMin;
    public Variable<float> inputMax;
    public bool remapOutput;
    public int size;

    private string gradientTextureName;

    public override void Handle(TreeContext context) {
        if (!context.Contains(this)) {
            HandleInternal(context);
        } else {
            context.gradientTextures[gradientTextureName].readKernels.Add($"CS{context.scopes[context.currentScope].name}");
        }
    }

    public override void HandleInternal(TreeContext context) {
        inputMin.Handle(context);
        inputMax.Handle(context);
        mixer.Handle(context);
        context.Hash(size);

        string textureName = context.GenId($"_gradient_texture");
        gradientTextureName = textureName;
        context.properties.Add($"Texture1D {textureName}_read;");
        context.properties.Add($"SamplerState sampler{textureName}_read;");

        context.Inject2((compute, textures) => {
            Texture2D tex = (Texture2D)textures[textureName];

            Color32[] colors = new Color32[size];
            for (int i = 0; i < size; i++) {
                float t = (float)i / size;
                colors[i] = gradient.Evaluate(t);
            }
            tex.SetPixels32(colors);
            tex.Apply();
        });

        string swizzle = Utils.SwizzleFromFloat4<T>();
        Variable<T> firstRemap = context.AssignTempVariable<T>($"{context[mixer]}_gradient_remapped", $"Remap({context[mixer]}, {context[inputMin]}, {context[inputMax]}, 0.0, 1.0)");
        Variable<T> sample = context.AssignTempVariable<T>($"{textureName}_gradient", $"{textureName}_read.SampleLevel(sampler{textureName}_read, {context[firstRemap]}, 0).{swizzle}");

        if (remapOutput) {
            Variable<T> secondRemap = context.AssignTempVariable<T>($"{context[mixer]}_gradient_second_remapped", $"Remap({context[sample]}, 0.0, 1.0, {context[inputMin]}, {context[inputMax]})");
            context.DefineAndBindNode<T>(this, $"{textureName}_gradient_sampled", context[secondRemap]);
        } else {
            context.DefineAndBindNode<T>(this, $"{textureName}_gradient_sampled", context[sample]);
        }


        context.gradientTextures.Add(gradientTextureName, new GradientTexture {
            size = size,
            readKernels = new List<string>() { $"CS{context.scopes[context.currentScope].name}" },
        });
    }
}

public class Ramp<T> {
    public Gradient gradient;
    public int size = 128;

    public Variable<float> inputMin = 0.0f;
    public Variable<float> inputMax = 1.0f;
    public bool remapOutput = true;

    public Ramp(Gradient gradient, int size = 128) {
        this.size = size;
        this.gradient = gradient;
    }

    public Ramp(Gradient gradient, Variable<float> inputMin, Variable<float> inputMax, int size = 128, bool remapOutput = true) {
        this.size = size;
        this.gradient = gradient;
        this.inputMin = inputMin;
        this.inputMax = inputMax;
        this.remapOutput = remapOutput;
    }

    public Variable<T> Evaluate(Variable<float> mixer) {
        return new GradientNode<T> {
            gradient = gradient,
            mixer = mixer,
            size = size,
            inputMin = inputMin,
            inputMax = inputMax,
            remapOutput = remapOutput
        };
    }
}
