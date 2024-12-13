using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
public abstract class VoxelGraph : MonoBehaviour {
    [HideInInspector]
    public ComputeShader shader;
    [HideInInspector]
    public PropertyInjector injector;
    private int hash;

    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Variable<float3> position, out Variable<float> density);

    // This transpile the voxel graph into HLSL code that can be executed on the GPU
    // This can be done outside the editor, but shader compilation MUST be done in editor
    public string Transpile() {
        TreeContext ctx = new TreeContext(true);
        
        Variable<float3> position = ctx.AliasExternalInput<float3>("position");
        ctx.start = position;
        Execute(position, out Variable<float> density);
        ctx.scopes[0].output = (Utils.StrictType.Float,  density);
        ctx.scopes[0].name = "Test";
        //(var symbols, var hash) = ctx.Handlinate(density2);

        ctx.Parse(density);

        List<string> lines = new List<string>();
        lines.Add("#pragma kernel CSVoxel");
        lines.AddRange(ctx.Properties);
        lines.Add("RWTexture3D<float> voxels;");

        lines.Add("int3 permuationSeed;\nint3 moduloSeed;");
        lines.Add("float3 scale;\nfloat3 offset;");

        // imports
        lines.Add("#include \"Assets/Noises.cginc\"");
        lines.Add("#include \"Assets/SDF.cginc\"");

        ctx.scopes.Sort((TreeContext.KernelScope a, TreeContext.KernelScope b) => { return b.depth.CompareTo(a.depth); });
        foreach (var scope in ctx.scopes) {
            Debug.Log(scope.depth);
            lines.Add($"// defined nodes: {scope.namesToNodes.Count}, depth: {scope.depth}, total lines: {scope.lines.Count} ");
            lines.Add($"{scope.output.Item1.ToStringType()} {scope.name}(float3 position) {{");
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
    voxels[id] = Test((float3(id) + offset) * scale);
}");


        injector = ctx.injector;
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
        ctx.scopes[0].name = "Test";
        ctx.Parse(density);

        if (hash != ctx.hash) {
            hash = ctx.hash;
            Debug.Log("Hash changed, recompiling...");
            Compile();
        }
    }

    public void Compile() {
#if UNITY_EDITOR
        Debug.Log("Compiling...");
        string source = Transpile();
        string folder = "Converted";

        if (!AssetDatabase.IsValidFolder("Assets/" + folder)) {
            string guid = AssetDatabase.CreateFolder("Assets", folder);
        }

        string filePath = "Assets/" + folder + "/" + name.ToLower() + ".voxel";
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