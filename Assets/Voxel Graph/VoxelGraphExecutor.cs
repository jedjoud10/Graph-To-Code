using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class VoxelGraphExecutor : MonoBehaviour {

    [Header("Seeding")]
    public int seed = 1234;
    public Vector3Int permutationSeed;
    public Vector3Int moduloSeed;

    public Dictionary<string, Texture> Textures { get; private set; }
    public RenderTexture temp;

    public int size;

    private VoxelGraph graph;

    private void OnEnable() {
        graph = GetComponent<VoxelGraph>();

        graph.onPropertiesChanged += () => { ExecuteShader(); };
        graph.onRecompilation += () => { CreateIntermediateTextures(); };
    }

    // Convert a strict type to a graphics format to be used for texture format
    private GraphicsFormat ToGfxFormat(Utils.StrictType type) {
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

    // Create intermediate textures (cached, gradient) to be used for voxel graph shader execution
    // Texture size will correspond to execution size property
    public void CreateIntermediateTextures() {
        if (Textures == null || Textures.Count == 0 || (Textures["voxels"] != null && Textures["voxels"].width != size)) {
            // Dispose of previous render textures if needed
            if (Textures != null) {
                foreach (var (name, tex) in Textures) {
                    if (tex is RenderTexture casted) {
                        casted.Release();
                    }
                }
            }

            // Creates dictionary with the default voxel graph textures (density + custom data)
            Textures = new Dictionary<string, Texture> {
                { "voxels", Utils.Create3DRenderTexture(size, GraphicsFormat.R32_SFloat, FilterMode.Trilinear, TextureWrapMode.Repeat, false) }
            };

            foreach (var (name, temp) in graph.tempTextures) {
                RenderTexture rt;
                if (temp.threeDimensions) {
                    rt = Utils.Create3DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type), temp.filter, temp.wrap, temp.mips);
                } else {
                    rt = Utils.Create2DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type), temp.filter, temp.wrap, temp.mips);
                }
                Textures.Add(name, rt);
            }

            foreach (var (name, temp) in graph.gradientTextures) {
                Texture2D texture = new Texture2D(temp.size, 1, DefaultFormat.LDR, TextureCreationFlags.None);
                texture.wrapMode = TextureWrapMode.Clamp;
                Textures.Add(name, texture);
            }
        }

        temp = (RenderTexture)Textures["voxels"];
    }

    public void ExecuteShader() {
        CreateIntermediateTextures();

        ComputeShader shader = graph.shader;
        shader.SetTexture(0, "voxels", Textures["voxels"]);
        shader.SetInt("size", size);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetVector("offset", Vector3.zero);
        shader.SetVector("scale", Vector3.one);
        graph.injector.UpdateInjected(shader, Textures);

        // Bind the gradient textures to their respective read kernels
        foreach (var (name, temp) in graph.gradientTextures) {
            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, name + "_read", Textures[name]);
            }
        }


        // Bind the required temp textures to their respective read/write kernels
        Dictionary<string, string> kernelsToWriteTexture = new Dictionary<string, string>();
        foreach (var (name, temp) in graph.tempTextures) {
            // Set texture as write only in the write kernel
            int writeKernelId = shader.FindKernel(temp.writeKernel);
            shader.SetTexture(writeKernelId, name + "_write", Textures[name]);
            kernelsToWriteTexture.Add(temp.writeKernel, name);

            // Set texture as read only in possibly multiple read kernels
            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, name + "_read", Textures[name]);
            }
        }

        // Execute the kernels sequentially
        foreach (var kernel in graph.sortedKernelDispatches) {
            int id = shader.FindKernel(kernel.name);
            int tempSize = size / (1 << kernel.sizeReductionPower);

            if (kernel.threeDimensions) {
                shader.Dispatch(id, tempSize / 8, tempSize / 8, tempSize / 8);
            } else {
                shader.Dispatch(id, tempSize / 32, tempSize / 32, 1);
            }

            if (kernelsToWriteTexture.TryGetValue(kernel.name, out var name)) {
                if (graph.tempTextures[name].mips && Textures[name] is RenderTexture texture) {
                    texture.GenerateMips();
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
}
