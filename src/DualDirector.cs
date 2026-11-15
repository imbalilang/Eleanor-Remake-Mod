using System;
using System.Linq;
namespace EleanorRomance
{
    public static class DualDirector
    {
        public static readonly string[] MainScenes = { "entry", "dr02", "dr03", "dr04", "dr05", "dr06", "dr07", "dr08", "dr09", "dr10", "coop", "dr11", "dr13", "dr14", "dr15", "dr16", "dr17", "dr18", "dr19", "dr20" };
        private static readonly int[] Days = {49,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67};

        // Harem-line v2 visits, one per chapter. Gaps are relative to the previous
        // visit's completion day; dv05 is the same-day evening follow-up.
        private static readonly string[] V2Scenes = { "dv01", "dv02", "dv03", "dv04", "dv05", "dv06", "dv07", "dv08", "dv09", "dv10", "dv11", "dv12", "dv13" };
        private static readonly int[] V2Gaps = { 2, 2, 1, 1, 0, 2, 1, 1, 1, 1, 1, 1, 2 };
        private static readonly string[] LegacyStoryScenes = { "d02", "d03", "d04", "d05", "d06", "d07", "d08", "d09" };

        /// <summary>New harem entries use v2; saves already inside d02-d09 keep the old path.</summary>
        internal static bool UsesV2 => DualState.Flag("d.route")
            && !LegacyStoryScenes.Any(DualState.Done);
        internal static string FinalStoryScene => UsesV2 ? "dv13" : "d09";
        internal static bool RouteStoryFinished => DualState.Flag("d.route") && DualState.Done(FinalStoryScene);
        private static bool Ours(StoreClient c) => c != null && VanillaEleanorBridge.IsOurClient(c) && (c.identifier ?? "").Contains("dual_");
        public static void RecoverPending(StoreClientManager manager)
        {
            if (!DualState.Writable || (DualState.PendingId == "" && DualState.PendingScene == 0)) return;
            if (Ours(PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint())
                || (manager?.clientStack != null && manager.clientStack.Any(Ours))) return;
            int reservedDay=DualState.NextSceneDay;
            DualState.PendingId = ""; DualState.PendingScene = 0;
            DualState.NextSceneDay = -1;
            // No live client survived: its uncommitted scene may resume today.
            if (RomanceState.LastQueuedDay == DualState.Today && reservedDay==DualState.Today) RomanceState.LastQueuedDay = -1;
        }
        internal static bool SameDayFollowUpDue(int day) => UsesV2
            && DualState.Done("dv04") && !DualState.Done("dv05")
            && DualState.SceneDay("dv04") == day && NextScene(day) == "dv05";

        public static bool TryQueue(StoreClientManager manager, int day)
        {
            if (manager == null || !DualState.Writable || (!DualState.Active && !CanOfferEntry(day))) return false;
            if (DualState.PendingId != "" || DualState.PendingScene != 0) return false;
            DualProgression.Check();
            string id = NextScene(day);
            bool sameDayFollowUp = SameDayFollowUpDue(day);
            if (RomanceState.LastQueuedDay == day && !sameDayFollowUp) return false;
            if (id == "d06") return false;
            if (string.IsNullOrEmpty(id) && DualState.Active && DualState.Flag("d.route"))
            {
                if (DualRewards.DueRewrittenReward(day) > 0) id = "reward";
                else if (DualState.Confirmed && RouteStoryFinished && day >= DualState.LastDailyDay + 4)
                    id = "daily";
            }
            if (string.IsNullOrEmpty(id)) return false;
            return Queue(manager, id, day, sameDayFollowUp);
        }
        private static bool Queue(StoreClientManager manager, string id, int day, bool next)
        {
            StoreClient client = DualStory.CreateScene(id);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            if (next) manager.AddNextClient(client); else manager.AddClient(client);
            if (manager.clientStack == null || !manager.clientStack.Contains(client)) return false;
            DualState.PendingId = id; DualState.PendingScene = SceneNumber(id);
            DualState.NextSceneDay = day; RomanceState.LastQueuedDay = day;
            return true;
        }
        public static bool ForceQueue(string id)
        {
            var manager = PlayerStore.Instance?.storeClientManager;
            return manager != null && DualState.Writable && Queue(manager,id,DualState.Today,true);
        }
        private static bool DReady() => RomanceState.DPreludeDUnlocked
            && RomanceState.MotherVisited
            && StoryCoreFacts.Done(19)
            && RomanceState.MotherState == 1
            && (RomanceState.Route == 1 || RomanceState.Route == 2)
            && !MaidState.Active && !RomanceState.RomanceLocked && RomanceState.Romance != 3 && RomanceState.Romance != 4
            && !RomanceState.IncompatibleSave && !RomanceState.OldBlocked;

