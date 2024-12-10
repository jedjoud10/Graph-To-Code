using System;
using System.Collections.Generic;
using Unity.Android.Types;


public class PreHandle {
    private List<TreeNode> symbols;
    private int remaining;

    public PreHandle(TreeNode head) {
        this.symbols = new List<TreeNode>() { head };
        this.remaining = 1;
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

    public void RegisterDependency(TreeNode node) {
        UnityEngine.Debug.Log(node.ToString());
        if (node == null) {
            UnityEngine.Debug.Log("aa");
        }

        this.symbols.Add(node);
        this.remaining++;
    }
}

public class TreeContext {
    private List<string> lines;
    private Dictionary<TreeNode, string> namesToNodes;
    private Dictionary<string, int> varNamesToId;
    private Dictionary<string, (Utils.StrictType, Func<object>)> injected;
    private List<string> properties;
    private int counter;
    private bool debugNames;

    public string this[TreeNode node] {
        get => namesToNodes[node];
    }

    public TreeContext(bool debugNames) {
        this.lines = new List<string>();
        this.properties = new List<string>();
        this.namesToNodes = new Dictionary<TreeNode, string>();
        this.injected = new Dictionary<string, (Utils.StrictType, Func<object>)>();
        this.varNamesToId = new Dictionary<string, int>();
        this.debugNames = debugNames;
        this.counter = 0;
    }

    public string Inject(Utils.StrictType type, string name, Func<object> func) {
        string newName = GenId(name);
        injected.Add(newName, (type, () => func()));
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

    public Variable<T> Bind<T>(string newName) {
        var a = new NoOP<T> { };
        this.Add(a, newName);
        return a;
    }

    public List<string> Combine() {
        List<string> result = new List<string>();
        result.AddRange(properties);
        result.AddRange(lines);
        return result;
    }

    public static void Parse(TreeNode head) {
        TreeContext ctx = new TreeContext(false);
        PreHandle preHandle = new PreHandle(head);
        var symbols = preHandle.TreeNodate(ctx);

        // forward pass to convert each node to its string representation
        for (int i = symbols.Count - 1; i >= 0; i--) {
            if (!ctx.Contains(symbols[i])) {
                string name = symbols[i].Handle(ctx);
                ctx.Add(symbols[i], name);
            }
        }

        foreach (string line in ctx.Combine()) {
            UnityEngine.Debug.Log(line);
        }
    }
}