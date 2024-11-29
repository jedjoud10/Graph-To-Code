using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

// Common utils and shorthand forms
public static class Utils {
    public enum StrictType {
        Float,
        Float2,
        Float3,
        Float4,
        Uint,
        Int,
    }

    public static string ToStringType(this StrictType data) {
        return data.ToString().ToLower();
    }

    // Get the x value of the float3
    public static Var<float> x(this Var<float3> vec3) {
        return new Var<float> {
            name = ShaderManager.singleton.DefineVariable<float>(vec3.name + "_x", vec3.name + ".x"),
        };
    }

    // Get the y value of the float3
    public static Var<float> y(this Var<float3> vec3) {
        return new Var<float> {
            name = ShaderManager.singleton.DefineVariable<float>(vec3.name + "_y", vec3.name + ".y"),
        };
    }

    // Get the z value of the float3
    public static Var<float> z(this Var<float3> vec3) {
        return new Var<float> {
            name = ShaderManager.singleton.DefineVariable<float>(vec3.name + "_z", vec3.name + ".z"),
        };
    }

    // Construct float3 from 1 float
    public static Var<float3> float3(this Var<float> val) {
        return new Var<float3> {
            name = ShaderManager.singleton.DefineVariable<float3>(val.name + "_f3", $"float3({val.name}, {val.name}, {val.name})"),
        };
    }

    // Construct float2 from 1 float
    public static Var<float2> float2(this Var<float> val) {
        return new Var<float2> {
            name = ShaderManager.singleton.DefineVariable<float2>(val.name + "_f2", $"float2({val.name}, {val.name})"),
        };
    }

    // Convert type data to string
    public static StrictType TypeOf<T>() {
        string tn = typeof(T).Name;
        StrictType output;

        switch (tn) {
            case "Single":
                output = StrictType.Float; break;
            case "float2":
                output = StrictType.Float2; break;
            case "float3":
                output = StrictType.Float3; break;
            case "float4":
                output = StrictType.Float4; break;
            case "UInt32":
                output = StrictType.Uint; break;
            case "Int32":
                output = StrictType.Int; break;
            default:
                throw new System.Exception("Type not supported");
        }

        return output;
    }

    public static void SetComputeShaderObj(ComputeShader shader, string id, object val, StrictType type) {
        switch (type) {
            case StrictType.Float:
                shader.SetFloat(id, (float)val);
                break;
            case StrictType.Float2:
                float2 temp = (float2)val;
                shader.SetFloats(id, temp.x, temp.y);
                break;
            case StrictType.Float3:
                float3 temp2 = (float3)val;
                shader.SetFloats(id, temp2.x, temp2.y, temp2.z);
                break;
            case StrictType.Float4:
                float4 temp3 = (float4)val;
                shader.SetFloats(id, temp3.x, temp3.y, temp3.z, temp3.w);
                break;
            case StrictType.Uint:
                shader.SetInt(id, (int)(uint)val);
                break;
            case StrictType.Int:
                shader.SetInt(id, (int)val);
                break;
        }
    }
}