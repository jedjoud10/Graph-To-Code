using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Simple script that handles previewing voxel graph as a volume texture
// This will call the shader every frame to execute it
public class VoxelGraphPreview : MonoBehaviour {
    public bool previewing;
    public float opacity;
    public int quality;
    public Gradient gradient;
    public FilterMode filterMode;
    [Range(16, 64)]
    public int size = 16;

    public void OnValidate() {
        size = Mathf.Max(Mathf.ClosestPowerOfTwo(size), 16);
    }

    static uint3 IndexToPos(int index, uint size) {
        uint index2 = (uint)index;

        // N(ABC) -> N(A) x N(BC)
        uint y = index2 / (size * size);   // x in N(A)
        uint w = index2 % (size * size);  // w in N(BC)

        // N(BC) -> N(B) x N(C)
        uint z = w / size;        // y in N(B)
        uint x = w % size;        // z in N(C)
        return new uint3(x, y, z);
    }
}