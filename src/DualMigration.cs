using System;

namespace EleanorRomance
{
    /// <summary>
    /// Initializes the dual extension from the facts already present in a save.
    /// It never replays a confession, a gift or a main-story chapter.
    /// </summary>
    public static class DualMigration
    {
        public static void OnLoaded()
        {
            if (!RomanceState.StoryActive || VersionGate.IsSupported == false) return;
            if (DualState.FeatureVersion > DualState.CurrentVersion)
            {
                ModEntry.Log("Dual romance paused: future schema " + DualState.FeatureVersion
                    + ", expected " + DualState.CurrentVersion + ".");
                return;
            }

            if (DualState.FeatureVersion == 0)
            {
                if (!CanOpenFromFacts()) return;
                if (RomanceState.RomanceConfirmed)
                {
                    DualState.Accept(2);
                    // The ordinary confession already happened; the dual route
                    // resumes with the old-lover entry and never repeats dr05.
                    DualState.Flag("scene.dr05", true);
                }
                else DualState.FeatureVersion = DualState.CurrentVersion;
                ModEntry.Log("Dual romance eligible. entry=" + DualState.Entry
                    + ", romanceConfirmed=" + RomanceState.RomanceConfirmed);
            }

            // A route that no longer supports the extension keeps its facts and
            // its saved dual data, but stops receiving new scenes.
            if (DualState.HasHistory && !DualState.Active)
                ModEntry.Log("Dual romance inactive for current route/mother/maid state; data preserved.");
        }

        public static bool CanOpenFromFacts()
        {
            return RomanceState.MotherState == 1
                && (RomanceState.Route == 1 || RomanceState.Route == 2)
                && !RomanceState.RomanceLocked
                && RomanceState.Romance != 4
                && !MaidState.Active
                && !RomanceState.IncompatibleSave
                && !RomanceState.OldBlocked;
        }

        public static bool IsLegacyLover => DualState.Entry == 2 && RomanceState.RomanceConfirmed;
    }
}
