using UnityEngine;

public static class SmokeHost
{
    public static string Run()
    {
        var session = Object.FindFirstObjectByType<PaketPanik.LanSession>();
        if (session == null) return "NO_SESSION";
        bool ok = session.Host();
        return "host=" + ok + "|status=" + session.Status + "|pin=" + session.Pin;
    }
}
