using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
public abstract class VoxelGraph : MonoBehaviour {
    private ComputeShader shader;
    public int seed = 1234;
    public Vector3Int permutationSeed;
    public Vector3Int moduloSeed;
    private TreeContext ctx;

    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Variable<float3> position, out Variable<float> density);

    // This transpile the voxel graph into HLSL code that can be executed on the GPU
    // This can be done outside the editor, but shader compilation MUST be done in editor
    public string Transpile() {
        ctx = new TreeContext(false);
        Variable<float3> position = ctx.Bind<float3>("position");
        Execute(position, out Variable<float> density);
        ctx.Parse(density);
        ctx.Set("density", density);
        IEnumerable<string> parsed2 = ctx.Lines.SelectMany(str => str.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)).Select(x => $"{x}");

        List<string> lines = new List<string>();
        lines.Add("#pragma kernel CSVoxel");
        lines.AddRange(ctx.Properties);
        lines.Add("RWTexture3D<float> voxels;");

        lines.Add("int3 permuationSeed;\nint3 moduloSeed;");

        // imports
        lines.Add("#include \"Assets/Noises.cginc\"");
        lines.Add("#include \"Assets/SDF.cginc\"");

        // function definition
        lines.Add("void Func(float3 position, out float density) {");
        lines.AddRange(parsed2);
        lines.Add("}");

        lines.Add(@"
#pragma kernel CSVoxel
[numthreads(8, 8, 8)]
void CSVoxel(uint3 id : SV_DispatchThreadID) {
    float density = 0.0;
    Func(float3(id) * 0.01, density);
    voxels[id] = density;
}");

        return lines.Aggregate("", (a, b) => a + "\n" + b);
    }

    // Recompiles the graph every time we reload the domain thingy
    [InitializeOnLoadMethod]
    static void Test() {
        VoxelGraph[] graph = Object.FindObjectsOfType<VoxelGraph>();

        foreach (var item in graph) {
            //item.currentHash = 0;
            item.Compile();
        }
    }

    private void OnValidate() {
        var temp = GetComponent<VoxelGraphPreview>();
        if (temp != null) {
            temp.OnValidate();
        }

        ComputeSecondarySeeds();
    }

    public void ExecuteShader(RenderTexture texture, int size) {
        if (shader == null) {
            Debug.LogWarning("Shader is not set. You must compile!!");
            return;
        }

        if (texture == null) {
            Debug.LogWarning("Texture is not set!!!");
            return;
        }

        if (ctx == null) {
            Transpile();
        }

        //counter++;
        //int3 offset = (int3)IndexToPos(counter % (4*4*4), 4);
        //shader.SetInts("offset", new int[] { offset.x, offset.y, offset.z });
        shader.SetTexture(0, "voxels", texture);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.Dispatch(0, size/8, size/8, size/8);
        ctx.UpdateInjected(shader);
    }

    private void ComputeSecondarySeeds() {
        var random = new System.Random(seed);
        permutationSeed.x = random.Next(-1000, 1000);
        permutationSeed.y = random.Next(-1000, 1000);
        permutationSeed.z = random.Next(-1000, 1000);
        moduloSeed.x = random.Next(-1000, 1000);
        moduloSeed.y = random.Next(-1000, 1000);
        moduloSeed.z = random.Next(-1000, 1000);
    }

    public void RandomizeSeed() {
        seed = UnityEngine.Random.Range(-9999, 9999);
        ComputeSecondarySeeds();
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