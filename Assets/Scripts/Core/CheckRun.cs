using System;

namespace ShardsOfTime
{
    // An exe check run (-exeCheck <dir>, Checks/ExeCheck.cs): settings are the defaults and never saved; the save file lives in the
    // check's own folder (<dir>/save).
    public static class CheckRun
    {
        static int active = -1;
        public static bool Active { get { if (active < 0) active = Array.IndexOf(Environment.GetCommandLineArgs(), "-exeCheck") >= 0 ? 1 : 0; return active == 1; } }
        public static string Dir => LauncherBridge.Argument("-exeCheck");
        public static string Only => LauncherBridge.Argument("-exeCheckOnly") ?? "all";
    }
}
