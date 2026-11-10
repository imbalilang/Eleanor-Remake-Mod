using UnityEngine;

namespace EleanorRomance
{
    public static class VersionGate
    {
        public static string GameVersion { get; private set; } = "";
        public static bool IsSupported { get; private set; } = true;

        public static void Refresh()
        {
            try
            {
                GameVersion = Application.version ?? "";
                // Verified against the installed 0.46D and 1.0.0.14 builds.
                // Empty is allowed only during the very early load before
                // Application.version is available; a later Refresh() will gate
                // the actual story.
                IsSupported = string.IsNullOrEmpty(GameVersion)
                    || GameVersion == "0.46D"
                    || GameVersion == "0.46"
                    || GameVersion == "1.0.0.13"
                    || GameVersion == "1.0.0.14";
            }
            catch
            {
                GameVersion = "";
                IsSupported = true;
            }
        }
    }
}
