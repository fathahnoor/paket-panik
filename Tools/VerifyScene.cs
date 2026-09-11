using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public static class VerifyScene
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var scene = EditorSceneManager.GetActiveScene();
        sb.Append("scene=").Append(scene.name);
        var game = GameObject.Find("/PaketPanik");
        var board = game.GetComponent<PaketPanik.SharedBoard>();
        var pres = game.GetComponent<PaketPanik.PanicPresentation>();
        var lan = game.GetComponent<PaketPanik.LanSession>();
        var origin = GameObject.Find("/XR Origin");
        var images = origin.GetComponent<ARTrackedImageManager>();
        var cam = GameObject.Find("/XR Origin/Camera Offset/Main Camera").GetComponent<Camera>();
        sb.Append("|boardRoot=").Append(board.boardRoot != null && board.boardRoot.name == "BoardRoot");
        sb.Append("|cam=").Append(board.arCamera == cam);
        sb.Append("|images=").Append(board.images == images);
        sb.Append("|anchors=").Append(board.anchors != null);
        sb.Append("|session=").Append(board.session != null);
        sb.Append("|pres=").Append(pres.board == board && pres.session == lan);
        var lib = images.referenceLibrary as XRReferenceImageLibrary;
        sb.Append("|lib=").Append(lib != null ? lib.count.ToString() : "NULL");
        if (lib != null && lib.count > 0) sb.Append("|name=").Append(lib[0].name).Append("|size=").Append(lib[0].size.x);
        sb.Append("|balance=").Append(Resources.Load<TextAsset>("PaketPanik/balance") != null);
        sb.Append("|hash=").Append(Resources.Load<TextAsset>("PaketPanik/marker-hash") != null);
        sb.Append("|product=").Append(PlayerSettings.productName);
        sb.Append("|pkg=").Append(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        sb.Append("|build0=").Append(EditorBuildSettings.scenes.Length > 0 ? EditorBuildSettings.scenes[0].path : "EMPTY");
        return sb.ToString();
    }
}
