using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using UnityEditor;

[ScriptedImporter(1, "voxel")]
public class Importer : ScriptedImporter {
    public override void OnImportAsset(AssetImportContext ctx) {
        string code = File.ReadAllText(ctx.assetPath);
        ComputeShader shader = ShaderUtil.CreateComputeShaderAsset(ctx, code);
        ctx.AddObjectToAsset("main obj", shader);
        ctx.SetMainObject(shader);
    }
}
