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
    public int hash;
    private int indent = 0;

    public ShaderManager(bool discard) {
        lines = new List<string>() { "// lines" };
        properties = new List<string>() { "// properties" };
        injected = new Dictionary<string, (Utils.StrictType, Func<object>)>();
        varNamesToId = new Dictionary<string, int>();
        counter = 0;
    }

    public void BeginIndentScope() {
        indent++;
    }

    public void EndIndentScope() {
        indent--;
    }

    public void AddLine(string line) {
        lines.Add(new string('\t', indent) + line);
    }


    public void HashenateMaxx(object val) {
        hash = HashCode.Combine(hash, val.GetHashCode());
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
        AddLine(suffix + Utils.TypeOf<T>().ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }

    public void SetVariable(string name, string value) {
        AddLine(name + " = " + value + ";");
    }
}

