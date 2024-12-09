using System;
using Unity.Mathematics;

/*
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
            switch (Utils.TypeOf<T>()) {
                case Utils.StrictType.Float:
                    return 1;
                case Utils.StrictType.Float2:
                    return 2;
                case Utils.StrictType.Float3:
                    return 3;
                case Utils.StrictType.Float4:
                    return 4;
                case Utils.StrictType.Uint:
                    return 1;
                case Utils.StrictType.Int:
                    return 1;
            }

            return -1;
        }
    }

    // Implicitly convert a constant value to a variable
    public static implicit operator Var<T>(T value) {
        return new Var<T> {
            name = ShaderManager.singleton.DefineVariable<T>("st_", Utils.ToDefinableString(value), true),
        };
    }

    public static implicit operator Var<T>(Inject<T> value) {
        return new Var<T> {
            name = ShaderManager.singleton.Inject<T>("inj_" + Utils.TypeOf<T>().ToStringType(), () => value.x),
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

    // TODO: Keep track of the inputs used for this variable so that we can use a 2d texture instead of a 3d one each time
    // TODO: Must create a different compute shader with required variables
    // TODO: Must create texture and create a variable that reads from it in the OG shader
    // sub-TODO: can squish multiple cached calls into a single RGBA texture (of the same size) to help performance
    public CachedVar<T> Cached(int sizeReduction = 1) {
        return null;
    }
}

public class CachedVar<T> {
    public Var<T> var;

    // Central difference gradient approximation
    public Var<float3> ApproxGradent() {
        return float3.zero;
    }
}
*/