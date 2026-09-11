using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Simulation;

public static class SimulateQuality
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var board = Object.FindAnyObjectByType<PaketPanik.SharedBoard>();
        var cam = board.arCamera;
        var image = Object.FindAnyObjectByType<SimulatedTrackedImage>();
        if (image == null) return "NO_IMAGE";
        var imageTransform = image.transform;
        var imagePos = imageTransform.position;
        var camTransform = cam.transform;
        var view = cam.WorldToViewportPoint(imagePos);
        var forward = camTransform.forward;
        var toImage = (imagePos - camTransform.position).normalized;
        sb.Append("view=").Append(view.ToString("F2"));
        sb.Append("|dist=").Append(Vector3.Distance(imagePos, camTransform.position).ToString("F2"));
        sb.Append("|dotNormal=").Append(Vector3.Dot(forward, imageTransform.up).ToString("F2"));
        sb.Append("|dotPos=").Append(Vector3.Dot(forward, toImage).ToString("F2"));
        var layerMask = ~0;
        var hits = Physics.RaycastAll(camTransform.position, imagePos - camTransform.position, Vector3.Distance(imagePos, camTransform.position), layerMask);
        sb.Append("|hits=").Append(hits.Length);
        foreach (var hit in hits) sb.Append('[').Append(hit.collider.gameObject.name).Append('@').Append((hit.point - imagePos).magnitude.ToString("F2")).Append(']');
        return sb.ToString();
    }
}
