using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
public abstract class VoxelGraph : MonoBehaviour {
    [HideInInspector]
    public ComputeShader shader;
    [HideInInspector]
    public PropertyInjector injector;
    public List<KernelDispatch> computeKernelNameAndDepth;
    public Dictionary<string, TempTexture> tempTextures;
    public Dictionary<string, GradientTexture> gradientTextures;
    private int hash;
    public bool debugName = true;

    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Variable<float3> position, out Variable<float> density);

    // Hashes extra parameters that could be used for recompilation
    public virtual void Hashinate(Hashinator hashinator) { }

    // This transpile the voxel graph into HLSL code that can be executed on the GPU
    // This can be done outside the editor, but shader compilation MUST be done in editor
    public string Transpile() {
        TreeContext ctx = new TreeContext(debugName);
        Hashinate(ctx.hashinator);
        
        Variable<float3> position = ctx.AliasExternalInput<float3>("position");
        ctx.start = position;
        Execute(position, out Variable<float> density);
        ctx.scopes[0].output = (Utils.StrictType.Float,  density);
        ctx.scopes[0].name = "Voxel";
        //(var symbols, var hash) = ctx.Handlinate(density2);

        ctx.Parse(density);

        List<string> lines = new List<string>();
        lines.AddRange(ctx.Properties);
        lines.Add("RWTexture3D<float> voxels;");

        lines.Add("int size;");
        lines.Add("int3 permuationSeed;\nint3 moduloSeed;");
        lines.Add("float3 scale;\nfloat3 offset;");


        // imports
        lines.Add("#include \"Assets/Compute/Noises.cginc\"");
        lines.Add("#include \"Assets/Compute/Other.cginc\"");
        //lines.Add("#include \"Assets/SDF.cginc\"");

        ctx.scopes.Sort((KernelScope a, KernelScope b) => { return b.depth.CompareTo(a.depth); });
        foreach (var scope in ctx.scopes) {
            Debug.Log(scope.depth);
            lines.Add($"// defined nodes: {scope.namesToNodes.Count}, depth: {scope.depth}, total lines: {scope.lines.Count} ");
            lines.Add($"{scope.output.Item1.ToStringType()} {scope.name}(float3 position, uint3 id) {{");
            scope.AddLine($"return {scope.namesToNodes[scope.output.Item2]};");
            IEnumerable<string> parsed2 = scope.lines.SelectMany(str => str.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)).Select(x => $"{x}");
            lines.AddRange(parsed2);
            lines.Add("}\n");
        }

        




        // function definition
        
        lines.Add(@"
#pragma kernel CSVoxel
[numthreads(8, 8, 8)]
void CSVoxel(uint3 id : SV_DispatchThreadID) {
    voxels[id] = Voxel((float3(id) + offset) * scale, id);
}"
);
        // TODO: Convert all of the default voxel stuff to use the stuff we've defined (aka remove the shit stuff from above kekw)
        ctx.computeKernelNameAndDepth.Add(new KernelDispatch {
            name = $"CSVoxel",
            depth = 0,
            sizeReductionPower = 0,
            threeDimensions = true,
        });

        lines.AddRange(ctx.computeKernels);

        injector = ctx.injector;
        ctx.computeKernelNameAndDepth.Sort((KernelDispatch a, KernelDispatch b) => { return b.depth.CompareTo(a.depth); });
        
        computeKernelNameAndDepth = ctx.computeKernelNameAndDepth;
        tempTextures = ctx.tempTextures;
        gradientTextures = ctx.gradientTextures;

        GetComponent<VoxelGraphExecutor>().SetDirty();
        return lines.Aggregate("", (a, b) => a + "\n" + b);
    }

    // Recompiles the graph every time we reload the domain thingy
    [InitializeOnLoadMethod]
    static void Test() {
        VoxelGraph[] graph = Object.FindObjectsOfType<VoxelGraph>();

        foreach (var item in graph) {
            item.Compile();
        }
    }

    private void OnValidate() {
        TreeContext ctx = new TreeContext(false);
        Variable<float3> position = ctx.AliasExternalInput<float3>("position");
        ctx.start = position;
        Execute(position, out Variable<float> density);
        ctx.scopes[0].output = (Utils.StrictType.Float, density);
        ctx.scopes[0].name = "Voxel";
        ctx.Parse(density);

        if (hash != ctx.hashinator.hash) {
            hash = ctx.hashinator.hash;
            Debug.Log("Hash changed, recompiling...");
            Compile();
        }
    }

    public void Compile() {
#if UNITY_EDITOR
        Debug.Log("Compiling...");
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

        Debug.Log("Asset database refresh");
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(filePath);

        ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(filePath);
        this.shader = shader;
#else
            Debug.LogError("Cannot transpile code at runtime");
#endif
    }
}