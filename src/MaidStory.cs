using System.Linq;

namespace EleanorRomance
{
    public static class MaidStory
    {
        public static int EventForDay(int day)
        {
            if (!RomanceState.StoryActive) return 0;
            if (MaidState.PendingVisit > 0)
                return day >= MaidState.NextVisitDay ? MaidState.PendingVisit : 0;
            return MaidV2Story.EventForDay(day);
        }

        public static bool TryQueue(StoreClientManager manager, int day)
        {
            int id = EventForDay(day);
            if (id == 0) return false;
            StoreClient client = CreateEvent(id);
            if (client == null) return false;

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            if (manager.clientStack == null || !manager.clientStack.Contains(client)) return false;

            MaidState.PendingVisit = id;
            MaidState.NextVisitDay = day;
            MaidState.LastImportantDay = day;
            RomanceState.LastQueuedDay = day;
            return true;
        }

        public static StoreClient CreateEvent(int id)
        {
            if (id == MaidDividends.VisitId) return MaidDividends.CreateVisit();
            return MaidV2Story.CreateEvent(id) ?? MaidBusiness.CreateEvent(id);
        }

        internal static int WaitingProject()
        {
            int p = !MaidProjects.ResultShown(1) ? 1
                : MaidState.Progress == 23 ? 2
                : MaidState.Progress == 24 ? 3
                : MaidState.Progress == 26 ? 4 : 0;
            return p > 0 && !MaidProjects.Paid(p) && MaidProjects.Available(p) ? p : 0;
        }

        public static bool HasUrgentClient(StoreClientManager manager)
        {
            return manager.clientStack != null && manager.clientStack.Any(c => c != null && !VanillaEleanorBridge.IsOurClient(c)
                && (c.isSentenceOfficer || !c.dismissable && c.clientIntent == StoreClient.ClientIntent.DIALOGUE
                    || c.isSecurity && !string.IsNullOrEmpty(c.eventSourceId)));
        }
    }
}
