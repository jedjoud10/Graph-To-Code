using UnityEngine;

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
}