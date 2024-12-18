using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TextureSampleNode<T> : Variable<float4> {
    public Variable<T> coordinates;
    public TextureSampler sampler;

    private string tempTextureName;
    public override void Handle(TreeContext context) {
        if (!context.Contains(this)) {
            HandleInternal(context);
        } else {
            context.tempTextures[tempTextureName].readKernels.Add($"CS{context.scopes[context.currentScope].name}");
        }
    }


    public override void HandleInternal(TreeContext context) {
        coordinates.Handle(context);
        context.Hash(sampler.filter);
        context.Hash(sampler.wrap);
        sampler.level.Handle(context);

        int dimensionality = Utils.DimensionalitySafeTextureSample<T>();

        string textureName = context.GenId($"_user_texture");

        tempTextureName = textureName;
        context.properties.Add($"Texture{dimensionality}D {textureName}_read;");
        context.properties.Add($"SamplerState sampler{textureName}_read;");
        context.DefineAndBindNode<float4>(this, "hehehehe", $"{textureName}_read.SampleLevel(sampler{textureName}_read, {context[coordinates]}, {context[sampler.level]})");
        context.userTextures.Add(tempTextureName, sampler.texture);
    }
}

public class TextureSampler {
    public Texture texture;
    public FilterMode filter;
    public TextureWrapMode wrap;
    public Variable<float> level;

    public TextureSampler(Texture texture) {
        this.filter = FilterMode.Trilinear;
        this.wrap = TextureWrapMode.Clamp;
        this.level = 0.0f;
        this.texture = texture;
    }

    public Variable<float4> Cache<T>(Variable<T> input) {
        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D && Utils.DimensionalitySafeTextureSample<T>() != 2) {
            throw new Exception();
        }

        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D && Utils.DimensionalitySafeTextureSample<T>() != 3) {
            throw new Exception();
        }

        return new TextureSampleNode<T> {
            coordinates = input,
            sampler = this,
        };
    }
}