using System;
using System.Linq;
namespace EleanorRomance
{
    public static class DualDirector
    {
        public static readonly string[] MainScenes = { "entry", "dr02", "dr03", "dr04", "dr05", "dr06", "dr07", "dr08", "dr09", "dr10", "coop", "dr11", "dr13", "dr14", "dr15", "dr16", "dr17", "dr18", "dr19", "dr20" };
        private static readonly int[] Days = {49,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67};
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
        public static bool TryQueue(StoreClientManager manager, int day)
        {
            if (manager == null || !DualState.Writable || (!DualState.Active && !CanOfferEntry(day))) return false;
            if (DualState.PendingId != "" || DualState.PendingScene != 0 || RomanceState.LastQueuedDay == day) return false;
            DualProgression.Check();
            string id = NextScene(day);
            if (string.IsNullOrEmpty(id)) return false;
            return Queue(manager, id, day, false);
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
        public static bool CanOfferEntry(int day) => !DualState.HasHistory && DualState.Entry != 3
            && DualMigration.CanOpenFromFacts() && day >= Math.Max(1,49-StoryDirector.StoryOffset());
        public static string NextScene(int day)
        {
            if (!DualState.Writable) return null;
            if (!DualState.HasHistory) return CanOfferEntry(day) ? "entry" : null;
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
            if (DualState.Confirmed && !DualState.Done("dual_epilogue") && day >= DualState.LastDailyDay+3) return "dual_epilogue";
            if (DualRewards.NeedsClaim && day >= DualState.LastDailyDay+4) return "claim";
            if (DualState.Confirmed && day >= DualState.LastDailyDay+4) return "daily";
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
            if(id.StartsWith("wait_",StringComparison.Ordinal))return 104;
            return 0;
        }
    }
}
