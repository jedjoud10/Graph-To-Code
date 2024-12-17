using System;
using Unity.Mathematics;
using UnityEngine;


public class TransformationNode : Variable<float3> {
    public Variable<float3> input;
    public InlineTransform transform;

    public override void HandleInternal(TreeContext ctx) {
        input.Handle(ctx);

        string matrixName = ctx.GenId("matrix");
        ctx.properties.Add($"float4x4 {matrixName};");

        ctx.Inject2((compute, textures) => {
            float4x4 matrix = transform == null ? float4x4.identity : math.AffineTransform(transform.position, Quaternion.Euler(transform.rotation), transform.scale);
            
            compute.SetMatrix(matrixName, matrix);
        });

        ctx.DefineAndBindNode<float3>(this, "projected", $"mul({matrixName}, float4({ctx[input]}, 1.0)).xyz");
    }
}

[Serializable]
public class InlineTransform {
    public Vector3 position = Vector3.zero;
    public Vector3 rotation = Vector3.zero;
    public Vector3 scale = Vector3.one;
}

public class ApplyTransformation {
    public InlineTransform transform;

    public ApplyTransformation(InlineTransform transform) {
        this.transform = transform;
    }

    public Variable<float3> Transform(Variable<float3> input) {
        return new TransformationNode {
            input = input,
            transform = transform
        };
    }
}

