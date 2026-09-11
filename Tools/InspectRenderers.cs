using System.Text;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class InspectRenderers
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.Append("URPAssetRunner:");
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
            sb.Append(' ').Append(AssetDatabase.GUIDToAssetPath(guid));
        sb.Append(" || dataType:");
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Project Configuration/Performant URP Renderer Config.asset");
        sb.Append(asset == null ? "NULL" : asset.GetType().FullName);
        if (asset is UniversalRendererData data)
        {
            sb.Append(" features:");
            foreach (var f in data.rendererFeatures) sb.Append(f == null ? "NULL" : f.GetType().Name).Append(',');
        }
        return sb.ToString();
    }
}
