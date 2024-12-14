using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class GradientTexture {
    public string name;
    public List<string> readKernels;
    public int size;
}

[Serializable]
public class TempTexture {
    public string name;
    public Utils.StrictType type;
    public FilterMode filter;
    public TextureWrapMode wrap;
    public List<string> readKernels;
    public string writeKernel;
    public bool threeDimensions;
    public int sizeReductionPower;
    public bool mips;
}