using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class DensityVisualizer : MonoBehaviour {
    public ComputeShader surfaceNetsCompute;
    public ComputeShader heightMapCompute;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer uvsBuffer;
    private GraphicsBuffer normalsBuffer;
    private GraphicsBuffer colorsBuffer;
    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer atomicCounters;
    private RenderTexture tempVertexTexture;
    private RenderTexture maxHeightAtomic;
    public Material customRenderingMaterial;
    private GraphicsBuffer.IndirectDrawIndexedArgs aaa;
    public RenderTexture testTexture;
    public bool blocky;
    public bool useHeightSimplification;
    public bool flatshaded;
    private int size;
    public int testTextureSize;

    public void InitializeForSize(int newSize) {
        if (!isActiveAndEnabled)
            return;

        if (indexBuffer != null && indexBuffer.IsValid()) {
            DisposeBuffers();
        }

        if (indexBuffer != null && newSize == size && indexBuffer.IsValid())
            return;

        size = newSize;
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size * 6, sizeof(int));
        uvsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size, sizeof(float) * 2);
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
        maxHeightAtomic = Utils.Create2DRenderTexture(size, GraphicsFormat.R32_UInt, FilterMode.Point, TextureWrapMode.Repeat, false);
        testTexture = Utils.Create2DRenderTexture(testTextureSize, GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Point, TextureWrapMode.Repeat, false);
    }

    private void OnDisable() {
        DisposeBuffers();
    }

    private void OnValidate() {
        GetComponent<VoxelGraph>().OnPropertiesChanged();
    }


    public void DisposeBuffers() {
        indexBuffer.Dispose();
        vertexBuffer.Dispose();
        normalsBuffer.Dispose();
        commandBuffer.Dispose();    
        atomicCounters.Dispose();
        colorsBuffer.Dispose();
        tempVertexTexture.Release();
        maxHeightAtomic.Release();
        testTexture.Release();
    }

    public void Meshify(RenderTexture voxels, RenderTexture colors) {
        if (useHeightSimplification) {
            // TODO: For some reason does not work on dx12
            // wut???????? why????????
            ExecuteHeightMapMesher(voxels, colors, -1, Vector3Int.zero);
        } else {
            ExecuteSurfaceNetsMesher(voxels, colors);
        }

    }

    public void ExecuteSurfaceNetsMesher(RenderTexture voxels, RenderTexture colors) {
        if (atomicCounters == null || !atomicCounters.IsValid())
            return;

        int size = voxels.width;
        atomicCounters.SetData(new uint[2] { 0, 0 });
        commandBuffer.SetData(new GraphicsBuffer.IndirectDrawIndexedArgs[1] { aaa });

        var shader = surfaceNetsCompute;
        shader.SetBool("blocky", blocky);
        shader.SetInt("size", size);
        shader.SetInt("testTextureSize", testTextureSize);

        int id = shader.FindKernel("CSVertex");
        shader.SetTexture(id, "densities", voxels);
        shader.SetTexture(id, "colorsIn", colors);
        shader.SetBuffer(id, "atomicCounters", atomicCounters);
        shader.SetBuffer(id, "vertices", vertexBuffer);
        shader.SetBuffer(id, "normals", normalsBuffer);
        shader.SetBuffer(id, "colors", colorsBuffer);
        shader.SetBuffer(id, "cmdBuffer", commandBuffer);
        shader.SetTexture(id, "vertexIds", tempVertexTexture);
        shader.Dispatch(id, size / 8, size / 8, size / 8);
        
        id = shader.FindKernel("CSQuad");
        shader.SetTexture(id, "densities", voxels);
        shader.SetBuffer(id, "indices", indexBuffer);
        shader.SetTexture(id, "vertexIds", tempVertexTexture);
        shader.SetBuffer(id, "cmdBuffer", commandBuffer);
        shader.SetBuffer(id, "atomicCounters", atomicCounters);
        shader.Dispatch(id, size / 8, size / 8, size / 8);

        id = shader.FindKernel("CSTexturing");
        shader.SetTexture(id, "testTextureIndeed", testTexture);
        shader.SetBuffer(id, "indices", indexBuffer);
        shader.SetBuffer(id, "vertices", vertexBuffer);
        shader.Dispatch(id, testTextureSize / 32, testTextureSize / 32, 1);
    }

    public void ExecuteHeightMapMesher(RenderTexture voxels, RenderTexture colors, int indexed, Vector3Int chunkOffset) {
        if (atomicCounters == null || !atomicCounters.IsValid())
            return;

        int size = voxels.width;

        if (indexed == -1)
            commandBuffer.SetData(new GraphicsBuffer.IndirectDrawIndexedArgs[1] { aaa });

        var shader = heightMapCompute;
        shader.SetInt("size", size);
        shader.SetVector("vertexOffset", (Vector3)chunkOffset * size);

        Graphics.SetRenderTarget(maxHeightAtomic);
        GL.Clear(false, true, Color.clear);
        Graphics.SetRenderTarget(null);

        int id = shader.FindKernel("CSFlatten");
        shader.SetTexture(id, "densities", voxels);
        shader.SetTexture(id, "maxHeight", maxHeightAtomic);
        shader.Dispatch(id, size / 8, size / 8, size / 8);

        id = shader.FindKernel("CSVertex");
        shader.SetInt("indexOffset", indexed == -1 ? 0 : indexed);
        shader.SetTexture(id, "densities", voxels);
        shader.SetTexture(id, "colorsIn", colors);
        shader.SetTexture(id, "maxHeight", maxHeightAtomic);
        shader.SetBuffer(id, "indices", indexBuffer);
        shader.SetBuffer(id, "vertices", vertexBuffer);
        shader.SetBuffer(id, "normals", normalsBuffer);
        shader.SetBuffer(id, "colors", colorsBuffer);
        shader.SetBuffer(id, "cmdBuffer", commandBuffer);
        shader.Dispatch(id, size / 32, size / 32, 1);
    }

    /*
    public void Start() {
        var executor = GetComponent<VoxelGraphExecutor>();
        InitializeForSize(executor.size);
        int index = 0;
        for (int j = 0; j < 3; j++) {
            for (int i = 0; i < 3; i++) {
                var offset = new Vector3Int(i, 0, j);
                executor.ExecuteShader(offset);
                RenderTexture density = (RenderTexture)executor.Textures["voxels"];
                RenderTexture colors = (RenderTexture)executor.Textures["colors"];
                ExecuteHeightMapMesher(density, colors, index, offset);
                //Meshify(density, colors, i);
                index++;
            }
        }
    }
    */

    public void Update() {
        RenderIndexedIndirectMesh();
    }

    public void RenderIndexedIndirectMesh() {
        if (indexBuffer == null || commandBuffer == null || !indexBuffer.IsValid() || !commandBuffer.IsValid())
            return;

        Bounds bounds = new Bounds {
            center = Vector3.zero,
            extents = Vector3.one * 1000.0f,
        };

        var mat = new MaterialPropertyBlock();
        mat.SetBuffer("_Indices", indexBuffer);
        mat.SetBuffer("_Vertices", vertexBuffer);
        mat.SetBuffer("_Normals", normalsBuffer);
        mat.SetBuffer("_Colors", colorsBuffer);
        mat.SetTexture("_TestTexture", testTexture);
        mat.SetInt("_Flatshaded", (flatshaded || blocky) ? 1 : 0);
        mat.SetInt("_TestTextureSize", testTextureSize);

        // FIXME: Why do I need to use this instead of just render mesh primitives indexed inderect???
        // Also why do I need to handle the indexing myself???

        // I kinda realized this too late but this is a non indexed thing, which explains why we need to sample the indices by ourselves in the shader
        // this also explains why I can "emulate" "flat" or no-interpolation by just passing different values that don't map to the vertex attributes

        // I really hope the slowness that comes from non-indexed rendering comes from the fact that it uses a lot of duped vertices and not the fact
        // that it's, well, is non-indexed.
        // In either case this is the only way to make it work with urp rn anyways so not like I have a choice AND it allows me to experiment with the goofy
        // texture mapping idea that I'm fiddling with right now
        Graphics.DrawProceduralIndirect(customRenderingMaterial, bounds, MeshTopology.Triangles, commandBuffer, properties: mat);
    }
}
