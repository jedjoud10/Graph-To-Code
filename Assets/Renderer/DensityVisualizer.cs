using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class DensityVisualizer : MonoBehaviour {
    public ComputeShader computeShader;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer normalsBuffer;
    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer atomicCounters;
    private RenderTexture tempVertexTexture;
    public Material customRenderingMaterial;
    private GraphicsBuffer.IndirectDrawIndexedArgs aaa;


    public void InitializeForSize() {
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64 * 64 * 64 * 6, sizeof(int));
        vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64 * 64 * 64, sizeof(float) * 4);
        normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64 * 64 * 64, sizeof(float) * 4);
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        atomicCounters = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, sizeof(uint));

        aaa = new GraphicsBuffer.IndirectDrawIndexedArgs {
            baseVertexIndex = 0,
            instanceCount = 1,
            startIndex = 0,
            startInstance = 0,
            indexCountPerInstance = 0,
        };

        commandBuffer.SetData(new GraphicsBuffer.IndirectDrawIndexedArgs[1] { aaa });

        tempVertexTexture = Utils.Create3DRenderTexture(64, GraphicsFormat.R32_UInt, FilterMode.Point, TextureWrapMode.Repeat, false);
    }

    public void Start() {
        InitializeForSize();
    }

    public void Update() {
        //ConvertToMeshAndRender();
    }

    public void Exec(RenderTexture density) {
        atomicCounters.SetData(new uint[2] { 0, 0 });
        commandBuffer.SetData(new GraphicsBuffer.IndirectDrawIndexedArgs[1] { aaa });

        int size = 64;

        //int id = computeShader.FindKernel("CSVoxel");
        //computeShader.SetTexture(id, "densities", density);
        //computeShader.Dispatch(id, size / 8, size / 8, size / 8);


        int id = computeShader.FindKernel("CSVertex");
        computeShader.SetTexture(id, "densities", density);
        computeShader.SetBuffer(id, "atomicCounters", atomicCounters);
        computeShader.SetBuffer(id, "vertices", vertexBuffer);
        computeShader.SetBuffer(id, "normals", normalsBuffer);
        computeShader.SetBuffer(id, "cmdBuffer", commandBuffer);
        computeShader.SetTexture(id, "vertexIds", tempVertexTexture);
        computeShader.Dispatch(id, size / 8, size / 8, size / 8);
        
        id = computeShader.FindKernel("CSQuad");
        computeShader.SetTexture(id, "densities", density);
        computeShader.SetBuffer(id, "indices", indexBuffer);
        computeShader.SetTexture(id, "vertexIds", tempVertexTexture);
        computeShader.SetBuffer(id, "cmdBuffer", commandBuffer);
        computeShader.SetBuffer(id, "atomicCounters", atomicCounters);
        computeShader.Dispatch(id, size / 8, size / 8, size / 8);
    }

    public void ConvertToMeshAndRender(RenderTexture density) {
        Exec(density);
        RenderParams renderParams = new RenderParams();
        renderParams.worldBounds = new Bounds {
            center = Vector3.zero,
            extents = Vector3.one * 1000.0f,
        };
        renderParams.material = customRenderingMaterial;
        customRenderingMaterial.SetBuffer("vertices", vertexBuffer);
        customRenderingMaterial.SetBuffer("normals", normalsBuffer);

        Graphics.RenderPrimitivesIndexedIndirect(renderParams, MeshTopology.Triangles, indexBuffer, commandBuffer);
    }
}
