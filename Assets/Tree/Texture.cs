using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


public class GradientTexture {
    public List<string> readKernels;
    public int size;
}

public class TempTexture {
    public Utils.StrictType type;
    public FilterMode filter;
    public TextureWrapMode wrap;
    public List<string> readKernels;
    public string writeKernel;
    public bool threeDimensions;
    public int sizeReductionPower;
    public bool mips;
}

public abstract class KernelDispatchReadyTexture {
    public FilterMode filter;
    public TextureWrapMode wrap;
    public List<string> readKernels;

    public Texture inner;
    public string name;


}

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



public class ExecutorTexture {
    public string name;
    public List<string> readKernels;
    public Texture texture;

    public ExecutorTexture(string name, List<string> readKernels, Texture texture) {
        this.name = name;
        this.readKernels = readKernels;
        this.texture = texture;
    }

    public static implicit operator Texture(ExecutorTexture self) {
        return self.texture;
    }

    public virtual void BindToComputeShader(ComputeShader shader) {
        foreach (var readKernel in readKernels) {
            int readKernelId = shader.FindKernel(readKernel);
            shader.SetTexture(readKernelId, name + "_read", texture);
        }
    }

    public virtual void PostDispatchKernel(ComputeShader shader, int kernel) {
    }
}

public class TemporaryExecutorTexture : ExecutorTexture {
    public string writeKernel;
    private int writingKernel;
    public bool mips;

    public TemporaryExecutorTexture(string name, List<string> readKernels, Texture texture, string writeKernel, bool mips) : base(name, readKernels, texture) {
        this.writingKernel = -1;
        this.writeKernel = writeKernel;
        this.mips = mips;
    }

    public override void BindToComputeShader(ComputeShader shader) {
        base.BindToComputeShader(shader);
        int writeKernelId = shader.FindKernel(writeKernel);
        shader.SetTexture(writeKernelId, name + "_write", texture);
        writingKernel = writeKernelId;
    }

    public override void PostDispatchKernel(ComputeShader shader, int kernel) {
        base.PostDispatchKernel(shader, kernel);

        if (writingKernel == kernel && mips && texture is RenderTexture casted) {
            casted.GenerateMips();
        }
    }
}


public class OutputExecutorTexture : ExecutorTexture {
    public OutputExecutorTexture(string name, List<string> readKernels, Texture texture) : base(name, readKernels, texture) {
    }

    public override void BindToComputeShader(ComputeShader shader) {
        foreach (var readKernel in readKernels) {
            int readKernelId = shader.FindKernel(readKernel);
            shader.SetTexture(readKernelId, name, texture);
        }
    }
}