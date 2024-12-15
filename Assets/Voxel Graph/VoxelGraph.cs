using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// A voxel graph is the base class to inherit from to be able to write custom voxel stuff
public abstract class VoxelGraph : MonoBehaviour {
    [Header("Compilation")]
    public bool debugName = true;

    [Header("Seeding")]
    public int seed = 1234;
    public Vector3Int permutationSeed;
    public Vector3Int moduloSeed;

    private ComputeShader shader;
    private PropertyInjector injector;
    private List<KernelDispatch> computeKernelNameAndDepth;
    private Dictionary<string, TempTexture> tempTextures;
    private Dictionary<string, GradientTexture> gradientTextures;
    private int hash;

    private Dictionary<string, Texture> textures;
    private bool dirtyTexturesRecompilation;

    public int size;


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
        dirtyTexturesRecompilation = true;
        return lines.Aggregate("", (a, b) => a + "\n" + b);
    }

    public void CreateIntermediateTextures() {
        GraphicsFormat ToGfxFormat(Utils.StrictType type) {
            switch (type) {
                case Utils.StrictType.Float:
                    return GraphicsFormat.R32_SFloat;
                case Utils.StrictType.Float2:
                    return GraphicsFormat.R32G32_SFloat;
                case Utils.StrictType.Float3:
                    return GraphicsFormat.R32G32B32_SFloat;
                case Utils.StrictType.Float4:
                    return GraphicsFormat.R32G32B32A32_SFloat;
                default:
                    throw new System.Exception();
            }
        }

        if (textures == null || textures.Count == 0 || (textures["voxels"] != null && textures["voxels"].width != size) || dirtyTexturesRecompilation) {
            dirtyTexturesRecompilation = false;

            if (textures != null) {
                foreach (var (name, tex) in textures) {
                    if (tex is RenderTexture casted) {
                        casted.Release();
                    }
                }
            }

            textures = new Dictionary<string, Texture> {
                { "voxels", Utils.Create3DRenderTexture(size, GraphicsFormat.R32_SFloat, FilterMode.Trilinear, TextureWrapMode.Repeat, false) }
            };

            foreach (var (no, temp) in tempTextures) {
                RenderTexture rt;
                if (temp.threeDimensions) {
                    rt = Utils.Create3DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type), temp.filter, temp.wrap, temp.mips);
                } else {
                    rt = Utils.Create2DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type), temp.filter, temp.wrap, temp.mips);
                }
                textures.Add(temp.name, rt);
            }

            foreach (var (no, temp) in gradientTextures) {
                Texture2D texture = new Texture2D(temp.size, 1, DefaultFormat.LDR, TextureCreationFlags.None);
                texture.wrapMode = TextureWrapMode.Clamp;
                textures.Add(temp.name, texture);
            }
        }
    }

    public void ExecuteShader() {
        if (injector == null || gradientTextures == null || tempTextures == null) {
            Transpile();
        }
        CreateIntermediateTextures();

        shader.SetTexture(0, "voxels", textures["voxels"]);
        shader.SetInt("size", size);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetVector("offset", Vector3.zero);
        shader.SetVector("scale", Vector3.one);
        injector.UpdateInjected(shader, textures);

        foreach (var (no, temp) in gradientTextures) {
            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, temp.name + "_read", textures[temp.name]);
            }
        }

        Dictionary<string, TempTexture> kernelsToWriteTexture = new Dictionary<string, TempTexture>();

        foreach (var (no, temp) in tempTextures) {
            int writeKernelId = shader.FindKernel(temp.writeKernel);
            shader.SetTexture(writeKernelId, temp.name + "_write", textures[temp.name]);
            kernelsToWriteTexture.Add(temp.writeKernel, temp);

            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, temp.name + "_read", textures[temp.name]);
            }
        }

        foreach (var kernel in computeKernelNameAndDepth) {
            int id = shader.FindKernel(kernel.name);
            int tempSize = size / (1 << kernel.sizeReductionPower);

            if (kernel.threeDimensions) {
                shader.Dispatch(id, tempSize / 8, tempSize / 8, tempSize / 8);
            } else {
                shader.Dispatch(id, tempSize / 8, tempSize / 8, 1);
            }

            if (kernelsToWriteTexture.TryGetValue(kernel.name, out var temp)) {
                if (temp.mips) {
                    if (textures[temp.name] is RenderTexture texture) {
                        texture.GenerateMips();
                    }
                }
            }
        }

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
        ExecuteShader();
    }

    public void SoftRecompile() {
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

        ExecuteShader();
        GetComponent<DensityVisualizer>().Exec((RenderTexture)textures["voxels"]);
    }

    // Every time the user updates a field, we will re-transpile (to check for hash-differences) and re-compile if needed
    // Also executing the shader at the specified size as well
    private void OnValidate() {
        SoftRecompile();
    }

    // Transpiles the C# shader code and saves it to a compute shader file
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

    // Recompiles the graph every time we reload the domain
    [InitializeOnLoadMethod]
    static void RecompileOnDomainReload() {
        VoxelGraph[] graph = Object.FindObjectsByType<VoxelGraph>(FindObjectsSortMode.None);

        foreach (var item in graph) {
            item.Compile();
        }
    }
}