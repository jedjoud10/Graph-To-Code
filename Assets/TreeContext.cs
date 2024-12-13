using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

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
    // One scope per compute shader kernel.
    // Multiple scopes are used when we want to execute multiple kernels sequentially
    public class KernelScope {
        public List<string> lines;
        public Dictionary<TreeNode, string> namesToNodes;
        public int depth;

        public (Utils.StrictType, TreeNode) output;
        public string name;
        public int indent;
        
        public KernelScope(int depth) {
            this.lines = new List<string>();
            this.namesToNodes = new Dictionary<TreeNode, string>();
            this.indent = 1;
            this.depth = depth;
        }
        public void AddLine(string line) {
            lines.Add(new string('\t', indent) + line);
        }
    }

    [Serializable]
    public class TempTexture {
        public string name;
        public Utils.StrictType type;
        public FilterMode filter;
        public TextureWrapMode wrap;
        public List<string> readKernels;
        public string writeKernel;
        public int sizeReductionPower;
        public bool mips;
    }

    [Serializable]
    public class ComputeKernelDispatch {
        public string name;
        public int depth;
        public int sizeReductionPower;
    }

    public List<TempTexture> tempTextures;
    public List<string> computeKernels;
    public List<ComputeKernelDispatch> computeKernelNameAndDepth;
    public Dictionary<string, int> varNamesToId;
    public PropertyInjector injector;
    public List<string> properties;
    public int counter;
    public bool debugNames;
    public int hash;
    public List<KernelScope> scopes;
    public int currentScope = 0;
    public int scopeDepth = 0;

    public string this[TreeNode node] {
        get => scopes[currentScope].namesToNodes[node];
    }

    public int Indent {
        get => scopes[currentScope].indent;
        set => scopes[currentScope].indent = value;
    }

    public List<string> Properties { get { return properties; } }

    public TreeNode start;



    public TreeContext(bool debugNames) {
        this.properties = new List<string>();
        this.injector = new PropertyInjector();
        this.varNamesToId = new Dictionary<string, int>();
        this.debugNames = debugNames;
        this.scopes = new List<KernelScope> {
            new TreeContext.KernelScope(0) 
        };

        this.currentScope = 0;
        this.scopeDepth = 0;
        this.counter = 0;
        this.computeKernels = new List<string>();
        this.computeKernelNameAndDepth = new List<ComputeKernelDispatch>();
        this.tempTextures = new List<TempTexture>();
    }

    public void Inject<T>(InjectedNode<T> node, string name, Func<object> func) {
        if (!Contains(node)) {
            string newName = GenId(name);
            injector.injected.Add(newName, (Utils.TypeOf<T>(), () => func()));
            properties.Add(Utils.TypeOf<T>().ToStringType() + " " + newName + ";");
            Add(node, newName);
        }
    }

    // TODO: Create a function header with the specific variables as either input variables or output variables
    // Input variables must be initialized first with AliasExternalInput
    // For now, just create a simple function (with a random name) that takes in a variable of a specific type and returns a specific another variable of another type    
    // asdgfasdsdf

    public void Hash(object val) {
        hash = HashCode.Combine(hash, val.GetHashCode());
    }

    public void Add(TreeNode node, string name) {
        scopes[currentScope].namesToNodes.Add(node, name);
    }

    public bool Contains(TreeNode node) {
        return scopes[currentScope].namesToNodes.ContainsKey(node);
    }

    public void AddLine(string line) {
        scopes[currentScope].AddLine(line);
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
                AddLine(newName + " = " + value + ";");
            } else {
                AddLine(suffix + type.ToStringType() + " " + newName + " = " + value + ";");
            }
            Add(node, newName);
        }
    }

    public void ApplyInPlaceUnaryOp(TreeNode node, string name, string op, string value) {
        if (!Contains(node)) {
            AddLine(name + $" {op}= " + value + ";");
            Add(node, name);
        }
    }

    public void DefineAndBindNode<T>(TreeNode node, string name, string value, bool constant = false, bool rngName = true, bool assignOnly = false) {
        DefineAndBindNode(node, Utils.TypeOf<T>(), name, value, constant, rngName, assignOnly);
    }

    public void Parse(TreeNode head) {
        head.Handle(this);
    }
}