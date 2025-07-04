using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using System.Drawing;


#if UNITY_EDITOR
using UnityEditor;
#endif

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
        if (!gameObject.activeSelf)
            return;

        var executor = GetComponent<VoxelGraphExecutor>();
        var visualizer = GetComponent<DensityVisualizer>();

        executor.ExecuteShader(Vector3Int.zero);
        RenderTexture density = (RenderTexture)executor.Textures["voxels"];
        RenderTexture colors = (RenderTexture)executor.Textures["colors"];
        //RenderTexture uvs = (RenderTexture)executor.Textures["uvs"];
        visualizer.Meshify(density, colors);
    }

    // Called when the voxel graph gets recompiled in the editor
    public void OnRecompilation() {
        if (!gameObject.activeSelf)
            return;

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

    public class AllInputs {
        public Variable<float3> position;
    }

    public class AllOutputs {
        public Variable<float> density;
        public Variable<float3> color;
        public Variable<float> metallic;
        public Variable<float> smoothness;
    }

    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Variable<float3> position, out Variable<float> density, out Variable<float3> color);

    // Even lower execution function that allows you to override metallic and smoothness values (and even probably pass your own uv values if needed)
    public virtual void ExecuteWithEverything(AllInputs input, out AllOutputs output) {
        output = new AllOutputs();
        output.metallic = 0.0f;
        output.smoothness = 0.0f;
        Execute(input.position, out output.density, out output.color);
    }

    // Parses the voxel graph into a tree context with all required nodes and everything!!!
    private TreeContext ParsedTranspilation() {
        TreeContext ctx = new TreeContext(debugName);

        // Create the external inputs that we use inside the function scope
        Variable<float3> position = ctx.AliasExternalInput<float3>("position");

        // Input scope arguments
        ctx.position = new ScopeArgument("position", Utils.StrictType.Float3, position, false);
        
        // Execute the voxel graph to get density and color
        AllInputs inputs = new AllInputs() { position = position };
        ExecuteWithEverything(inputs, out AllOutputs outputs);

        var combinedUvs = new ConstructNode<float2>() { inputs = new Variable<float>[2] { outputs.smoothness, outputs.smoothness } };

        // Voxel function output arguments 
        ScopeArgument voxelArgument = new ScopeArgument("voxel", Utils.StrictType.Float, outputs.density, true);
        ScopeArgument colorArgument = new ScopeArgument("color", Utils.StrictType.Float3, outputs.color, true);
        ScopeArgument uvsArgument = new ScopeArgument("uvs", Utils.StrictType.Float2, combinedUvs, true);

        // Voxel function scope
        // We can't initialize the scope again because it contains the shader graph nodes
        ctx.scopes[0].name = "Voxel";
        ctx.scopes[0].arguments = new ScopeArgument[] {
            ctx.position, voxelArgument, colorArgument
        };

        // Voxel kernel dispatcher
        ctx.computeKernelNameAndDepth.Add(new KernelDispatch {
            name = $"CSVoxel",
            depth = 0,
            sizeReductionPower = 0,
            threeDimensions = true,
            scopeName = "Voxel",
            frac = 1.0f,
            scopeIndex = 0,
            numThreads = "[numthreads(8, 8, 8)]",
            remappedCoords = "id.xyz",
            writeCoords = "xyz",
            outputs = new KernelOutput[] {
                new KernelOutput { output = voxelArgument, outputTextureName = "voxels" },
                new KernelOutput { output = colorArgument, outputTextureName = "colors" },
                //new KernelOutput { output = uvsArgument, outputTextureName = "uvs" },
            }
        });

        // Prase the voxel graph going from density and color
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        ctx.Parse(new TreeNode[] { outputs.density, outputs.color });
        timer.Stop();
        //Debug.Log($"{timer.Elapsed.TotalMilliseconds}ms");
        
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
        lines.Add("RWTexture3D<float> voxels_write;");
        lines.Add("RWTexture3D<float3> colors_write;");
        lines.Add("RWTexture3D<float2> uvs_write;");

        lines.Add("int size;");
        lines.Add("int3 permuationSeed;\nint3 moduloSeed;");
        lines.Add("float3 scale;\nfloat3 offset;");

        // imports
        //lines.Add("#include \"Assets/Compute/Noises.cginc\"");
        lines.Add("#include \"Packages/com.jedjoud.graphtocode/Runtime/Compute/Noises.cginc\"");
        lines.Add("#include \"Packages/com.jedjoud.graphtocode/Runtime/Compute/SDF.cginc\"");
        lines.Add("#include \"Packages/com.jedjoud.graphtocode/Runtime/Compute/Other.cginc\"");

        lines.Add(@"
float3 ConvertIntoWorldPosition(float3 tahini) {
    return  (tahini + offset) * scale;
}

float3 ConvertFromWorldPosition(float3 worldPos) {
    return  (worldPos / scale) - offset;
}");

        var temp = ctx.computeKernelNameAndDepth.AsEnumerable().Select(x => x.ConvertToKernelString(ctx)).ToList();

        // Sort the scopes based on their depth
        // We want the scopes that don't require other scopes to be defined at the top, and scopes that require scopes to be defined at the bottom
        ctx.scopes.Sort((TreeScope a, TreeScope b) => { return b.depth.CompareTo(a.depth); });

        // Define each scope as a separate function with its arguments (input / output)
        int index = 0;
        foreach (var scope in ctx.scopes) {
            lines.Add($"// defined nodes: {scope.namesToNodes.Count}, depth: {scope.depth}, index: {index}, total lines: {scope.lines.Count}, argument count: {scope.arguments.Length} ");

            // Create a string containing all the required arguments and stuff
            string arguments = "";
            for (int i = 0; i < scope.arguments.Length; i++) {
                var item = scope.arguments[i];
                var comma = i == scope.arguments.Length - 1 ? "" : ",";
                var output = item.output ? " out " : "";

                arguments += $"{output}{Utils.ToStringType(item.type)} {item.name}{comma}";
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
            index++;
        }

        lines.AddRange(temp);

        return lines.Aggregate("", (a, b) => a + "\n" + b);
    }

    // Checks if we need to recompile the shader by checking the hash changes. Calls a property callback in all cases
    public void SoftRecompile() {
        if (!gameObject.activeSelf)
            return;

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
        if (!gameObject.activeSelf)
            return;

        SoftRecompile();
        OnPropertiesChanged();
    }

    // Transpiles the C# shader code and saves it to a compute shader file
    public void Compile() {
#if UNITY_EDITOR
        string source = Transpile();

        if (!AssetDatabase.IsValidFolder("Assets/Compute/Converted/")) {
            // TODO: Use package cache instead? would it work???
            AssetDatabase.CreateFolder("Assets", "Compute");
            AssetDatabase.CreateFolder("Assets/Compute", "Converted");
            Debug.Log("Creating converted compute shaders folders");
        }

        string filePath = "Assets/Compute/Converted/" + name.ToLower() + ".compute";
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

#if UNITY_EDITOR
    // Recompiles the graph every time we reload the domain
    [InitializeOnLoadMethod]
    static void RecompileOnDomainReload() {
        VoxelGraph[] graph = Object.FindObjectsByType<VoxelGraph>(FindObjectsSortMode.None);

        foreach (var item in graph) {
            //item.Compile();
        }
    }
#endif
}