        public static bool CanOfferEntry(int day) => !LoveState.Started
            && !DualState.HasHistory && DualState.Entry != 3
            && (DReady() || (RomanceState.DPreludeState == 0 && DualMigration.CanOpenFromFacts()))
            && day >= Math.Max(1,49-StoryDirector.StoryOffset());

        internal static void FinishRewrittenCore()
        {
            if (!DualState.Active || !DualState.Flag("d.route") || !RouteStoryFinished || DualState.CoreCommitted) return;
            DualState.CoreTakenOver = true;
            DualState.CoreCommitted = true;
            StoryDirector.CompleteCore();
            DualState.LastDailyDay = DualState.SceneDay(FinalStoryScene);
        }
        public static string NextScene(int day)
        {
            if (!DualState.Writable) return null;
            if (!DualState.HasHistory) return CanOfferEntry(day) ? "entry" : null;
            if (DualState.Flag("d.route"))
            {
                if (UsesV2) return NextV2Scene(day);
                if (!DualState.Flag("d.act2a"))
                {
                    int entryDay = DualState.SceneDay("entry");
                    int due = (entryDay >= 0 ? entryDay : DualState.LastImportantDay) + 3;
                    return day >= due ? "d02" : null;
                }
                if (!DualState.Flag("d.act2"))
                {
                    int d02Day = DualState.SceneDay("d02");
                    int due = (d02Day >= 0 ? d02Day : DualState.LastImportantDay) + 3;
                    return day >= due ? "d03" : null;
                }
                if (!DualState.Done("d04"))
                {
                    int d03Day = DualState.SceneDay("d03");
                    int due = (d03Day >= 0 ? d03Day : DualState.LastImportantDay) + 1;
                    return day >= due ? "d04" : null;
                }
                if (!DualState.Done("d05"))
                {
                    int d04Day = DualState.SceneDay("d04");
                    int due = (d04Day >= 0 ? d04Day : DualState.LastImportantDay) + 5;
                    return day >= due ? "d05" : null;
                }
                if (!DualState.Done("d06"))
                {
                    int d05Day = DualState.SceneDay("d05");
                    int due = (d05Day >= 0 ? d05Day : DualState.LastImportantDay) + 1;
                    return day >= due ? "d06" : null;
                }
                if (!DualState.Done("d07"))
                {
                    int d06Day = DualState.SceneDay("d06");
                    int due = (d06Day >= 0 ? d06Day : DualState.LastImportantDay) + 1;
                    return day >= due ? "d07" : null;
                }
                if (!DualState.Done("d08"))
                {
                    int d07Day = DualState.SceneDay("d07");
                    int due = (d07Day >= 0 ? d07Day : DualState.LastImportantDay) + 3;
                    return day >= due ? "d08" : null;
                }
                if (!DualState.Done("d09"))
                {
                    int d08Day = DualState.SceneDay("d08");
                    int due = (d08Day >= 0 ? d08Day : DualState.LastImportantDay) + 3;
                    return day >= due ? "d09" : null;
                }
                return null;
            }
            if (!DualState.Active) return null;
            for (int i=0; i<MainScenes.Length; i++)
            {
                string id=MainScenes[i];
                if(id=="dr06" && RomanceState.Romance==2)return "resolve";
                if (DualState.Done(id)) continue;
                if (id == "dr03" && StoryCoreFacts.Done(19) || id == "dr05" && RomanceState.RomanceConfirmed
                    || id == "dr07" && StoryCoreFacts.Done(22) || id == "dr08" && StoryCoreFacts.Done(23)
                    || id == "coop" && (StoryCoreFacts.CooperationDone || DualState.Flag("migration.coop_waived")) || id == "dr11" && StoryCoreFacts.Done(24))
                { DualState.Flag("scene."+id,true); if(id=="dr11")DualState.CoreCommitted=true; DualProgression.Check(); continue; }
                int gate = id == "dr02" ? 1 : id == "dr09" ? 2 : id == "dr13" ? 3 : 0;
                if(gate==3 && MaidWorldEvents.NeedsReceipt)return day>=DualState.LastDailyDay+4?"claim":null;
                if (gate != 0 && !DualProgression.Passed(gate)) return DualDaily.Next(day,gate);
                // The original cooperation buyer is a separate actual business visit.
                if (id == "dr11" && !StoryCoreFacts.CooperationClosed) return null;
                int due=Math.Max(Days[i]-StoryDirector.StoryOffset(), DualState.LastImportantDay+1);
                if (day < due) return null;
                return id;
            }
            if (DualRewards.NeedsClaim && day >= DualState.LastDailyDay+4) return "claim";
            if (DualState.Confirmed && day >= DualState.LastDailyDay+4) return "daily";
            return null;
        }
        private static string NextV2Scene(int day)
        {
            string previous = "entry";
            for (int i = 0; i < V2Scenes.Length; i++)
            {
                string id = V2Scenes[i];
                if (DualState.Done(id)) { previous = id; continue; }
                int baseDay = DualState.SceneDay(previous);
                if (baseDay < 0) baseDay = DualState.LastImportantDay;
                return day >= baseDay + V2Gaps[i] ? id : null;
            }
            return null;
        }
        public static bool SuppressMainChapter(int chapter) => DualState.Active && chapter >= 19 && chapter <= 24;
        public static void ReconcileMainProgression(int day)
        {
            if (!DualState.Writable || !DualState.HasHistory || DualState.Active) return;
            int chapter=RomanceState.Chapter;
            while (chapter >= 19 && chapter <= 24 && StoryCoreFacts.Done(chapter)) chapter++;
            if (chapter > 24 && RomanceState.Chapter < RomanceState.ChapterComplete) StoryDirector.CompleteCore();
            else if (chapter != RomanceState.Chapter && chapter <=24)
            { RomanceState.Chapter=chapter; RomanceState.PendingChapter=0; RomanceState.NextVisitDay=day+1; }
        }
        public static void CompleteScene(string id, Action flags = null)
        {
            if (!DualState.Writable) return;
            flags?.Invoke(); DualState.Complete(id);
            foreach(string key in new System.Collections.Generic.List<string>(ModHelper.GetModDataKeys(ModConstants.ModId)))
                if(key.StartsWith("er.dual.cursor.",StringComparison.Ordinal))ModHelper.SetModData(ModConstants.ModId,key,"");
            DualState.PendingId=""; DualState.PendingScene=0; DualState.NextSceneDay=-1;
            if (id=="entry") DualState.Complete("dr01");
            if (id=="dr11") DualState.Complete("dr12");
            DualProgression.Check();
            ReconcileMainProgression(DualState.Today);
        }
        public static int SceneNumber(string id)
        {
            int n=Array.IndexOf(MainScenes,id);
            if(n>=0)return n+1;
            if(id=="daily")return 101;
            if(id=="dual_epilogue")return 102;
            if(id=="claim")return 103;
            if(id=="reward")return 105;
            if(id.StartsWith("wait_",StringComparison.Ordinal))return 104;
            if(id=="d02")return 201;
            if(id=="d03")return 202;
            if(id=="d04")return 203;
            if(id=="d05")return 204;
            if(id=="d06")return 205;
            if(id=="d07")return 206;
            if(id=="d08")return 207;
            if(id=="d09")return 208;
            int v2=Array.IndexOf(V2Scenes,id);
            if(v2>=0)return 210+v2;
            return 0;
        }
    }
}
