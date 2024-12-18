
using System;
using System.Collections.Generic;

public class KernelTextureOutput {
    public string name;
}

public class KernelDispatch {
    public string name;
    public int depth;
    public int sizeReductionPower;
    public bool threeDimensions;
    public string writingCoords;
    public string remappedCoords;
    public string dispatchGroups;
    public List<KernelTextureOutput> outputs;
}