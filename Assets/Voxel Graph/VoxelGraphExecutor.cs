using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class VoxelGraphExecutor : MonoBehaviour {
    [Header("Seeding")]
    public int seed = 1234;
    public Vector3Int permutationSeed;
    public Vector3Int moduloSeed;

    [Header("Transform")]
    public Vector3 transformScale;
    public Vector3 transformOffset;
    private VoxelGraph graph;

    private Dictionary<string, Texture> textures;
    private bool dirtyTexturesRecompilation;

    public RenderTexture VoxelTexture {
        get {
            return (RenderTexture)textures["voxels"];
        }
    }

    public void SetDirty() {
        dirtyTexturesRecompilation = true;
    }

    public void CreateIntermediateTextures(int size) {
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

            foreach (var (no, temp) in graph.tempTextures) {
                RenderTexture rt;
                if (temp.threeDimensions) {
                     rt = Utils.Create3DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type), temp.filter, temp.wrap, temp.mips);
                } else {
                    rt = Utils.Create2DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type), temp.filter, temp.wrap, temp.mips);
                }
                textures.Add(temp.name, rt);
            }

            foreach (var (no, temp) in graph.gradientTextures) {
                Texture2D texture = new Texture2D(temp.size, 1, DefaultFormat.LDR, TextureCreationFlags.None);
                texture.wrapMode = TextureWrapMode.Clamp;
                textures.Add(temp.name, texture);
            }
        }
    }

    public void ExecuteShader(int size) {
        graph = GetComponent<VoxelGraph>();

        CreateIntermediateTextures(size);

        if (graph.shader == null) {
            Debug.LogWarning("Shader is not set. You must compile!!");
            return;
        }

        if (graph.injector == null) {
            graph.Transpile();
        }

        var shader = graph.shader;
        shader.SetTexture(0, "voxels", textures["voxels"]);
        shader.SetInt("size", size);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetVector("offset", transformOffset);
        shader.SetVector("scale", transformScale);
        graph.injector.UpdateInjected(shader, textures);

        foreach (var (no, temp) in graph.gradientTextures) {
            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, temp.name + "_read", textures[temp.name]);
            }
        }

        Dictionary<string, TempTexture> kernelsToWriteTexture = new Dictionary<string, TempTexture>();

        foreach (var (no, temp) in graph.tempTextures) {
            int writeKernelId = shader.FindKernel(temp.writeKernel);
            shader.SetTexture(writeKernelId, temp.name + "_write", textures[temp.name]);
            kernelsToWriteTexture.Add(temp.writeKernel, temp);

            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, temp.name + "_read", textures[temp.name]);
            }
        }

        foreach (var kernel in graph.computeKernelNameAndDepth) {
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
    }
}