using System;
using System.Linq;

namespace EleanorRomance
{
    /// <summary>
    /// Schedules the three solo-romance Act1 visits and keeps the common
    /// post-confession chapters from taking over while the love route owns them.
    /// </summary>
    public static class LoveDirector
    {
        private static bool Ours(StoreClient client)
        {
            return client != null && VanillaEleanorBridge.IsOurClient(client)
                && (client.identifier ?? "").Contains("love_a");
        }

        private static bool HasLoveClient(StoreClientManager manager)
        {
            return Ours(PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint())
                || (manager?.clientStack != null && manager.clientStack.Any(Ours));
        }

        private static bool Conditions
        {
            get
            {
                return LoveState.Writable
                    && RomanceState.StoryActive
                    && !RomanceState.IncompatibleSave
                    && !RomanceState.OldBlocked
                    && RomanceState.Romance == 1
                    && StoryCoreFacts.Done(20)
                    && RomanceState.MotherState == 1
                    && (RomanceState.Route == 1 || RomanceState.Route == 2)
                    && !RomanceState.RomanceLocked
                    && !DualState.HasHistory
                    && !DualState.Active
                    && !MaidState.HasHistory
                    && !MaidState.Active;
            }
        }

        public static bool CanStart(int day)
        {
            if (LoveState.Started || !LoveState.Writable) return false;
            if (!Conditions) return false;
            if (day < Math.Max(1, RomanceState.NextVisitDay)) return false;
            // Let the dual-route entry be offered first; if it is declined or
            // unavailable, the love route starts on a later tick.
            if (DualDirector.CanOfferEntry(day)) return false;
            return true;
        }

        public static void RecoverPending(StoreClientManager manager)
        {
            if (!LoveState.Writable || LoveState.PendingVisit <= 0 || HasLoveClient(manager)) return;
            int reservedDay = LoveState.NextVisitDay;
            LoveState.PendingVisit = 0;
            if (RomanceState.LastQueuedDay == LoveState.Today && reservedDay == LoveState.Today)
                RomanceState.LastQueuedDay = -1;
        }

        public static bool TryQueue(StoreClientManager manager, int day)
        {
            if (manager == null || !LoveState.Writable) return false;
            if (LoveState.Started && !Conditions) return false;
            if (RomanceState.LastQueuedDay == day) return false;

            if (!LoveState.Started)
            {
                if (!CanStart(day)) return false;
                LoveState.Start(day);
            }

            if (!LoveState.Active) return false;

            if (HasLoveClient(manager))
            {
                if (LoveState.PendingVisit == 0) LoveState.PendingVisit = NextVisit(day);
                return false;
            }

            // A save/load can drop an uncommitted client. Let the visit resume.
            if (LoveState.PendingVisit > 0) LoveState.PendingVisit = 0;

            int visit = NextVisit(day);
            if (visit == 0) return false;

            StoreClient client = LoveStory.CreateVisit(visit);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            if (manager.clientStack == null || !manager.clientStack.Contains(client)) return false;
            if (visit == 4) EnsureA2FirstLast(manager);

            LoveState.PendingVisit = visit;
            LoveState.NextVisitDay = day;
            RomanceState.LastQueuedDay = day;
            return true;
        }

        internal static void EnsureA2FirstLast(StoreClientManager manager)
        {
            if (!LoveState.Active || LoveState.A2T1Done) return;
            var stack = manager?.clientStack;
            if (stack == null) return;
            int index = stack.FindIndex(c => c != null
                && c.identifier == "er_story_love_a2_01"
                && VanillaEleanorBridge.IsOurClient(c));
            if (index < 0 || index == stack.Count - 1) return;
            StoreClient client = stack[index];
            stack.RemoveAt(index);
            stack.Add(client); // Already registered with the native queue.
        }

        internal static bool IsA2FirstDue(int day)
        {
            if (!Conditions) return false;
            if (LoveState.PendingVisit == 4) return !SerpicoStoryVisitDue(day);
            if (LoveState.A2T1Done) return false;
            return NextVisit(day) == 4;
        }

        /// <summary>
        /// Serpico's story visits are one-day native clients. The love water
        /// event closes the shop and dismisses the remaining client stack, so
        /// starting love_a2_01 on one of those days can discard Serpico's
        /// document choice or final reward.  Delay the love visit instead of
        /// risking the native storyline.  For the conclusion day, only a real
        /// concluding choice (1/2/3) creates the final client.
        /// </summary>
        internal static bool SerpicoStoryVisitDue(int day)
        {
            try
            {
                if (day == SerpicoStoryline.Serpico1Day
                    || day == SerpicoStoryline.Serpico3Day
                    || day == SerpicoStoryline.Serpico4Day
                    || day == SerpicoStoryline.Serpico5Day
                    || day == SerpicoStoryline.Serpico6Day
                    || day == SerpicoStoryline.LetterDay
                    || day == SerpicoStoryline.OfficerDay
                    || day == SerpicoStoryline.DocumentDay)
                    return true;
                var data = PlayerStore.Instance?.storeClientData;
                if (data == null) return false;
                int choice = data.serpicoDocumentChoice;
                return (choice == 1 || choice == 2 || choice == 3)
                    && day == SerpicoStoryline.ConclusionDay;
            }
            catch
            {
                return false;
            }
        }

        private static int NextVisit(int day)
        {
            if (!LoveState.Started || LoveState.Act3Done) return 0;
            if (day < LoveState.NextVisitDay) return 0;
            if (!LoveState.T1Done) return 1;
            if (!LoveState.T2Done) return 2;
            if (!LoveState.ClosingDone) return 3;
            if (!LoveState.A2T1Done)
                return SerpicoStoryVisitDue(day) ? 0 : 4;
            if (!LoveState.A2T2Done) return 5;
            if (!LoveState.A2T3Done) return 6;
            if (!LoveState.A3T1Done) return 7;
            if (!LoveState.A3T2Done) return 8;
            if (!LoveState.A3T3Done) return 9;
            if (!LoveState.WeddingDone) return 10;
            return 0;
        }

        public static void Complete(int visit)
        {
            LoveState.Complete(visit);
            RomanceState.LastQueuedDay = LoveState.Today;
        }

        public static bool SuppressMainChapter(int chapter, int day)
        {
            if (chapter < 20 || chapter > 24) return false;
            // Once the solo romance is a candidate, keep the common post-C20
            // chapters from pre-empting it even on days with no love visit due.
            if (LoveState.Started) return Conditions;
            return CanStart(day) || (Conditions && !DualState.HasHistory && !MaidState.Active);
        }

        public static bool ForceQueue(int visit)
        {
            var manager = PlayerStore.Instance?.storeClientManager;
            if (manager == null) return false;
            if (!LoveState.Started) LoveState.Start(LoveState.Today);
            StoreClient client = LoveStory.CreateVisit(visit);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddNextClient(client);
            if (manager.clientStack == null || !manager.clientStack.Contains(client)) return false;
            LoveState.PendingVisit = visit;
            return true;
        }
    }
}
