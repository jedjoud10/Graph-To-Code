using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

// Common utils and shorthand forms
public static class Utils {
    // Get the x value of the float3
    public static Var<float> x(this Var<float3> vec3) {
        ShaderManager.singleton.DefineVariable<float>(vec3.name + "_x", vec3.name + ".x");
        
        return new Var<float> {
            name = vec3.name + "_x",
        };
    }

    // Get the y value of the float3
    public static Var<float> y(this Var<float3> vec3) {
        ShaderManager.singleton.DefineVariable<float>(vec3.name + "_y", vec3.name + ".y");

        return new Var<float> {
            name = vec3.name + "_y",
        };
    }

    // Get the z value of the float3
    public static Var<float> z(this Var<float3> vec3) {
        ShaderManager.singleton.DefineVariable<float>(vec3.name + "_z", vec3.name + ".z");

        return new Var<float> {
            name = vec3.name + "_z",
        };
    }

    // Convert type data to string
    public static string TypeOf<T>() {
        string nuhuh = typeof(T).Name;

        if (nuhuh == "Single") {
            nuhuh = "float";
        }

        return nuhuh;
    }
}
