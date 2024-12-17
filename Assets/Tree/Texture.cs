using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class GradientTexture {
    public List<string> readKernels;
    public int size;
}

public class TempTexture {
    public Utils.StrictType type;
    public FilterMode filter;
    public TextureWrapMode wrap;
    public List<string> readKernels;
    public string writeKernel;
    public bool threeDimensions;
    public int sizeReductionPower;
    public bool mips;
}

public class KernelTexture {
    public Utils.StrictType type;
    public FilterMode filter;
    public TextureWrapMode wrap;
    public List<string> readKernels;
    public string writeKernel;
    public Action<uint3> calculateSize;
    public bool mips;
}