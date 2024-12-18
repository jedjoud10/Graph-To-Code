using Unity.Mathematics;

public class WarperNode : Variable<float2> {
    public AbstractNoiseNode<float2> toClone;
    public Variable<float2> warpingScale2;
    public Variable<float2> warpingScale;
    public Variable<float2> position;

    public float3 offsets_x = new float3(123.85441f, 32.223543f, -359.48534f);
    public float3 offsets_y = new float3(65.4238f, -551.15353f, 159.5435f);
    public float3 offsets_z = new float3(-43.85454f, -3346.234f, 54.7653f);

    public override void HandleInternal(TreeContext context) {
        warpingScale2.Handle(context);
        warpingScale.Handle(context);
        position.Handle(context);

        Variable<float2> a_offsetted = context.AssignTempVariable<float2>($"{context[position]}_x_offset", $"(({context[position]} + float2({offsets_x.x}, {offsets_x.y})) * {context[warpingScale]}.x)");
        Variable<float2> b_offsetted = context.AssignTempVariable<float2>($"{context[position]}_y_offset", $"(({context[position]} + float2({offsets_y.x}, {offsets_y.y})) * {context[warpingScale]}.y)");

        var a = (AbstractNoiseNode<float2>)toClone.Clone();
        a.position = a_offsetted;
        var b = (AbstractNoiseNode<float2>)toClone.Clone();
        b.position = b_offsetted;

        a.Handle(context);
        b.Handle(context);


        Variable<float> a2 = context.AssignTempVariable<float>($"{context[position]}_warped_x", $"({context[position]}.x + {context[a]} * {context[warpingScale2]}.x)");
        Variable<float> b2 = context.AssignTempVariable<float>($"{context[position]}_warped_y", $"({context[position]}.y + {context[b]} * {context[warpingScale2]}.y)");
        context.DefineAndBindNode<float2>(this, $"{context[position]}_warped", $"float2({context[a2]}, {context[b2]})");
    }
}

public class Warper {
    public Noise noisinator;
    public Variable<float2> warpingScale2;
    public Variable<float2> warpingScale;

    public Warper(Noise noise) {
        this.noisinator = noise;
        this.warpingScale = new float2(1.0f);
        this.warpingScale2 = new float2(1.0f);
    }

    public Variable<float2> Warpinate(Variable<float2> position) {
        return new WarperNode {
            toClone = noisinator.CreateAbstractYetToEval<float2>(),
            warpingScale = warpingScale,
            warpingScale2 = warpingScale2,
            position = position,
        };
    }
}
