using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class VoxelGraphExecutor : MonoBehaviour {
    [Header("Seeding")]
    public int seed = 1234;
    public Vector3Int permutationSeed;
    public Vector3Int moduloSeed;

    [Header("Transform")]
    public Vector3 transformScale;
    public Vector3 transformOffset;
    private VoxelGraph graph;

    public Dictionary<string, RenderTexture> textures;
    public bool dirtyTexturesRecompilation;

    public void CreateIfNull(int size) {
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
                    tex.Release();
                }
            }

            textures = new Dictionary<string, RenderTexture> {
                { "voxels", Utils.Create3DRenderTexture(size, GraphicsFormat.R32_SFloat) }
            };

            foreach (var temp in graph.tempTextures) {
                RenderTexture rt = Utils.Create3DRenderTexture(size / (1 << temp.sizeReductionPower), ToGfxFormat(temp.type));
                textures.Add(temp.readName, rt);
                textures.Add(temp.writeName, rt);
            }
        }
    }

    public void ExecuteShader(int size) {
        graph = GetComponent<VoxelGraph>();

        CreateIfNull(size);

        if (graph.shader == null) {
            Debug.LogWarning("Shader is not set. You must compile!!");
            return;
        }

        if (graph.injector == null) {
            graph.Transpile();
        }

        //counter++;
        //int3 offset = (int3)IndexToPos(counter % (4*4*4), 4);
        //shader.SetInts("offset", new int[] { offset.x, offset.y, offset.z });
        var shader = graph.shader;
        shader.SetTexture(0, "voxels", textures["voxels"]);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetVector("offset", transformOffset);
        shader.SetVector("scale", transformScale);
        graph.injector.UpdateInjected(shader);

        
        foreach (var temp in graph.tempTextures) {
            int writeKernelId = shader.FindKernel(temp.writeKernel);
            shader.SetTexture(writeKernelId, temp.writeName, textures[temp.writeName]);

            foreach (var readKernel in temp.readKernels) {
                int readKernelId = shader.FindKernel(readKernel);
                shader.SetTexture(readKernelId, temp.readName, textures[temp.readName]);
            }
        }

        foreach (var kernel in graph.computeKernelNameAndDepth) {
            int id = shader.FindKernel(kernel.name);
            int tempSize = size / (1 << kernel.sizeReductionPower);
            shader.Dispatch(id, tempSize / 8, tempSize / 8, tempSize / 8);
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