using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Profiling;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class CellularTilerNode<T> : Variable<float> {
	public Variable<T> inner;
	public float tilingModSize;
	public CellularTiler<T>.Distance distance;
    public CellularTiler<T>.ShouldSpawn shouldSpawn;

	public Variable<T> min;
    public Variable<T> max;
    public Variable<float> offset;
    public Variable<float> factor;

    public override void HandleInternal(TreeContext context) {
        inner.Handle(context);
		bool tiling = tilingModSize > 0;
		context.Hash(tilingModSize);

        string scopeName = context.GenId($"PeriodicityScope");
        string outputName = $"{scopeName}_sdf_output";

        Variable<float> tahini = new CustomCode<float>((TreeNode self, TreeContext ctx) => {
            offset.Handle(ctx);
            factor.Handle(ctx);
            string typeString = Utils.ToStringType<T>();

            int maxLoopSize = 1;
            int dimensions = Utils.DimensionalitySafeTextureSample<T>();
            
            string loopInit = dimensions == 2 ? $@"
for (int y = -{maxLoopSize}; y <= {maxLoopSize}; y++)
for (int x = -{maxLoopSize}; x <= {maxLoopSize}; x++) {{
" : $@"
for(int z = -{maxLoopSize}; z <= {maxLoopSize}; z++)
for(int y = -{maxLoopSize}; y <= {maxLoopSize}; y++)
for(int x = -{maxLoopSize}; x <= {maxLoopSize}; x++) {{
";

            string tiler = tiling ? $"{typeString} tiled = fmod(cell, {tilingModSize});" : $"{typeString} tiled = cell;";

            string outputFirst = $@"
{typeString} posCell = floor({ctx[inner]});
{typeString} posFrac = frac({ctx[inner]});

float output = 100.0;

{loopInit}
    {typeString} cell = {typeString}({Utils.VectorConstructor<T>()}) + posCell;
    {tiler}
    {typeString} randomOffset = hash{dimensions}{dimensions}(tiled);
";
            //hash1{dimensions}(tiled)
            Variable<T> tiled = ctx.AssignTempVariable<T>("test__", "tiled");
            Variable<float> shouldSpawnVar = shouldSpawn(tiled);
            shouldSpawnVar.Handle(ctx);

            string outputSecond = $@"
    if ({ctx[shouldSpawnVar]} > 0.0) {{
        {typeString} checkingPos = cell + randomOffset;
        output = min(output, distance(checkingPos, {ctx[inner]}));
    }}
}}
";
            ctx.AddLine(outputFirst);
            ctx.AddLine(outputSecond);
            ctx.DefineAndBindNode<float>(self, "__", $"min(output, 1) * {ctx[factor]} + {ctx[offset]}");
        });

        ScopeArgument input = new ScopeArgument(context[inner], Utils.TypeOf<T>(), inner, false);
        ScopeArgument output = new ScopeArgument(outputName, Utils.TypeOf<float>(), tahini, true);

        int index = context.scopes.Count;
        int oldScopeIndex = context.currentScope;

        TreeScope scopium = new TreeScope(context.scopeDepth + 1) {
            name = scopeName,
            arguments = new ScopeArgument[] { input, output, },
            namesToNodes = new Dictionary<TreeNode, string> { { input.node, context[input.node] } },
        };

        context.scopes.Add(scopium);

        // ENTER NEW SCOPE!!!
        context.currentScope = index;
        context.scopeDepth++;

        // Add the start node (position node) to the new scope
        //context.scopes[index].namesToNodes.TryAdd(input.node, "position");

        // Call the recursive handle function within the indented scope
        tahini.Handle(context);

        // EXIT SCOPE!!!
        context.scopeDepth--;
        context.currentScope = oldScopeIndex;

        context.scopes[context.currentScope].lines.Add(scopium.InitializeTempnation());
        context.scopes[context.currentScope].lines.Add(scopium.Callenate());
        context.DefineAndBindNode<float>(this, "cellular_tiler", outputName);
    }
}

public class CellularTiler<T> {
	public float tilingModSize;

	public delegate Variable<float> Distance(Variable<T> a, Variable<T> b);
    public delegate Variable<float> ShouldSpawn(Variable<T> point);

	public Distance distance;
    public ShouldSpawn shouldSpawn;

	public Variable<T> min;
    public Variable<T> max;

    public Variable<float> offset;
    public Variable<float> factor;

    public CellularTiler(Distance distance = null, ShouldSpawn shouldSpawn = null, float ilingModSize = -1) {
        this.distance = distance;
        this.tilingModSize = ilingModSize;
        this.shouldSpawn = shouldSpawn;
        this.min = null;
        this.max = null;
        this.offset = 0.0f;
        this.factor = 1.0f;
    }

    public Variable<float> Tile(Variable<T> position) {
        return new CellularTilerNode<T>() {
			tilingModSize = tilingModSize,
			distance = distance != null ? distance : (a, b) => SdfOps.Distance(a, b),
			shouldSpawn = shouldSpawn != null ? shouldSpawn : (pos) => 1.0f,
			inner = position,
			min = min,
			max = max,
            offset = offset,
            factor = factor,
		};
	}
}

/*
float test( in float2 p)
{
	float2 posCell = floor(p);
    float2 posFrac = frac(p);
    
	float a = 100.0;
    for( int y=-1; y<=1; y++ )
    for( int x=-1; x<=1; x++ )
    {
        float2 cell = float2(x, y) + posCell;
		//cell = fmod(cell, 5);
		float2 tiled = fmod(cell, 16.0);
		float2 randomOffset = hash22(tiled);

		if (hash12(tiled) < 0.6) {
			float2 checkingPos = cell + randomOffset;
			a = min(a,customDistance(checkingPos, p));
		}
	}

	return min(a, 1) * 10.0;
}

float test( in float3 p)
{
	float3 posCell = floor(p);
    float3 posFrac = frac(p);
    
	float a = 1000.0;
	for( int z=-1; z<=1; z++ )
    for( int y=-1; y<=1; y++ )
    for( int x=-1; x<=1; x++ )
    {
        float3 cell = float3(x, y, z) + posCell;
		float3 tiled = fmod(cell, 4.0);
		float3 randomOffset = hash33(tiled);

		if (cell.y > 0) {
			continue;
		}

		if (hash13(tiled) < 0.6) {
			float3 checkingPos = cell + randomOffset;
			a = min(a,customDistance2(checkingPos, p));
		}
	}

	return a;
}
*/