using System.Collections.Generic;


public class ScopeArgument {
    public Utils.StrictType type;
    public TreeNode node;
    public string name;
    public bool output;

    public ScopeArgument(string name, Utils.StrictType type, TreeNode node, bool output) {
        this.type = type;
        this.name = name;
        this.node = node;
        this.output = output;
    }
}

// One scope per compute shader kernel.
// Multiple scopes are used when we want to execute multiple kernels sequentially
public class TreeScope {
    public List<string> lines;
    public Dictionary<TreeNode, string> namesToNodes;
    public int depth;
    public ScopeArgument[] arguments;
    public string name;
    public int indent;

    public TreeScope(int depth) {
        this.lines = new List<string>();
        this.namesToNodes = new Dictionary<TreeNode, string>();
        this.indent = 1;
        this.depth = depth;
        this.arguments = null;
        this.name = "asdfdas";
    }
    public void AddLine(string line) {
        lines.Add(new string('\t', indent) + line);
    }
}