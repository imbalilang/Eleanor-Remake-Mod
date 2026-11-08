using System.Collections.Generic;
using System.Linq;

namespace EleanorRomance
{
    public static class MaidRouteMigration
    {
        /// <summary>Shape evidence, not a guessed universal historical-choice flag.</summary>
        public static string ExclusionReason()
        {
            if (!RomanceState.StoryActive) return "inactive/schema/vanilla";
            if (MaidState.FeatureVersion != 0 || MaidState.LegacyResume != 0 || MaidState.HasHistory) return "feature already initialized";
            if (RomanceState.Chapter != 13) return "chapter";
            if (RomanceState.PendingChapter != 0) return "pending chapter (separate unsupported historical shape)";
            if (RomanceState.Romance != 4 || RomanceState.Route != 0 || RomanceState.BetrayalKind != 0) return "relationship/route";
            if (RomanceState.DriveState != 0 || RomanceState.MotherState != 0 || RomanceState.Savings != 800) return "world facts";
            if (RomanceState.MotherVisited || RomanceState.EpilogueDone || RomanceState.Arrested || VanillaEleanorBridge.RescuePending) return "terminated/rescue";
            var data = PlayerStore.Instance?.storeClientData;
            if (data != null && (data.isEleanorDead || data.eleanorArrested && data.eleanorBailChoice == 0)) return "native rescue/death";
            var current = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            if (current != null && VanillaEleanorBridge.IsOurClient(current)) return "active mod visitor";
            var queue = PlayerStore.Instance?.storeClientManager?.clientStack;
            if (queue != null && queue.Any(c => c != null && VanillaEleanorBridge.IsOurClient(c))) return "queued mod visitor";
            return null;
        }

        public static void OnLoaded()
        {
            if (!RomanceState.StoryActive) return;
            PreserveEstablishedAddress();
            if (MaidState.LegacyResume == 1) return; // Preserve the pending choice and its original due day.
            if (MaidState.FeatureVersion != 0) return;
            string reason = ExclusionReason();
            if (reason == null)
            {
                MaidState.Text("legacy_source", "0.66-compatible-no-payment-callback-shape");
                MaidState.Text("legacy_snapshot", "chapter=13;pending=0;romance=4;route=0;betrayal=0;drive=0;mother=0;savings=800;motherVisited=0;epilogue=0");
                MaidState.LegacyResume = 1;
                MaidState.LegacyDueDay = MaidState.Today + 1;
                MaidState.FeatureVersion = MaidState.CurrentVersion;
                RomanceState.Romance = 0;
                ModEntry.Log("Legacy 800 candidate recorded; dedicated continuation due " + MaidState.LegacyDueDay + ". No money or world facts changed.");
            }
            else
            {
                if (RomanceState.RomanceLocked && RomanceState.Route==0 && RomanceState.BetrayalKind==0
                    && RomanceState.Chapter>=13 && RomanceState.Chapter<RomanceState.ChapterComplete)
                    ModEntry.Log("Legacy 800 continuation excluded: " + reason + "; chapter="+RomanceState.Chapter
                        +"; pending="+RomanceState.PendingChapter+"; drive="+RomanceState.DriveState);
                // Do not claim or migrate unknown/late historical shapes on a subsequent load.
                if (MaidState.FeatureVersion == 0) MaidState.FeatureVersion = MaidState.CurrentVersion;
            }
        }

        // Earlier releases already used the address on entry. Keep that history
        // without replaying chapters, inventing rescue results or charging cash.
        public static void PreserveEstablishedAddress()
        {
            if(!MaidState.HasHistory || MaidState.MotivationVersion!=0)return;
            MaidState.AddressAccepted=true;
            MaidState.MotivationVersion=1;
        }

        /// <summary>
        /// v0.7 removed "let me think again".  A save that chose it is treated as
        /// a confirmed attendant romance; the migrated flag lets reading chapter
        /// 22 skip Lydia's already-played deferred variant.
        /// </summary>
        public static void MigrateDeferred()
        {
            if (!MaidState.Active || MaidState.Confession != 3) return;
            bool lydiaDeferred = RomanceState.Chapter >= 23;
            MaidState.Confession = 1;
            RomanceState.Romance = 1;
            MaidState.Flag("migrated.deferred", true);
            MaidState.Flag("migrated.lydia_deferred", lydiaDeferred);
            ModEntry.Log("Deferred confession migrated to attendant romance; chapter=" + RomanceState.Chapter
                + ", lydia-variant=" + lydiaDeferred + ".");
            NotifyDeferred();
        }

        /// <summary>Retries the one-time migration notice once the game UI exists.</summary>
        public static void NotifyDeferred()
        {
            if (!MaidState.Flag("migrated.deferred") || MaidState.Flag("migrated.notified")) return;
            if (!UiReady()) return;
            MaidState.Flag("migrated.notified", true);
            StoryDirector.Notify(L.Get("er_maid_notice_migrated"));
        }

        private static bool UiReady()
        {
            try { return StoreUIManager.Instance != null; }
            catch { return false; }
        }

        public static void Complete(bool maid)
        {
            if (MaidState.LegacyResume != 1) return;
            if (maid) MaidState.Enter(2);
            else RomanceState.Romance = 0;
            MaidState.LegacyResume = 2;
            MaidState.PendingVisit = 0;
            MaidState.NextVisitDay = -1;
            MaidState.SetInt("legacy_direction", maid ? 1 : 2);
            RomanceState.PendingChapter = 0;
            RomanceState.NextVisitDay = MaidState.Today + 1;
            MaidState.LastImportantDay = MaidState.Today;
            StoryDirector.Notify(L.Get(maid ? "er_maid_notice_entry" : "er_maid_notice_continue"));
        }
    }
}
