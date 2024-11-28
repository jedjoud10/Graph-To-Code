using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// Handles initializing variables, functions, and other shader stuff
public class ShaderManager {
    public static ShaderManager singleton;
    public List<string> lines;
    private Dictionary<string, int> varNamesToId;
    public ShaderManager() {
        lines = new List<string>();
        varNamesToId = new Dictionary<string, int>();
        DefineVariable<float>("identity_float", "0.0");
        DefineVariable<float2>("identity_float2", "float2(0.0, 0.0)");
        DefineVariable<float3>("identity_float3", "float3(0.0, 0.0, 0.0)");
    }

    public void AddLine(string line) {
        lines.Add(line);
    }

    public string DefineVariable<T>(string name, string value) {
        int id = 0;

        if (varNamesToId.ContainsKey(name)) {
            id = ++varNamesToId[name];
        } else {
            varNamesToId.Add(name, 0);
        }

        string newName = name + "_" + id.ToString();
        lines.Add(Utils.TypeOf<T>() + " " + newName + " = " + value + ";");
        return newName;
    }
}

