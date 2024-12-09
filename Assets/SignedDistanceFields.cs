using Unity.Mathematics;

/*
public abstract class SdfShape {
    public Var<float3> offset;
    public SdfShape(Var<float3> offset) {
        this.offset = offset;
    }

    public abstract string Internal(string name);
    public virtual Var<float> Evaluate(Var<float3> position) {
        return Var<float>.CreateFromName(position.name + "_sdf", Internal($"{position.name} - {offset.name}"));
    }
}

public class SdfBox : SdfShape {
    public Var<float3> extent;

    public SdfBox(Var<float3> offset, Var<float3> extent) : base(offset) {
        this.extent = extent;
    }

    public override string Internal(string name) {
        return $"sdBox({name}, {extent.name})";
    }
}
*/