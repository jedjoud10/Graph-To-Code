using System;
using System.Collections.Generic;
using UnityEngine;

public class PreHandle {
    private List<TreeNode> symbols;
    private int remaining;
    public int hash;

    public PreHandle(TreeNode head) {
        this.symbols = new List<TreeNode>() { head };
        this.remaining = 1;
        this.hash = 0;
    }

    public List<TreeNode> TreeNodate(TreeContext ctx) {
        // forward pass, add all symbols to list
        for (int i = 0; i < 5000; i++) {
            if (remaining == 0) {
                break;
            }

            symbols[i].PreHandle(this);
            remaining--;
        }
        return symbols;
    }

    public void Hash(object val) {
        hash = HashCode.Combine(hash, val.GetHashCode());
    }

    public void RegisterDependency(TreeNode node) {
        this.symbols.Add(node);
        this.remaining++;
    }
}

public class PropertyInjector {
    public PropertyInjector() {
        this.injected = new Dictionary<string, (Utils.StrictType, Func<object>)>();
    }

    public Dictionary<string, (Utils.StrictType, Func<object>)> injected;

    public void UpdateInjected(ComputeShader shader) {
        foreach (var (name, (type, func)) in injected) {
            Utils.SetComputeShaderObj(shader, name, func(), type);
        }
    }
}

public class TreeContext {
    private List<string> lines;
    private Dictionary<TreeNode, string> namesToNodes;
    private Dictionary<string, int> varNamesToId;
    public PropertyInjector injector;
    private List<string> properties;
    private int counter;
    private bool debugNames;

    public List<string> Lines { get { return lines; } }
    public List<string> Properties { get { return properties; } }

    public string this[TreeNode node] {
        get => namesToNodes[node];
    }

    public TreeContext(bool debugNames) {
        this.lines = new List<string>();
        this.properties = new List<string>();
        this.namesToNodes = new Dictionary<TreeNode, string>();
        this.injector = new PropertyInjector();
        this.varNamesToId = new Dictionary<string, int>();
        this.debugNames = debugNames;
        this.counter = 0;
    }

    public string Inject(Utils.StrictType type, string name, Func<object> func) {
        string newName = GenId(name);
        injector.injected.Add(newName, (type, () => func()));
        properties.Add(type.ToStringType() + " " + newName + ";");
        return newName;
    }

    public void Add(TreeNode node, string name) {
        this.namesToNodes.Add(node, name);
    }

    public bool Contains(TreeNode node) {
        return this.namesToNodes.ContainsKey(node);
    }

    public string GenId(string name) {
        int id = 0;

        if (varNamesToId.ContainsKey(name)) {
            id = ++varNamesToId[name];
        } else {
            varNamesToId.Add(name, 0);
        }

        if (debugNames) {
            return name + "_" + id.ToString();
        } else {
            return "_" + ++counter;
        }
    }

    public string DefineVariable(Utils.StrictType type, string name, string value, bool constant = false) {
        string newName = GenId(name);
        string suffix = constant ? "const " : "";
        lines.Add(suffix + type.ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }

    public string DefineVariable<T>(string name, string value, bool constant = false) {
        string newName = GenId(name);
        string suffix = constant ? "const " : "";
        lines.Add(suffix + Utils.TypeOf<T>().ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }

    public Variable<T> DefineVariableNoOp<T>(string name, string value) {
        string newName = GenId(name);
        lines.Add(Utils.TypeOf<T>().ToStringType() + " " + newName + " = " + value + ";");
        var a = new NoOP<T> { };
        this.Add(a, newName);
        return a;
    }

    public void Set<T>(string name, Variable<T> hmm) {
        lines.Add(name + " = " + this[hmm] + ";");
    }

    public Variable<T> Bind<T>(string newName) {
        var a = new NoOP<T> { };
        this.Add(a, newName);
        return a;
    }

    public void Parse(List<TreeNode> symbols) {
        // forward pass to convert each node to its string representation
        for (int i = symbols.Count - 1; i >= 0; i--) {
            if (!Contains(symbols[i])) {
                string name = symbols[i].Handle(this);
                Add(symbols[i], name);
            }
        }
    }

    public (List<TreeNode>, int) Handlinate(TreeNode head) {
        PreHandle preHandle = new PreHandle(head);
        var symbols = preHandle.TreeNodate(this);
        return (symbols, preHandle.hash);
    }
}