using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class DensityVisualizer : MonoBehaviour {
    public ComputeShader computeShader;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer commandBuffer;
    private RenderTexture tempVertexTexture;
    public Material customRenderingMaterial;


    public void InitializeForSize() {
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured, 64 * 64 * 64 * 6, sizeof(int));
        vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Structured, 64 * 64 * 64, sizeof(float) * 3);
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandBuffer.SetData(new GraphicsBuffer.IndirectDrawIndexedArgs[1] { new GraphicsBuffer.IndirectDrawIndexedArgs {
            baseVertexIndex = 0,
        } });

        tempVertexTexture = Utils.Create3DRenderTexture(64, GraphicsFormat.R32_UInt, FilterMode.Point, TextureWrapMode.Repeat, false);
    }

    public void ConvertToMeshAndRender(Texture3D density) {
        int size = 64;
        RenderParams renderParams = new RenderParams();
        renderParams.worldBounds = new Bounds {
            center = Vector3.zero,
            extents = Vector3.one * 1000.0f
        };
        renderParams.material = customRenderingMaterial;
        customRenderingMaterial.SetBuffer("_Positions", vertexBuffer);

        computeShader.SetTexture(0, "density", density);
        computeShader.SetBuffer(0, "vertices", vertexBuffer);
        computeShader.SetTexture(0, "vertexIds", tempVertexTexture);
        computeShader.Dispatch(0, size / 8, size / 8, size / 8);

        computeShader.SetTexture(1, "density", density);
        computeShader.SetBuffer(1, "indices", indexBuffer);
        computeShader.SetTexture(1, "vertexIds", tempVertexTexture);
        computeShader.SetBuffer(1, "cmdBuffer", commandBuffer);
        computeShader.Dispatch(1, size / 8, size / 8, size / 8);

        Graphics.RenderPrimitivesIndexedIndirect(renderParams, MeshTopology.Triangles, indexBuffer, commandBuffer);
    }
}
