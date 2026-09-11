using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;

namespace PaketPanik.EditorTools
{
    // Idempotent project builder: marker art, image library, Resources, renderer feature, scene and build settings.
    public static class PaketPanikSceneBuilder
    {
        const string Root = "Assets/PaketPanik";
        const string ScenePath = Root + "/Scenes/PaketPanik.unity";
        const string ArtFolder = Root + "/Art";
        const string MarkerPng = ArtFolder + "/Marker_PaketPanik.png";
        const string MarkerPrint = ArtFolder + "/Marker_PaketPanik_Print.html";
        const string MarkerInfo = ArtFolder + "/MARKER_INFO.md";
        const string LibraryPath = ArtFolder + "/PaketPanikMarkerLibrary.asset";
        const string ResFolder = Root + "/Resources/PaketPanik";
        const string MarkerName = "PaketPanikMarker";
        const float MarkerMeters = .20f;
        const string AndroidPackage = "com.fathahnoor.paketpanik";

        [MenuItem("PaketPanik/Build Project Assets")]
        public static void BuildMenu() => Debug.Log(BuildAll());

        public static string BuildAll()
        {
            var report = new StringBuilder("PAKET_PANIK_BUILD ");
            EnsureFolder(ArtFolder);
            EnsureFolder(ResFolder);
            EnsureFolder(Root + "/Scenes");
            report.Append("|balance:").Append(SyncBalance());
            var texture = BuildMarkerTexture();
            report.Append("|marker:").Append(BuildMarkerLibrary(texture));
            report.Append("|hash:").Append(WriteMarkerHash());
            report.Append("|renderers:").Append(AddBackgroundFeatureToRenderers());
            report.Append("|scene:").Append(BuildScene());
            report.Append("|player:").Append(ConfigurePlayer());
            report.Append("|loaders:").Append(ConfigureAndroidLoaders());
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static string SyncBalance()
        {
            const string source = "Design/balance.json";
            if (!File.Exists(source)) return "MISSING";
            File.WriteAllText(ResFolder + "/balance.json", File.ReadAllText(source), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ResFolder + "/balance.json", ImportAssetOptions.ForceSynchronousImport);
            return "OK";
        }

        static Texture2D BuildMarkerTexture()
        {
            const int S = 512;
            var rng = new System.Random(20260911);
            var px = new Color32[S * S];
            Color32 paper = new Color32(236, 226, 208, 255);
            Color32 card = new Color32(201, 161, 107, 255);
            Color32 ink = new Color32(23, 20, 28, 255);
            Color32 lilac = new Color32(180, 156, 255, 255);
            Color32 gold = new Color32(255, 211, 103, 255);
            Color32 coral = new Color32(255, 111, 119, 255);
            Color32 mint = new Color32(118, 224, 186, 255);
            Color32 white = new Color32(255, 252, 245, 255);

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    px[y * S + x] = Shift(paper, rng.Next(-9, 10));

            Frame(px, S, 16, 16, S - 32, S - 32, 10, card);
            Corner(px, S, 44, 44, 1, 1, 74, 16, ink);
            Corner(px, S, S - 44, 44, -1, 1, 74, 16, ink);
            Corner(px, S, 44, S - 44, 1, -1, 74, 16, ink);

            Rect(px, S, 66, 86, 158, 62, ink);
            for (int i = 0; i < 12; i++) Rect(px, S, 76 + i * 12, 94, i % 3 == 0 ? 7 : 3, 46, white);
            Disc(px, S, 362, 118, 62, ink);
            Rect(px, S, 354, 70, 16, 44, white);
            Disc(px, S, 362, 136, 10, white);

            Stripes(px, S, 34, 296, 178, 178, lilac, ink, 13, 9);
            Checker(px, S, 298, 318, 152, 122, 25, gold, ink);
            Disc(px, S, 118, 214, 30, mint);
            Tri(px, S, 292, 46, 74, 84, 108, 150, coral);
            Rect(px, S, 288, 100, 96, 22, ink);
            Disc(px, S, 458, 470, 18, coral);

            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(MarkerPng), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(MarkerPng, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(MarkerPng);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPng);
        }

        static string BuildMarkerLibrary(Texture2D texture)
        {
            if (!texture) return "NO_TEXTURE";
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (!library)
            {
                library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            while (library.count > 0) library.RemoveAt(0);
            library.Add();
            library.SetTexture(0, texture, true);
            library.SetName(0, MarkerName);
            library.SetSpecifySize(0, true);
            library.SetSize(0, new Vector2(MarkerMeters, MarkerMeters));
            EditorUtility.SetDirty(library);
            return "OK";
        }

        static string WriteMarkerHash()
        {
            if (!File.Exists(MarkerPng)) return "NO_PNG";
            byte[] bytes = File.ReadAllBytes(MarkerPng);
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            File.WriteAllText(ResFolder + "/marker-hash.txt", hash, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ResFolder + "/marker-hash.txt", ImportAssetOptions.ForceSynchronousImport);
            File.WriteAllText(MarkerInfo,
                "# Marker PaketPanik\n\n- File: Marker_PaketPanik.png (512x512 px)\n- Lebar fisik: 0.20 m, kuadrat\n- SHA256: " + hash +
                "\n- Versi library: 1\n- Digenerate oleh PaketPanikSceneBuilder pada " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " WIB\n\nCetak 20 cm (lihat Marker_PaketPanik_Print.html), datar di meja terang. Jangan diubah tanpa membangun ulang library dan menyamakan hash di kedua HP.\n");
            File.WriteAllText(MarkerPrint, PrintHtml(hash), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(MarkerPrint, ImportAssetOptions.ForceSynchronousImport);
            return hash.Substring(0, 12);
        }

        static string PrintHtml(string hash) =>
            "<!doctype html><html lang=\"id\"><meta charset=\"utf-8\"><title>PAKET PANIK - Kartu Marker</title>" +
            "<style>body{font:16px/1.5 system-ui,sans-serif;background:#101219;color:#f5f2ec;text-align:center;padding:24px}" +
            "img{width:20cm;height:20cm;background:#fff;padding:1cm;border-radius:8px}" +
            "p{max-width:20cm;margin:14px auto;color:#bbbfc9}@media print{body{background:#fff;color:#000}}</style>" +
            "<h1>PAKET PANIK &mdash; kartu marker</h1><img src=\"Marker_PaketPanik.png\" alt=\"marker\">" +
            "<p>Cetak pada skala 100% (tanpa \"fit to page\"). Ukur sisi gambar: harus 20,0 cm. Letakkan datar di meja terang.</p>" +
            "<p>SHA256: " + hash + "</p></html>";

        static void Frame(Color32[] px, int S, int x, int y, int w, int h, int t, Color32 c)
        {
            Rect(px, S, x, y, w, t, c);
            Rect(px, S, x, y + h - t, w, t, c);
            Rect(px, S, x, y, t, h, c);
            Rect(px, S, x + w - t, y, t, h, c);
        }

        static void Rect(Color32[] px, int S, int x, int y, int w, int h, Color32 c)
        {
            int x0 = Mathf.Max(0, x), y0 = Mathf.Max(0, y), x1 = Mathf.Min(S, x + w), y1 = Mathf.Min(S, y + h);
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                    px[yy * S + xx] = c;
        }

        static void Disc(Color32[] px, int S, int cx, int cy, int r, Color32 c)
        {
            int r2 = r * r;
            for (int yy = Mathf.Max(0, cy - r); yy < Mathf.Min(S, cy + r); yy++)
                for (int xx = Mathf.Max(0, cx - r); xx < Mathf.Min(S, cx + r); xx++)
                {
                    int dx = xx - cx, dy = yy - cy;
                    if (dx * dx + dy * dy <= r2) px[yy * S + xx] = c;
                }
        }

        static void Corner(Color32[] px, int S, int x, int y, int sx, int sy, int len, int t, Color32 c)
        {
            Rect(px, S, sx > 0 ? x : x - len, sy > 0 ? y : y - t, len, t, c);
            Rect(px, S, sx > 0 ? x : x - t, sy > 0 ? y : y - len, t, len, c);
        }

        static void Stripes(Color32[] px, int S, int x, int y, int w, int h, Color32 a, Color32 b, int period, int width)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    px[yy * S + xx] = ((xx + yy) / period) % 2 == 0 || (xx + yy) % period < width ? a : b;
        }

        static void Checker(Color32[] px, int S, int x, int y, int w, int h, int cell, Color32 a, Color32 b)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    px[yy * S + xx] = ((xx - x) / cell + (yy - y) / cell) % 2 == 0 ? a : b;
        }

        static void Tri(Color32[] px, int S, int ax, int ay, int bx, int by, int cx, int cy, Color32 c)
        {
            int x0 = Mathf.Max(0, Mathf.Min(ax, Mathf.Min(bx, cx))), x1 = Mathf.Min(S, Mathf.Max(ax, Mathf.Max(bx, cx)));
            int y0 = Mathf.Max(0, Mathf.Min(ay, Mathf.Min(by, cy))), y1 = Mathf.Min(S, Mathf.Max(ay, Mathf.Max(by, cy)));
            int s = Sign(ax, ay, bx, by), t = Sign(bx, by, cx, cy), u = Sign(cx, cy, ax, ay);
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                    if (Sign(xx, yy, bx, by) == s && Sign(xx, yy, cx, cy) == t && Sign(xx, yy, ax, ay) == u) px[yy * S + xx] = c;
        }

        static int Sign(int px, int py, int qx, int qy) => (px - qx) * (py - qy) >= 0 ? 1 : -1;

        static Color32 Shift(Color32 c, int v) => new Color32(
            (byte)Mathf.Clamp(c.r + v, 0, 255), (byte)Mathf.Clamp(c.g + v, 0, 255), (byte)Mathf.Clamp(c.b + v, 0, 255), 255);

        static string AddBackgroundFeatureToRenderers()
        {
            var result = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                result.Add(Path.GetFileNameWithoutExtension(path) + ":" + (AddBackgroundFeature(path) ? "ADDED" : "SKIP"));
            }
            return result.Count > 0 ? string.Join(",", result) : "NONE";
        }

