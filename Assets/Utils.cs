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

    public enum Swizzle2Mode {
        XY,
        XZ,
        YX,
        YZ,
        ZX,
        ZY,
    }

    public enum Swizzle3Mode {
        XYX,
        XYZ,
        XZX,
        XZY,
        YXY,
        YXZ,
        YZX,
        YZY,
        ZXY,
        ZXZ,
        ZYX,
        ZYZ,
    }

    public enum Swizzle4Mode {
        XYXY,
        XYXZ,
        XYZX,
        XYZY,
        XZXY,
        XZXZ,
        XZYX,
        XZYZ,
        YXYX,
        YXYZ,
        YXZX,
        YXZY,
        YZXY,
        YZXZ,
        YZYX,
        YZYZ,
        ZXYX,
        ZXYZ,
        ZXZX,
        ZXZY,
        ZYXY,
        ZYXZ,
        ZYZX,
        ZYZY,
    }

    public static string ToStringType(this StrictType data) {
        return data.ToString().ToLower();
    }

    public static string ToDefinableString<T>(T value) {
        string a = value.ToString();
        object cock = (object)value;

        switch (Utils.TypeOf<T>()) {
            case Utils.StrictType.Float2:
                float2 f2 = (float2)cock;
                return $"float2({f2.x},{f2.y})";
            case Utils.StrictType.Float3:
                float3 f3 = (float3)cock;
                return $"float3({f3.x},{f3.y},{f3.z})";
            case Utils.StrictType.Float4:
                float4 f4 = (float4)cock;
                return $"float3({f4.x},{f4.y},{f4.z})";
            default:
                return value.ToString();
        }
    }

    /*
    // Get the x value of the float3
    public static Var<float> X(this Var<float3> vec3) {
        return new Var<float> {
            name = ShaderManager.singleton.DefineVariable<float>(vec3.name + "_x", vec3.name + ".x"),
        };
    }

    // Get the y value of the float3
    public static Var<float> Y(this Var<float3> vec3) {
        return new Var<float> {
            name = ShaderManager.singleton.DefineVariable<float>(vec3.name + "_y", vec3.name + ".y"),
        };
    }

    // Get the z value of the float3
    public static Var<float> Z(this Var<float3> vec3) {
        return new Var<float> {
            name = ShaderManager.singleton.DefineVariable<float>(vec3.name + "_z", vec3.name + ".z"),
        };
    }

    // Construct float3 from 1 float
    public static Var<float3> Float3(this Var<float> val) {
        return new Var<float3> {
            name = ShaderManager.singleton.DefineVariable<float3>(val.name + "_f3", $"float3({val.name}, {val.name}, {val.name})"),
        };
    }

    // Construct float2 from 1 float
    public static Var<float2> Float2(this Var<float> val) {
        return new Var<float2> {
            name = ShaderManager.singleton.DefineVariable<float2>(val.name + "_f2", $"float2({val.name}, {val.name})"),
        };
    }

    private static Var<T> SwizzleInternal<T, G>(this Var<G> val, string stringed) {
        if (val.Dimensionality < stringed.Length) {
            throw new System.Exception("Nuhuh!!");
        }

        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>(val.name + "_swizzled", $"{val.name}.{stringed}"),
        };
    }

    public static Var<float2> Swizzle2<G>(this Var<G> val, Swizzle2Mode swizzle) {
        return SwizzleInternal<float2, G>(val, swizzle.ToString().ToLower());
    }

    public static Var<float3> Swizzle3<G>(this Var<G> val, Swizzle3Mode swizzle) {
        return SwizzleInternal<float3, G>(val, swizzle.ToString().ToLower());
    }

    public static Var<float4> Swizzle4<G>(this Var<G> val, Swizzle4Mode swizzle) {
        return SwizzleInternal<float4, G>(val, swizzle.ToString().ToLower());
    }

    public static Var<T> Mix<T>(this Var<float> t, Var<T> a, Var<T> b) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>($"{t.name}_mix_{a.name}_{b.name}", $"lerp({a.name}, {b.name}, {t.name})"),
        };
    }
    */

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