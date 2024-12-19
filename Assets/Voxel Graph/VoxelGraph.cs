using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
public abstract class VoxelGraph : MonoBehaviour {
    [Header("Compilation")]
    public bool debugName = true;

    public ComputeShader shader;
    public PropertyInjector injector;
    public List<KernelDispatch> sortedKernelDispatches;
    public Dictionary<string, TextureDescriptor> textureDescriptors;
    private int hash;

    // Called when the voxel graph's properties get modified
    public void OnPropertiesChanged() {
        var executor = GetComponent<VoxelGraphExecutor>();
        var visualizer = GetComponent<DensityVisualizer>();

        executor.ExecuteShader(Vector3Int.zero);
        RenderTexture density = (RenderTexture)executor.Textures["voxels"];
        RenderTexture colors = (RenderTexture)executor.Textures["colors"];
        visualizer.Meshify(density, colors);
    }

    // Called when the voxel graph gets recompiled in the editor
    public void OnRecompilation() {
        var executor = GetComponent<VoxelGraphExecutor>();
        var visualizer = GetComponent<DensityVisualizer>();

        visualizer.InitializeForSize(executor.size);
        executor.CreateIntermediateTextures();
    }

    public void PrepareForExecution() {
        if (injector == null) {
            ParsedTranspilation();
        }

        var executor = GetComponent<VoxelGraphExecutor>();

        if (executor.Textures == null) {
            executor.CreateIntermediateTextures();
        }
    }

    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Variable<float3> position, Variable<uint3> id, out Variable<float> density, ref Variable<float3> color);

    // Parses the voxel graph into a tree context with all required nodes and everything!!!
    private TreeContext ParsedTranspilation() {
        TreeContext ctx = new TreeContext(debugName);
        Variable<float3> position = ctx.AliasExternalInput<float3>("position");
        Variable<uint3> id = ctx.AliasExternalInput<uint3>("id");
        ctx.startPosition = position;
        ctx.startId = id;
        
        Variable<float3> color = float3.zero;
        Execute(position, id, out Variable<float> density, ref color);

        ctx.scopes[0].arguments = new ScopeArgument[] {
            new ScopeArgument("position", Utils.StrictType.Float3, position, false),
            new ScopeArgument("id", Utils.StrictType.Uint3, id, false),
            new ScopeArgument("voxel", Utils.StrictType.Float, density, true),
            new ScopeArgument("color", Utils.StrictType.Float3, color, true),
        };
        ctx.scopes[0].name = "Voxel";
        //ctx.scopes[0].namesToNodes.Add(position, "position");
        //ctx.scopes[0].namesToNodes.Add(id, "id");

        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        ctx.Parse(new TreeNode[] { density, color });
        timer.Stop();
        //Debug.Log($"{timer.Elapsed.TotalMilliseconds}ms");

        // TODO: Convert all of the default voxel stuff to use the stuff we've defined (aka remove the shit stuff from above kekw)
        ctx.computeKernelNameAndDepth.Add(new KernelDispatch {
            name = $"CSVoxel",
            depth = 0,
            sizeReductionPower = 0,
            threeDimensions = true,
        });

        injector = ctx.injector;
        ctx.computeKernelNameAndDepth.Sort((KernelDispatch a, KernelDispatch b) => { return b.depth.CompareTo(a.depth); });
        sortedKernelDispatches = ctx.computeKernelNameAndDepth;
        textureDescriptors = ctx.textures;

        return ctx;
    }

    // This transpile the voxel graph into HLSL code that can be executed on the GPU
    // This can be done outside the editor, but shader compilation MUST be done in editor
    public string Transpile() {
        TreeContext ctx = ParsedTranspilation();

        List<string> lines = new List<string>();
        lines.AddRange(ctx.Properties);
        lines.Add("RWTexture3D<float> voxels;");
        lines.Add("RWTexture3D<float3> colors;");

        lines.Add("int size;");
        lines.Add("int3 permuationSeed;\nint3 moduloSeed;");
        lines.Add("float3 scale;\nfloat3 offset;");

        // imports
        lines.Add("#include \"Assets/Compute/Noises.cginc\"");
        lines.Add("#include \"Assets/Compute/Other.cginc\"");
        lines.Add("#include \"Assets/Compute/SDF.cginc\"");

        // Sort the scopes based on their depth
        // We want the scopes that don't require other scopes to be defined at the top, and scopes that require scopes to be defined at the bottom
        ctx.scopes.Sort((TreeScope a, TreeScope b) => { return b.depth.CompareTo(a.depth); });

        // Define each scope as a separate function with its arguments (input / output)
        foreach (var scope in ctx.scopes) {
            lines.Add($"// defined nodes: {scope.namesToNodes.Count}, depth: {scope.depth}, total lines: {scope.lines.Count}, argument count: {scope.arguments.Length} ");

            string arguments = "";

            for (int i = 0; i < scope.arguments.Length; i++) {
                var item = scope.arguments[i];
                var comma = i == scope.arguments.Length - 1 ? "" : ",";
                var output = item.output ? " out" : "";

                arguments += $"{output} {Utils.ToStringType(item.type)} {item.name}{comma}";
            }

            // Open scope
            lines.Add($"void {scope.name}({arguments}) {{");

            // Set the output arguments inside of the scope
            foreach (var item in scope.arguments) {
                if (item.output) {
                    scope.AddLine($"{item.name} = {scope.namesToNodes[item.node]};");
                }
            }

            // Add the lines of the scope to the main shader lines
            IEnumerable<string> parsed2 = scope.lines.SelectMany(str => str.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)).Select(x => $"{x}");
            lines.AddRange(parsed2);

            // Close scope
            lines.Add("}\n");
        }

        // function definition
        
        lines.Add(@"
#pragma kernel CSVoxel
[numthreads(8, 8, 8)]
void CSVoxel(uint3 id : SV_DispatchThreadID) {
    float density;
    float3 color;
    Voxel((float3(id) + offset) * scale, id, density, color);
    voxels[id] = density;
    colors[id] = color;
}"
);

        lines.AddRange(ctx.computeKernels);

        return lines.Aggregate("", (a, b) => a + "\n" + b);
    }

    // Checks if we need to recompile the shader by checking the hash changes. Calls a property callback in all cases
    public void SoftRecompile() {
        TreeContext ctx = ParsedTranspilation();
        if (hash != ctx.hashinator.hash) {
            hash = ctx.hashinator.hash;
            Debug.Log("Hash changed, recompiling...");
            Compile();
        }
    }

    // Every time the user updates a field, we will re-transpile (to check for hash-differences) and re-compile if needed
    // Also executing the shader at the specified size as well
    private void OnValidate() {
        SoftRecompile();
        OnPropertiesChanged();
    }

    // Transpiles the C# shader code and saves it to a compute shader file
    public void Compile() {
#if UNITY_EDITOR
        string source = Transpile();
        string folder = "Converted";
        string root = "Assets/Compute";

        if (!AssetDatabase.IsValidFolder(root + "/" + folder + "/")) {
            Debug.Log("Creating converted compute shaders folders");
            string guid = AssetDatabase.CreateFolder(root, folder);
        }

        string filePath = root + "/" + folder + "/" + name.ToLower() + ".compute";
        using (StreamWriter sw = File.CreateText(filePath)) {
            sw.Write(source);
        }

        AssetDatabase.ImportAsset(filePath);
        shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(filePath);
        OnRecompilation();
        OnPropertiesChanged();
#else
            Debug.LogError("Cannot transpile code at runtime");
#endif
    }

    // Recompiles the graph every time we reload the domain
    [InitializeOnLoadMethod]
    static void RecompileOnDomainReload() {
        VoxelGraph[] graph = Object.FindObjectsByType<VoxelGraph>(FindObjectsSortMode.None);

        foreach (var item in graph) {
            item.Compile();
        }
    }
}