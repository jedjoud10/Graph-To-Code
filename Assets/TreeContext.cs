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

    public void Inject<T>(InjectedNode<T> node, string name, Func<object> func) {
        string newName = GenId(name);
        injector.injected.Add(newName, (Utils.TypeOf<T>(), () => func()));
        properties.Add(Utils.TypeOf<T>().ToStringType() + " " + newName + ";");
        Add(node, newName);
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

    // Binds a name to a no op variable that you can use in your code.
    // Useful when the variable is defined outside of the scope for example
    // Does not generate a custom name for it
    public Variable<T> AliasExternalInput<T>(string name) {
        var a = new NoOp<T> { };
        this.Add(a, name);
        return a;
    }

    public Variable<T> AssignOnly<T>(string name, Variable<T> output) {
        return new AssignOnly<T> {
            name = name,
            inner = output,
        };
    }

    // Assign a new variable using its inner value and a name given for it
    // Internally returns a NoOp
    public Variable<T> AssignTempVariable<T>(string name, string value) {
        var a = new NoOp<T> { };
        DefineAndBindNode<T>(a, name, value);
        return a;
    }

    // Assigns an already defined node to some value in the code
    public void DefineAndBindNode(TreeNode node, Utils.StrictType type, string name, string value, bool constant = false, bool rngName = true, bool assignOnly = false) {
        if (!Contains(node)) {
            string newName = rngName ? GenId(name) : name;
            string suffix = constant ? "const " : "";
            
            if (assignOnly) {
                lines.Add(newName + " = " + value + ";");
            } else {
                lines.Add(suffix + type.ToStringType() + " " + newName + " = " + value + ";");
            }
            Add(node, newName);
        }
    }

    public void DefineAndBindNode<T>(TreeNode node, string name, string value, bool constant = false, bool rngName = true, bool assignOnly = false) {
        DefineAndBindNode(node, Utils.TypeOf<T>(), name, value, constant, rngName, assignOnly);
    }

    /*
    public string DefineVariable<T>(string name, string value, bool constant = false) {
        string newName = GenId(name);
        string suffix = constant ? "const " : "";
        lines.Add(suffix + Utils.TypeOf<T>().ToStringType() + " " + newName + " = " + value + ";");
        return newName;
    }


    */



    /*

    */

    public void Parse(List<TreeNode> symbols) {
        // forward pass to convert each node to its string representation
        for (int i = symbols.Count - 1; i >= 0; i--) {
            symbols[i].Handle(this);

        }
    }

    public (List<TreeNode>, int) Handlinate(TreeNode head) {
        PreHandle preHandle = new PreHandle(head);
        var symbols = preHandle.TreeNodate(this);
        return (symbols, preHandle.hash);
    }
}