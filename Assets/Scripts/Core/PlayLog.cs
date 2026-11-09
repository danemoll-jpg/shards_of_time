using UnityEngine;

namespace ShardsOfTime
{
    // Game events in Player.log, one line each ("[Play] <time> ..."), so a report from Dan's play can be read back.
    public static class PlayLog
    {
        public static event System.Action<string> Logged;
        public static void Line(string s) { Debug.Log($"[Play] {Time.realtimeSinceStartup:0.0} {s}"); Logged?.Invoke(s); }
    }
}
