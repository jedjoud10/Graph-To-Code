using System.Collections.Generic;


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