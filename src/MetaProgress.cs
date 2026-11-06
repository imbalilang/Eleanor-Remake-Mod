using System;

namespace EleanorRomance
{
    public static class MetaProgress
    {
        public static bool IsTrueEndingUnlocked()
        {
            try
            {
                if (!ModHelper.ModDataFileExists(ModConstants.ModId, ModConstants.MetaFile))
                {
                    return false;
                }

                string json = ModHelper.ReadModDataFileText(ModConstants.ModId, ModConstants.MetaFile);
                return !string.IsNullOrEmpty(json)
                    && json.IndexOf("\"trueEnding\":true", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        public static void UnlockTrueEnding()
        {
            try
            {
                string json = "{\"trueEnding\":true,\"unlockedUtc\":\"" + DateTime.UtcNow.ToString("o") + "\"}";
                ModHelper.WriteModDataFileText(ModConstants.ModId, ModConstants.MetaFile, json);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to write meta progress: " + ex);
            }
        }
    }
}
