using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Var<T> {
    // the name of the variable. automatically generated
    public string name;

    public static Var<T> CreateFromName(string name, string value) {
        string newName = ShaderManager.singleton.DefineVariable<T>(name, value);

        return new Var<T> {
            name = newName,
        };
    }

    // Get the dimensionality of the variable
    public int Dimensionality {
        get {
            if (typeof(T) == typeof(float)) {
                return 1;
            } else if (typeof(T) == typeof(float2)) {
                return 2;
            } else if (typeof(T) == typeof(float3)) {
                return 3;
            } else if (typeof(T) == typeof(float4)) {
                return 4;
            }

            return -1;
        }
    }

    // Null, zero, or identity value
    public static Var<T> Identity {
        get {
            return new Var<T> {
                name = "identity_" + Utils.TypeOf<T>().ToStringType(),
            };
        }
    }

    // Implicitly convert a constant value to a variable
    public static implicit operator Var<T>(T value) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>("st_", value.ToString(), true),
        };
    }

    // Inject a custom variable that will update its value dynamically based on the given callback 
    // Mainly used to pass inputs from fields from the unity editor to the graph
    public static Var<T> Inject(Func<T> callback) {
        // hook on before on graph execution (runtime) 
        // update "internal" value
        // send value to shader using uniform (make sure name matches up)
        return new Var<T> {
            name = ShaderManager.singleton.Inject<T>("inj_" + Utils.TypeOf<T>().ToStringType(), callback),
        };
    }

    // Common operators
    public static Var<T> operator +(Var<T> a, Var<T> b) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>(a.name + "_p_" + b.name, a.name + " + " + b.name)
        };
    }
    public static Var<T> operator -(Var<T> a, Var<T> b) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>(a.name + "_s_" + b.name, a.name + " - " + b.name)
        };
    }
    public static Var<T> operator *(Var<T> a, Var<T> b) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>(a.name + "_m_" + b.name, a.name + " * " + b.name)
        };
    }
    public static Var<T> operator /(Var<T> a, Var<T> b) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>(a.name + "_d_" + b.name, a.name + " / " + b.name)
        };
    }
}