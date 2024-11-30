using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
public abstract class VoxelGraph : MonoBehaviour {
    public ComputeShader shader;
    public RenderTexture texture;
    private ShaderManager manager;
    public bool preview;
    public Gradient previewGradient;
    public float previewOpacity;
    public int previewQuality;
    public Vector3Int moduloSeed;
    public Vector3Int permutationSeed;
    private int currentHash;

    // Execute the voxel graph at a specific position and fetch the density and material values
    public abstract void Execute(Var<float3> position, out Var<float> density, out Var<uint> material);

    // This transpile the voxel graph into HLSL code that can be executed on the GPU
    // This can be done outside the editor, but shader compilation MUST be done in editor
    public string Transpile(bool hashOnly = false) {
        manager = new ShaderManager(hashOnly);
        ShaderManager.singleton = manager;

        Var<float3> position = new Var<float3> {
            name = "position"
        };

        Var<float> density = new Var<float> {
            name = "density"
        };

        Var<uint> material = new Var<uint> {
            name = "material"
        };

        Execute(position, out Var<float> _density, out Var<uint> _material);

        ShaderManager.singleton.SetVariable("density", _density.name);
        ShaderManager.singleton.SetVariable("material", _material.name);

        ShaderManager.singleton = null;

        if (hashOnly) {
            return "";
        }

        List<string> strings = new List<string>();

        strings.Add("#pragma kernel CSVoxel");
        strings.AddRange(manager.properties);
        strings.Add("RWTexture3D<float> voxels;");

        strings.Add("int3 permuationSeed;\nint3 moduloSeed;");
        
        strings.Add("#include \"Assets/Noises.cginc\"");

        strings.Add("void Func(float3 position, out float density, out uint material) {");
        strings.AddRange(manager.lines.SelectMany(str => str.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)).Select(x => $"\t{x}"));
        strings.Add("}");

        strings.Add(@"
int3 offset;
[numthreads(8, 8, 8)]
void CSVoxel(uint3 id : SV_DispatchThreadID) {
    float3 position = float3(id + offset * 32);
    float density = 0.0;
    uint material = 0;
    Func(position * 0.01, density, material);
    voxels[id + offset * 32] = density;
}
        ");

        this.currentHash = manager.hash;
        return strings.Aggregate("", (a, b) => a + "\n" + b);
    }

    public void OnValidate() {
        int oldHash = currentHash;
        Transpile();

        if (oldHash != currentHash) {
            Compile();
        }

        ExecuteShader();
    }

    int counter;

    public static uint3 IndexToPos(int index, uint size) {
        uint index2 = (uint)index;

        // N(ABC) -> N(A) x N(BC)
        uint y = index2 / (size * size);   // x in N(A)
        uint w = index2 % (size * size);  // w in N(BC)

        // N(BC) -> N(B) x N(C)
        uint z = w / size;        // y in N(B)
        uint x = w % size;        // z in N(C)
        return new uint3(x, y, z);
    }

    public void ExecuteShader() {
        Transpile();
        counter++;

        int3 offset = (int3)IndexToPos(counter % (4*4*4), 4);


        if (texture == null)
            texture = Create3DRenderTexture(128, GraphicsFormat.R32_SFloat);
        
        shader.SetTexture(0, "voxels", texture);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetInts("offset", new int[] { offset.x, offset.y, offset.z });
        shader.Dispatch(0, 4, 4, 4);
        manager.UpdateInjected(shader);
    }

    public static RenderTexture Create3DRenderTexture(int size, GraphicsFormat format) {
        RenderTexture texture = new RenderTexture(size, size, 0, format);
        texture.height = size;
        texture.width = size;
        texture.depth = 0;
        texture.volumeDepth = size;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        texture.enableRandomWrite = true;
        texture.Create();
        return texture;
    }


    [InitializeOnLoadMethod]
    static void Test() {
        VoxelGraph[] graph = Object.FindObjectsOfType<VoxelGraph>();

        foreach (var item in graph) {
            item.currentHash = 0;
            item.Compile();
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

        string filePath = "Assets/" + folder + "/" + name.ToLower() + ".baka";
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