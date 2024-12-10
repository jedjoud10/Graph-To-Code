using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
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
    public void ExecuteShader(RenderTexture texture, int size) {
        graph = GetComponent<VoxelGraph>();
        if (graph.shader == null) {
            Debug.LogWarning("Shader is not set. You must compile!!");
            return;
        }

        if (texture == null) {
            Debug.LogWarning("Texture is not set!!!");
            return;
        }

        if (graph.injector == null) {
            graph.Transpile();
        }

        //counter++;
        //int3 offset = (int3)IndexToPos(counter % (4*4*4), 4);
        //shader.SetInts("offset", new int[] { offset.x, offset.y, offset.z });
        var shader = graph.shader;
        shader.SetTexture(0, "voxels", texture);
        shader.SetInts("permuationSeed", new int[] { permutationSeed.x, permutationSeed.y, permutationSeed.z });
        shader.SetInts("moduloSeed", new int[] { moduloSeed.x, moduloSeed.y, moduloSeed.z });
        shader.SetVector("offset", transformOffset);
        shader.SetVector("scale", transformScale);
        graph.injector.UpdateInjected(shader);
        shader.Dispatch(0, size/8, size/8, size/8);
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