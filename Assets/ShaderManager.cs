using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

// Handles initializing variables, functions, and other shader stuff
public class ShaderManager {
    public static ShaderManager singleton;
    public List<string> lines;
    private Dictionary<string, int> varNamesToId;
    Dictionary<string, (Utils.StrictType, Func<object>)> injected;
    public List<string> properties;
    private int counter;

    public ShaderManager() {
        lines = new List<string>() { "// lines" };
        properties = new List<string>() { "// properties" };
        injected = new Dictionary<string, (Utils.StrictType, Func<object>)>();
        varNamesToId = new Dictionary<string, int>();
        counter = 0;
        DefineVariable<float>("identity_float", "0.0", true);
        DefineVariable<float2>("identity_float2", "float2(0.0, 0.0)", true);
        DefineVariable<float3>("identity_float3", "float3(0.0, 0.0, 0.0)", true);
    }

    public void AddLine(string line) {
        lines.Add(line);
    }

    public string Inject<T>(string name, Func<T> func) {
        string newName = GenId(name);
        injected.Add(newName, (Utils.TypeOf<T>(), () => func()));
        properties.Add(Utils.TypeOf<T>().ToStringType() + " " + newName + ";");
        return newName;
    }

    public void UpdateInjected(ComputeShader shader) {
        foreach (var (name, (type, func)) in injected) {
            Utils.SetComputeShaderObj(shader, name, func(), type);
        }
    }

    private string GenId(string name) {
        int id = 0;

        if (varNamesToId.ContainsKey(name)) {
            id = ++varNamesToId[name];
        } else {
            varNamesToId.Add(name, 0);
        }

        return name + "_" + id.ToString();
        //string newName = "_v_" + ++counter;
    }

    public string DefineVariable<T>(string name, string value, bool constant = false) {
        string newName = GenId(name);
        string suffix = constant ? "const " : "";
        lines.Add(suffix + Utils.TypeOf<T>().ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }

    public void SetVariable(string name, string value) {
        lines.Add(name + " = " + value + ";");
    }
}

