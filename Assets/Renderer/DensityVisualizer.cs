using Unity.Mathematics;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class DensityVisualizer : MonoBehaviour {
    public ComputeShader computeShader;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer normalsBuffer;
    private GraphicsBuffer colorsBuffer;
    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer atomicCounters;
    private RenderTexture tempVertexTexture;
    public Material customRenderingMaterial;
    private GraphicsBuffer.IndirectDrawIndexedArgs aaa;
    public bool blocky;

    public void InitializeForSize() {
        int size = GetComponent<VoxelGraphExecutor>().size;
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size * 6, sizeof(int));
        vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size, sizeof(float) * 3);
        normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size, sizeof(float) * 3);
        colorsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size, sizeof(float) * 3);
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

        tempVertexTexture = Utils.Create3DRenderTexture(size, GraphicsFormat.R32_UInt, FilterMode.Point, TextureWrapMode.Repeat, false);
    }

    private void OnDisable() {
        Killnate();
    }

    private void OnEnable() {
        GetComponent<VoxelGraph>().onRecompilation += () => {
            InitializeForSize();
        };

        GetComponent<VoxelGraph>().onPropertiesChanged += () => {
            RenderTexture rt = (RenderTexture)GetComponent<VoxelGraphExecutor>().Textures["voxels"];
            Exec(rt);
        };
    }

    public void Killnate() {
        indexBuffer.Dispose();
        vertexBuffer.Dispose();
        normalsBuffer.Dispose();
        commandBuffer.Dispose();    
        atomicCounters.Dispose();
        colorsBuffer.Dispose();
        tempVertexTexture.Release();
    }

    public void Exec(RenderTexture density) {
        int size = GetComponent<VoxelGraphExecutor>().size;
        atomicCounters.SetData(new uint[2] { 0, 0 });
        commandBuffer.SetData(new GraphicsBuffer.IndirectDrawIndexedArgs[1] { aaa });

        computeShader.SetBool("blocky", blocky);
        computeShader.SetInt("size", size);

        int id = computeShader.FindKernel("CSVertex");
        computeShader.SetTexture(id, "densities", density);
        computeShader.SetBuffer(id, "atomicCounters", atomicCounters);
        computeShader.SetBuffer(id, "vertices", vertexBuffer);
        computeShader.SetBuffer(id, "normals", normalsBuffer);
        computeShader.SetBuffer(id, "colors", colorsBuffer);
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

    public void Update() {
        ConvertToMeshAndRender();
    }

    public void ConvertToMeshAndRender() {
        if (indexBuffer == null || commandBuffer == null)
            return;

        RenderParams renderParams = new RenderParams();
        renderParams.worldBounds = new Bounds {
            center = Vector3.zero,
            extents = Vector3.one * 1000.0f,
        };
        //renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        renderParams.material = customRenderingMaterial;

        var mat = new MaterialPropertyBlock();
        mat.SetBuffer("_Vertices", vertexBuffer);
        mat.SetBuffer("_Normals", normalsBuffer);
        mat.SetBuffer("_Colors", colorsBuffer);
        renderParams.matProps = mat;

        Graphics.RenderPrimitivesIndexedIndirect(renderParams, MeshTopology.Triangles, indexBuffer, commandBuffer);
    }
}
