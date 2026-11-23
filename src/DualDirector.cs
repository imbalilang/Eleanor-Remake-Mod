using System;
using System.Linq;
namespace EleanorRomance
{
    public static class DualDirector
    {
        // Harem-line v2 visits, one per chapter. Gaps are relative to the previous
        // visit's completion day; dv05 is the same-day evening follow-up.
        private static readonly string[] V2Scenes = { "dv01", "dv02", "dv03", "dv04", "dv05", "dv06", "dv07", "dv08", "dv09", "dv10", "dv11", "dv12", "dv13" };
        private static readonly int[] V2Gaps = { 2, 2, 1, 1, 0, 2, 1, 1, 1, 1, 1, 1, 2 };
        private static int warnedFuture = -1;

        internal static bool RouteStoryFinished => DualState.Done("dv13");

        internal static void OnLoaded()
        {
            if (!RomanceState.StoryActive) return;
            if (!DualState.Writable)
            {
                if (warnedFuture == DualState.FeatureVersion) return;
                warnedFuture = DualState.FeatureVersion;
                ModEntry.Log("Harem story paused: save format " + warnedFuture);
                StoryDirector.Notify(L.Get("er_dual_future_schema", warnedFuture));
                return;
            }
            FinishRewrittenCore();
        }

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
        internal static bool SameDayFollowUpDue(int day) => DualState.Active
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
            if (id == DualClosingDay.Scene)
            {
                // The paid closure is queued from the native morning customer
                // generation hook (StoryDirector.OnGenerateCustomerVeryEarly),
                // not from the daily pass. This matches Lou's special visit.
                return false;
            }
            if (string.IsNullOrEmpty(id) && DualState.Active)
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
        private static bool HasEntryPrerequisites() => RomanceState.DPreludeDUnlocked
            && RomanceState.MotherVisited
            && StoryCoreFacts.Done(19)
            && RomanceState.MotherState == 1
            && (RomanceState.Route == 1 || RomanceState.Route == 2)
            && !MaidState.Active && !RomanceState.RomanceLocked && RomanceState.Romance != 3 && RomanceState.Romance != 4
            && RomanceState.StoryActive;

        public static bool CanOfferEntry(int day) => !LoveState.Started
            && !DualState.HasHistory && DualState.Entry != 3
            && HasEntryPrerequisites()
            && day >= Math.Max(1,49-StoryDirector.StoryOffset());

        internal static void FinishRewrittenCore()
        {
            if (!DualState.Active || !RouteStoryFinished || DualState.CoreCommitted) return;
            DualState.CoreCommitted = true;
            StoryEngine.GiveSelfRefillingPerfume();
            StoryDirector.CompleteCore();
            DualState.LastDailyDay = DualState.SceneDay("dv13");
        }
        public static string NextScene(int day)
        {
            if (!DualState.Writable) return null;
            if (!DualState.HasHistory) return CanOfferEntry(day) ? "entry" : null;
            if (!DualState.Active) return null;
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
            DualProgression.Check();
            ReconcileMainProgression(DualState.Today);
        }
        public static int SceneNumber(string id)
        {
            if (id == "entry") return 1;
            if (id == "daily") return 101;
            if (id == "reward") return 105;
            int v2=Array.IndexOf(V2Scenes,id);
            if(v2>=0)return 210+v2;
            return 0;
        }
    }
}
