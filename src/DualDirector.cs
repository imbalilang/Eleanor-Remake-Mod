using System;
using System.Collections.Generic;
using System.Linq;

namespace EleanorRomance
{
    /// <summary>
    /// Schedules the twenty dual-romance scenes. The extension owns its own
    /// progress; it does not fake main-chapter completion or replay content.
    /// </summary>
    public static class DualDirector
    {
        private sealed class SceneDef
        {
            public string Id;
            public int Day;
            public string Requires;
            public bool Always;
            public SceneDef(string id, int day, string requires = null, bool always = false)
            {
                Id = id; Day = day; Requires = requires; Always = always;
            }
        }

        // dr01+dr02 are one visit; dr11+dr12 are one visit.
        private static readonly SceneDef[] Scenes =
        {
            new SceneDef("entry", 49),
            new SceneDef("dr03", 50, "entry"),
            new SceneDef("dr04", 51, "dr03"),
            new SceneDef("dr05", 52, "dr04"),
            new SceneDef("dr06", 53, "dr05"),
            new SceneDef("dr07", 54, "dr06"),
            new SceneDef("dr08", 55, "dr07"),
            new SceneDef("dr09", 56, "dr08"),
            new SceneDef("dr10", 57, "dr09"),
            new SceneDef("dr11", 59, "dr10"),
            new SceneDef("dr13", 60, "dr11"),
            new SceneDef("dr14", 61, "dr13"),
            new SceneDef("dr15", 62, "dr14"),
            new SceneDef("dr16", 63, "dr15"),
            new SceneDef("dr17", 64, "dr16"),
            new SceneDef("dr18", 65, "dr17"),
            new SceneDef("dr19", 66, "dr18"),
            new SceneDef("dr20", 67, "dr19"),
        };

        /// <summary>
        /// A save made in the middle of a dual visit must not leave the
        /// extension permanently "pending". If no dual client survived the
        /// load, clear the marker so the same scene can be queued again.
        /// </summary>
        public static void RecoverPending(StoreClientManager manager)
        {
            if (DualState.PendingScene == 0) return;
            bool present = false;
            StoreClient current = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            if (current != null && VanillaEleanorBridge.IsOurClient(current)
                && !string.IsNullOrEmpty(current.identifier) && current.identifier.Contains("dual_"))
                present = true;
            if (!present && manager?.clientStack != null)
                present = manager.clientStack.Any(c => c != null
                    && VanillaEleanorBridge.IsOurClient(c)
                    && !string.IsNullOrEmpty(c.identifier) && c.identifier.Contains("dual_"));
            if (!present)
            {
                DualState.PendingScene = 0;
                ModEntry.Log("Dual pending scene recovered after load; it will be queued again.");
            }
        }

        public static bool TryQueue(StoreClientManager manager, int day)
        {
            if (manager == null) return false;
            if (!DualState.Active && !CanOfferEntry(day)) return false;
            if (DualState.PendingScene != 0) return false;
            if (DualState.Relation == 3) return false;
            if (DualState.LastImportantDay == day || RomanceState.LastQueuedDay == day) return false;

            string id = NextScene(day);
            if (string.IsNullOrEmpty(id)) return false;
            StoreClient client = DualStory.CreateScene(id);
            if (client == null) return false;

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            if (manager.clientStack == null || !manager.clientStack.Contains(client)) return false;

            DualState.PendingScene = SceneNumber(id);
            DualState.NextSceneDay = day;
            DualState.LastImportantDay = day;
            RomanceState.LastQueuedDay = day;
            ModEntry.Log("Dual scene queued: " + id + " on day " + day + ".");
            return true;
        }

        /// <summary>Test-only shortcut used by the F9 panel; bypasses the day gate.</summary>
        public static bool ForceQueue(string id)
        {
            StoreClientManager manager = PlayerStore.Instance?.storeClientManager;
            if (manager == null) return false;
            StoreClient client = DualStory.CreateScene(id);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddNextClient(client);
            DualState.PendingScene = SceneNumber(id);
            DualState.LastImportantDay = DualState.Today;
            ModEntry.Log("Dual debug scene queued: " + id);
            return true;
        }

        public static bool CanOfferEntry(int day)
        {
            if (!DualMigration.CanOpenFromFacts()) return false;
            if (DualState.HasHistory || DualState.Entry == 3) return false;
            return day >= Adjust(49);
        }

