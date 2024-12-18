using System.Collections.Generic;


public class ScopeOutput {
    public Utils.StrictType type;
    public TreeNode node;
    public string name;

    public ScopeOutput(string name, Utils.StrictType type, TreeNode node) {
        this.type = type;
        this.name = name;
        this.node = node;
    }
}

// One scope per compute shader kernel.
// Multiple scopes are used when we want to execute multiple kernels sequentially
public class TreeScope {
    public List<string> lines;
    public Dictionary<TreeNode, string> namesToNodes;
    public int depth;
    public ScopeOutput[] outputs;
    public string name;
    public int indent;

    public TreeScope(int depth) {
        this.lines = new List<string>();
        this.namesToNodes = new Dictionary<TreeNode, string>();
        this.indent = 1;
        this.depth = depth;
        this.outputs = null;
        this.name = "asdfdas";
    }
    public void AddLine(string line) {
        lines.Add(new string('\t', indent) + line);
    }
}