using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Simulation;

public static class SetupSimulation
{
    const string Source = "Packages/com.unity.xr.arfoundation/Assets/Prefabs/DefaultSimulationEnvironment.prefab";
    const string Target = "Assets/PaketPanik/Art/SimulationPaketPanik.prefab";
    const string MarkerTex = "Assets/PaketPanik/Art/Marker_PaketPanik.png";
    const string Prefs = "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";

    public static string Run()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerTex);
        if (!texture) return "NO_MARKER";
        AssetDatabase.DeleteAsset(Target);
        if (!AssetDatabase.CopyAsset(Source, Target)) return "COPY_FAILED";

        string report;
        var root = PrefabUtility.LoadPrefabContents(Target);
        try
        {
            var image = root.GetComponentsInChildren<SimulatedTrackedImage>(true).FirstOrDefault();
            if (image == null) report = "NO_SIM_IMAGE_IN_TEMPLATE";
            else
            {
                var imageSo = new SerializedObject(image);
                imageSo.FindProperty("m_Image").objectReferenceValue = texture;
                imageSo.FindProperty("m_ImagePhysicalSizeMeters").vector2Value = new Vector2(.2f, .2f);
                imageSo.ApplyModifiedPropertiesWithoutUndo();
                image.transform.localPosition += new Vector3(0f, .04f, 0f);

                var env = root.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == "SimulationEnvironment");
                var marker = image.transform.localPosition;
                if (env != null)
                {
                    var envSo = new SerializedObject(env);
                    var pose = envSo.FindProperty("m_CameraStartingPose");
                    var desired = marker + new Vector3(0f, .35f, -.55f);
                    var rot = Quaternion.LookRotation((marker - desired).normalized, Vector3.up);
                    pose.FindPropertyRelative("position").vector3Value = desired;
                    pose.FindPropertyRelative("rotation").quaternionValue = rot;
                    envSo.ApplyModifiedPropertiesWithoutUndo();
                }
                report = "image@" + marker + "|desiredEye=" + (marker + new Vector3(0f, .35f, -.55f));
            }
        }
        finally
        {
            PrefabUtility.SaveAsPrefabAsset(root, Target);
            PrefabUtility.UnloadPrefabContents(root);
        }

        var prefs = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Prefs);
        if (prefs != null)
        {
            var prefsSo = new SerializedObject(prefs);
            var prop = prefsSo.FindProperty("m_EnvironmentPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Target);
                prefsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefs);
            }
        }

        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        bool loader = settings != null && XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.Simulation.SimulationLoader", BuildTargetGroup.Standalone);
        AssetDatabase.SaveAssets();
        return report + "|prefs=" + (prefs != null) + "|loader=" + loader;
    }
}
