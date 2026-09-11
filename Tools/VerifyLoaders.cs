using System.Text;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

public static class VerifyLoaders
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var android = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(UnityEditor.BuildTargetGroup.Android);
        var standalone = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(UnityEditor.BuildTargetGroup.Standalone);
        sb.Append("android=");
        if (android != null) foreach (var loader in android.Manager.activeLoaders) sb.Append(loader.GetType().Name).Append(',');
        sb.Append("|standalone=");
        if (standalone != null) foreach (var loader in standalone.Manager.activeLoaders) sb.Append(loader.GetType().Name).Append(',');
        return sb.ToString();
    }
}
