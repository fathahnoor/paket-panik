using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public static class SimulateRun
{
    public static string Begin()
    {
        var board = Object.FindAnyObjectByType<PaketPanik.SharedBoard>();
        var session = Object.FindAnyObjectByType<PaketPanik.LanSession>();
        if (board == null) return "NO_BOARD";
        if (session != null && !session.Connected) session.Host();
        board.BeginCamera();
        return "state=" + ARSession.state;
    }

    public static string Poll()
    {
        var board = Object.FindAnyObjectByType<PaketPanik.SharedBoard>();
        if (board == null) return "NO_BOARD";
        return "status=" + board.Status + "|calibrated=" + board.Calibrated + "|tracking=" + board.TrackingValid + "|supported=" + board.Supported;
    }
}