        public static string NextScene(int day)
        {
            if (!DualState.HasHistory)
                return CanOfferEntry(day) ? "entry" : null;

            if (!DualState.Done("entry") && DualState.Entry != 3)
                return "entry";
            if (DualState.Relation == 3) return null;
            if (!DualState.Done("dr03")) return Due("dr03", day, null);
            if (!DualState.Done("dr04")) return Due("dr04", day, "dr03");
            if (!DualState.Done("dr05"))
            {
                // Existing lovers already have the ordinary confession. Do
                // not replay it just to unlock the extension.
                if (DualState.Entry == 2 || RomanceState.RomanceConfirmed)
                {
                    DualState.Flag("scene.dr05", true);
                }
                else return Due("dr05", day, "dr04");
            }
            if (!DualState.Done("dr06")) return Due("dr06", day, "dr05");
            if (!DualState.Done("dr07")) return Due("dr07", day, "dr06");
            if (!DualState.Done("dr08")) return Due("dr08", day, "dr07");
            if (DualState.FeedbackPending && !DualState.FeedbackShown && !DualState.Done("reputation")
                && day > DualState.LastImportantDay)
                return "reputation";
            if (!DualState.Done("dr09"))
            {
                if (!RomanceState.RomanceConfirmed) return null;
                return Due("dr09", day, "dr08");
            }
            if (!DualState.Done("dr10")) return Due("dr10", day, "dr09");
            if (!DualState.Done("dr11"))
            {
                // Do not outrun the common core and silently skip the original
                // cooperation visit (running chapter 21). Let it play first.
                if (RomanceState.Chapter < 21 && RomanceState.Chapter < RomanceState.ChapterComplete)
                    return null;
                return Due("dr11", day, "dr10");
            }
            if (!DualState.Done("dr13")) return Due("dr13", day, "dr11");
            if (!DualState.Done("dr14")) return Due("dr14", day, "dr13");
            if (!DualState.Done("dr15")) return Due("dr15", day, "dr14");
            if (!DualState.Done("dr16")) return Due("dr16", day, "dr15");
            if (!DualState.Done("dr17")) return Due("dr17", day, "dr16");
            if (DualState.Relation == 3) return null;
            if (!DualState.Done("dr18")) return Due("dr18", day, "dr17");
            if (!DualState.Done("dr19")) return Due("dr19", day, "dr18");
            if (!DualState.Done("dr20")) return Due("dr20", day, "dr19");
            if (DualState.Confirmed && !DualState.Done("dual_epilogue")
                && day >= DualState.LastDailyDay + 3) return "dual_epilogue";
            if (DualState.Confirmed && day >= DualState.LastDailyDay + 4) return "daily";
            return null;
        }

        private static string Due(string id, int day, string requires)
        {
            if (!string.IsNullOrEmpty(requires) && !DualState.Done(requires)) return null;
            SceneDef def = Scenes.FirstOrDefault(s => s.Id == id);
            if (def == null) return null;
            int due = Adjust(def.Day);
            int last = DualState.LastImportantDay;
            if (last >= 0) due = Math.Max(due, last + 1);
            return day >= due ? id : null;
        }

        private static int Adjust(int day)
        {
            return Math.Max(1, day - StoryDirector.StoryOffset());
        }

        /// <summary>The dual route replaces main chapters 22–24; chapter 21's cooperation buyer stays available.</summary>
        public static bool SuppressMainChapter(int chapter)
        {
            if (!DualState.Active) return false;
            // Before the confession scene resolves, do not freeze the main
            // route behind a dual visit that may be waiting for its original
            // qualification. The replacement takes over only after dr05.
            if (DualState.Entry != 2 && !DualState.Done("dr05")) return false;
            if (DualState.Relation == 3) return chapter == 22 || chapter == 23;
            if (!RomanceState.RomanceConfirmed && DualState.Done("dr08"))
                return chapter == 22 || chapter == 23;
            return chapter >= 22 && chapter <= 24;
        }

        /// <summary>
        /// If the dual romance ended or its ordinary confession is still
        /// deferred, do not leave the main route stuck behind the replaced
        /// chapters 22–23. Skip them once the replacements have played.
        /// </summary>
        public static void ReconcileMainProgression(int day)
        {
            if (!DualState.Active) return;
            bool ended = DualState.Relation == 3;
            bool deferred = !RomanceState.RomanceConfirmed && DualState.Done("dr08");
            if (!ended && !deferred) return;
            if (RomanceState.Chapter >= 22 && RomanceState.Chapter < 24)
            {
                RomanceState.Chapter = 24;
                RomanceState.PendingChapter = 0;
                if (RomanceState.NextVisitDay < 0 || RomanceState.NextVisitDay > day + 1)
                    RomanceState.NextVisitDay = day + 1;
                ModEntry.Log("Dual route ended/deferred; main chapters 22-23 skipped, chapter 24 can resume.");
            }
        }

        public static void CompleteScene(string id, Action flags = null)
        {
            flags?.Invoke();
            DualState.Complete(id);
            DualState.PendingScene = 0;
            DualState.NextSceneDay = -1;
            if (id == "entry")
            {
                DualState.SetSceneDay("dr01", DualState.Today);
                DualState.SetSceneDay("dr02", DualState.Today);
                DualState.Complete("dr01");
                DualState.Complete("dr02");
                if (DualState.Entry == 2) DualState.Complete("legacy_entry");
            }
            if (id == "dr11") DualState.Complete("dr12");
            ModEntry.Log("Dual scene complete: " + id + ".");
        }

        public static int SceneNumber(string id)
        {
            if (id == "entry") return 1;
            int n;
            if (id.StartsWith("dr", StringComparison.Ordinal) && int.TryParse(id.Substring(2), out n)) return n;
            return 100 + Math.Abs(id.GetHashCode() % 100);
        }
    }
}
