using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public abstract class TextureDescriptor {
    public FilterMode filter;
    public TextureWrapMode wrap;
    public List<string> readKernels;
    public string name;

    public abstract ExecutorTexture Create(int size);
}

public class TempTextureDescriptor : TextureDescriptor {
    public Utils.StrictType type;
    public string writeKernel;
    public bool threeDimensions;
    public int sizeReductionPower;
    public bool mips;

    public override ExecutorTexture Create(int size) {
        RenderTexture rt;
        int textureSize = size / (1 << sizeReductionPower);
        textureSize = Mathf.Max(textureSize, 1);

        if (threeDimensions) {
            rt = Utils.Create3DRenderTexture(textureSize, Utils.ToGfxFormat(type), filter, wrap, mips);
        } else {
            rt = Utils.Create2DRenderTexture(textureSize, Utils.ToGfxFormat(type), filter, wrap, mips);
        }

        return new TemporaryExecutorTexture(name, readKernels, rt, writeKernel, mips);
    }
}

public class GradientTextureDescriptor : TextureDescriptor {
    public int size;

    public override ExecutorTexture Create(int volumeSize) {
        Texture2D texture = new Texture2D(size, 1, DefaultFormat.LDR, TextureCreationFlags.None);
        texture.wrapMode = wrap;
        texture.filterMode = filter;

        return new ExecutorTexture(name, readKernels, texture);
    }
}

public class UserTextureDescriptor : TextureDescriptor {
    public Texture texture;

    public override ExecutorTexture Create(int volumeSize) {
        return new ExecutorTexture(name, readKernels, texture);
    }
}