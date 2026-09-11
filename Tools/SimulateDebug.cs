using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Simulation;

public static class SimulateDebug
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var board = Object.FindAnyObjectByType<PaketPanik.SharedBoard>();
        if (board == null) return "NO_BOARD";
        var cam = board.arCamera.transform;
        sb.Append("cam=").Append(cam.position.ToString("F2")).Append(' ').Append(cam.rotation.eulerAngles.ToString("F0"));
        foreach (var img in Object.FindObjectsByType<SimulatedTrackedImage>(FindObjectsSortMode.None))
            sb.Append("|img=").Append(img.gameObject.name).Append('@').Append(img.transform.position.ToString("F2")).Append(" size=").Append(img.size).Append(" active=").Append(img.gameObject.activeInHierarchy);
        var manager = Object.FindAnyObjectByType<ARTrackedImageManager>();
        sb.Append("|lib=").Append(manager != null && manager.referenceLibrary != null ? manager.referenceLibrary.count.ToString() : "NULL");
        sb.Append("|trackables=").Append(manager != null ? manager.trackables.count.ToString() : "?");
        foreach (var trackable in manager.trackables)
            sb.Append("|tracked=").Append(trackable.trackingState).Append('@').Append(trackable.transform.position.ToString("F2")).Append(' ').Append(trackable.transform.rotation.eulerAngles.ToString("F0"));
        sb.Append("|boardRoot=").Append(board.boardRoot.position.ToString("F2")).Append(" active=").Append(board.boardRoot.gameObject.activeInHierarchy).Append(" parent=").Append(board.boardRoot.parent != null ? board.boardRoot.parent.name : "null");
        foreach (var r in board.boardRoot.GetComponentsInChildren<Renderer>(true))
        { sb.Append("|r=").Append(r.name).Append('@').Append(r.bounds.center.ToString("F2")).Append(" on=").Append(r.enabled); break; }
        return sb.ToString();
    }
}
