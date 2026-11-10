namespace EleanorRomance
{
    public static class DualMigration
    {
        private static int warnedFuture = -1;
        public static void OnLoaded()
        {
            DualShowcaseEffects.OnLoaded();
            if (!RomanceState.StoryActive) return;
            if(!DualState.Writable)
            {
                if(warnedFuture!=DualState.FeatureVersion)
                {warnedFuture=DualState.FeatureVersion;ModEntry.Log("Dual paused: future schema "+warnedFuture);StoryDirector.Notify(L.Get("er_dual_future_schema",warnedFuture));}
                return;
            }
            if (DualState.FeatureVersion == DualState.CurrentVersion) return;
            int oldVersion = DualState.FeatureVersion;
            bool oldAuto = oldVersion == 1 && DualState.Entry == 2;
            // v0.81 auto-wrote dr05: it is not evidence of consent.
            bool substantive = DualState.Done("entry") || DualState.Done("dr03")
                || DualState.Done("dr04") || DualState.Done("dr06") || DualState.Done("dr07")
                || DualState.Done("dr08") || DualState.Done("dr09") || DualState.Done("dr10")
                || DualState.Done("dr11") || DualState.Done("dr13") || DualState.Relation == 2;
            if (oldAuto && !substantive)
            {
                DualState.Flag("migration.auto_entry_removed", true);
                DualState.Flag("migration.consent_pending", true);
                DualState.Entry = 0;
                DualState.Flag("scene.dr05", false);
            }
            else if (DualState.HasHistory) DualState.Flag("entry.explicit", true);
            DualState.FeatureVersion = DualState.CurrentVersion;
            DualState.MigrationVersion = DualState.CurrentVersion;
            DualState.PendingId = ""; DualState.PendingScene = 0;
            if (DualState.Done("entry")) DualState.Flag("scene.dr02", true);
            if (DualState.HasHistory)
                for (int s = 1; s <= 3; s++)
                    if (DualState.Done(s == 1 ? "dr02" : s == 2 ? "dr09" : "dr13"))
                        DualState.Flag("gate." + s + ".waived", true);
            if (DualState.PlaqueReceived) DualState.Flag("reward.1.earned", true);
            DualState.Flag("biz.legacy_second", DualState.SecondBusiness == 1 || DualState.SecondBusiness == 2);
            DualState.FirstBusiness = 4;
            if (!DualState.Flag("biz.legacy_second")) DualState.SecondBusiness = 4;
            DualState.FeedbackPending = false;
            if (DualState.Relation == 2) DualState.Flag("scav.transferred", true);
            DualProgression.Check();
            ModEntry.Log("Dual schema migrated " + oldVersion + " -> 2; world facts preserved.");
        }
        public static bool CanOpenFromFacts()
        {
            return DualState.Writable && RomanceState.StoryActive && RomanceState.MotherVisited
                && RomanceState.MotherState == 1 && (RomanceState.Route == 1 || RomanceState.Route == 2)
                && !RomanceState.RomanceLocked && RomanceState.Romance != 4 && RomanceState.Romance != 3
                && !MaidState.Active && !RomanceState.IncompatibleSave && !RomanceState.OldBlocked
                && (!RomanceState.RomanceConfirmed || DualState.Flag("migration.consent_pending"))
                && (RomanceState.RomanceConfirmed || StoryEngine.CanConfess());
        }
        public static bool IsLegacyLover => DualState.Entry == 2 && RomanceState.RomanceConfirmed;
    }
}
