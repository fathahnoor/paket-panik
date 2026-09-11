using UnityEngine;

public static class SmokeWorld
{
    public static string Run()
    {
        var session = Object.FindAnyObjectByType<PaketPanik.LanSession>();
        if (session == null) return "NO_SESSION";
        if (!session.Connected) session.Host();
        var board = Object.FindAnyObjectByType<PaketPanik.SharedBoard>();
        if (board == null) return "NO_BOARD";
        board.simulator = true;
        board.boardRoot.gameObject.SetActive(true);
        var cam = board.arCamera;
        var driver = cam.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (driver) driver.enabled = false;
        cam.transform.SetPositionAndRotation(new Vector3(0f, .42f, -.62f), Quaternion.Euler(16f, 0f, 0f));
        return "ok|hosting=" + session.Hosting + "|status=" + session.Status;
    }
}
