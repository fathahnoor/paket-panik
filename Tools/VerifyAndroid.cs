using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;

public static class VerifyAndroid
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.Append("target=").Append(EditorUserBuildSettings.activeBuildTarget);
        sb.Append("|pkg=").Append(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        sb.Append("|minSdk=").Append(PlayerSettings.Android.minSdkVersion);
        sb.Append("|arch=").Append(PlayerSettings.Android.targetArchitectures);
        sb.Append("|backend=").Append(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
        sb.Append("|apis=").Append(string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)));
        sb.Append("|orient=").Append(PlayerSettings.defaultInterfaceOrientation);
        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        sb.Append("|loaders=");
        if (settings != null)
        {
            foreach (var loader in settings.Manager.activeLoaders) sb.Append(loader.GetType().Name).Append(',');
        }
        else sb.Append("NULL");
        return sb.ToString();
    }
}
