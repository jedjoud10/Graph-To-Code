using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class VoxelGraphExecutor : MonoBehaviour {
    [Header("Seeding")]
    public int seed = 1234;
    public Vector3Int permutationSeed;
    public Vector3Int moduloSeed;

    public List<Texture> debugTextures;
    public Dictionary<string, ExecutorTexture> Textures { get; private set; }

    public int size;

    private VoxelGraph graph;

    private void OnEnable() {
        graph = GetComponent<VoxelGraph>();
    }

    private void OnValidate() {
        size = Mathf.ClosestPowerOfTwo(size);
        graph.OnPropertiesChanged();
        ComputeSecondarySeeds();
    }



    // Create intermediate textures (cached, gradient) to be used for voxel graph shader execution
    // Texture size will correspond to execution size property
    public void CreateIntermediateTextures() {
        graph = GetComponent<VoxelGraph>();
        // Dispose of previous render textures if needed
        if (Textures != null) {
            foreach (var (name, tex) in Textures) {
                if (tex.texture is RenderTexture casted) {
                    casted.Release();
                }
            }
        }

        // Creates dictionary with the default voxel graph textures (density + custom data)
        Textures = new Dictionary<string, ExecutorTexture> {
            { "voxels", new OutputExecutorTexture("voxels", new List<string>() { "CSVoxel" }, Utils.Create3DRenderTexture(size, GraphicsFormat.R32_SFloat)) },
            { "colors", new OutputExecutorTexture("colors", new List < string >() { "CSVoxel" }, Utils.Create3DRenderTexture(size, GraphicsFormat.R32G32B32A32_SFloat)) },
        };

        foreach (var (name, descriptor) in graph.textureDescriptors) {
            Textures.Add(name, descriptor.Create(size));
        }

        debugTextures = Textures.Values.AsEnumerable().Select(x => x.texture).ToList();
    }

    public void ExecuteShader(Vector3Int chunkOffset) {
        graph = GetComponent<VoxelGraph>();
        graph.PrepareForExecution();

        ComputeShader shader = graph.shader;
        shader.SetInt("size", size);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetVector("offset", (Vector3)chunkOffset * size);
        shader.SetVector("scale", Vector3.one);



        graph.injector.UpdateInjected(shader, Textures);

        foreach (var (name, texture) in Textures) {
            texture.BindToComputeShader(shader);
        }

        // Execute the kernels sequentially
        foreach (var kernel in graph.sortedKernelDispatches) {
            int id = shader.FindKernel(kernel.name);
            int tempSize = size / (1 << kernel.sizeReductionPower);
            tempSize = Mathf.Max(tempSize, 1);

            int minScaleBase3D = Mathf.CeilToInt((float)tempSize / 8.0f);
            int minScaleBase2D = Mathf.CeilToInt((float)tempSize / 32.0f);

            if (kernel.threeDimensions) {
                shader.Dispatch(id, minScaleBase3D, minScaleBase3D, minScaleBase3D);
            } else {
                shader.Dispatch(id, minScaleBase2D, minScaleBase2D, 1);
            }

            // TODO: Dictionary<string, string> kernelsToWriteTexture = new Dictionary<string, string>();
            foreach (var (name, item) in Textures) {
                item.PostDispatchKernel(shader, id);
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
        graph.OnPropertiesChanged();
    }
}
