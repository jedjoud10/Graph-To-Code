using System.Collections.Generic;


public class KernelOutput {
    public Utils.StrictType type;
    public TreeNode node;
    public string name;

    public KernelOutput(string name, Utils.StrictType type, TreeNode node) {
        this.type = type;
        this.name = name;
        this.node = node;
    }
}

// One scope per compute shader kernel.
// Multiple scopes are used when we want to execute multiple kernels sequentially
public class KernelScope {
    public List<string> lines;
    public Dictionary<TreeNode, string> namesToNodes;
    public int depth;
    public KernelOutput[] outputs;
    public string name;
    public int indent;

    public KernelScope(int depth) {
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