        static bool AddBackgroundFeature(string path)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (!data) return false;
            foreach (var feature in data.rendererFeatures) if (feature is ARBackgroundRendererFeature) return false;
            var ar = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            ar.name = "ARBackgroundRendererFeature";
            AssetDatabase.AddObjectToAsset(ar, data);
            AssetDatabase.SaveAssets();
            var so = new SerializedObject(data);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            map.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ar;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            so.Update();
            map = so.FindProperty("m_RendererFeatureMap");
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ar, out _, out long localId))
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return true;
        }

        static string BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sessionGo = new GameObject("AR Session");
            var session = sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();

            var originGo = new GameObject("XR Origin");
            var origin = originGo.AddComponent<XROrigin>();
            var offsetGo = new GameObject("Camera Offset");
            offsetGo.transform.SetParent(originGo.transform, false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offsetGo.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 20f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ARCameraManager>();
            camGo.AddComponent<ARCameraBackground>();
            var driver = camGo.AddComponent<TrackedPoseDriver>();
            var position = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            position.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rotation = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
            driver.positionInput = new InputActionProperty(position);
            driver.rotationInput = new InputActionProperty(rotation);

            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            var images = originGo.AddComponent<ARTrackedImageManager>();
            images.referenceLibrary = library;
            var anchors = originGo.AddComponent<ARAnchorManager>();
            origin.Camera = cam;
            origin.Origin = originGo;
            origin.CameraFloorOffsetObject = offsetGo;

            var gameGo = new GameObject("PaketPanik");
            gameGo.AddComponent<Unity.Netcode.NetworkManager>();
            gameGo.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            var boardRoot = new GameObject("BoardRoot");
            boardRoot.transform.SetParent(gameGo.transform, false);
            var board = gameGo.AddComponent<SharedBoard>();
            var lan = gameGo.AddComponent<LanSession>();
            var presentation = gameGo.AddComponent<PanicPresentation>();
            board.boardRoot = boardRoot.transform;
            board.arCamera = cam;
            board.images = images;
            board.anchors = anchors;
            board.session = session;
            board.simulator = false;
            presentation.board = board;
            presentation.session = lan;
            presentation.interfaceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/MRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");

            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, .98f, .94f);
            lightGo.transform.rotation = Quaternion.Euler(52, -34, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .6f, .64f);
            RenderSettings.fog = false;

            session.enabled = false;
            images.enabled = false;
            anchors.enabled = false;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            return ScenePath;
        }

        static string ConfigurePlayer()
        {
            PlayerSettings.productName = "PAKET PANIK";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            return "OK";
        }

        static string ConfigureAndroidLoaders()
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null) return "NO_ANDROID_SETTINGS";
            bool added = XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android);
            bool removed = XRPackageMetadataStore.RemoveLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
            return "arcore:" + (added ? "ADD" : "HAD") + ",openxr:" + (removed ? "REMOVED" : "ABSENT");
        }
    }
}
