using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Game version compatibility is deliberately not an allow list.  The game
    /// changes Application.version on every patch, and a list of accepted
    /// strings turned each patch into a story shutdown for players who were
    /// already mid-save.  The version is recorded for diagnostics only; the
    /// mod compiles against the installed game assembly and its Harmony patch
    /// report is the compatibility check.  Save-schema compatibility is handled
    /// separately by RomanceState.IncompatibleSave.
    /// </summary>
    public static class VersionGate
    {
        public static string GameVersion { get; private set; } = "";

        // Kept for feature code that predates this policy.  It must never turn
        // a game update into a refusal to load the story.
        public static bool IsSupported => true;

        public static void Refresh()
        {
            try
            {
                GameVersion = Application.version ?? "";
            }
            catch
            {
                GameVersion = "";
            }
        }
    }
}
