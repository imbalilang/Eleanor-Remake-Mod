using System;
namespace EleanorRomance
{
    // Actual completed nodes, separate from the pending chapter number.
    public static class StoryCoreFacts
    {
        private static bool Flag(string key) => ModHelper.GetModDataBool(ModConstants.ModId,"er.core."+key,false);
        private static void Flag(string key,bool value) => ModHelper.SetModDataBool(ModConstants.ModId,"er.core."+key,value);
        public static bool Done(int chapter) => Flag("done."+chapter);
        public static void Record(int chapter) => Flag("done."+chapter,true);
        public static bool CooperationDone => Done(21);
        public static bool CooperationClosed => Flag("coop.closed");
        public static bool CooperationPending => Flag("coop.pending") && !CooperationClosed;
        public static bool PerfumePending => Flag("perfume.pending") && !RomanceState.PerfumeGiven;
        public static bool LocketPending => Flag("locket.pending") && !RomanceState.LocketGiven && !RomanceState.LocketSold && !LocketRefused;
        public static void FinishLocket(bool refused){Flag("locket.pending",false);if(refused)RefuseLocket();}
        public static bool LocketRefused => Flag("locket.refused");
        public static void RefuseLocket() => Flag("locket.refused",true);
        public static void ArrangeCooperation() { Record(21); Flag("coop.pending",true); }
        public static void CloseCooperation() { Flag("coop.closed",true); Flag("coop.pending",false); }
        public static void Confess(int relationship) { RomanceState.Romance=relationship; Record(20); }
        public static void FinishCore()
        {
            Flag("perfume.pending",!RomanceState.PerfumeGiven);
            Record(24); StoryDirector.CompleteCore();
        }
        public static void Migrate()
        {
            if (Flag("initialized") || !RomanceState.StoryActive || !DualState.Writable) return;
            bool oldDual=DualState.FeatureVersion==1 && DualState.HasHistory;
            for(int ch=18;ch<=24;ch++)
                if(RomanceState.Chapter>ch && !(oldDual && ch>=21 && ch<=23))Record(ch);
            if(oldDual)
            {
                // v0.81 could jump straight from pending c21 to the completed
                // core. Preserve the route without inventing a cooperation trade.
                if(RomanceState.Chapter>21)DualState.Flag("migration.coop_waived",true);
                if(DualState.Done("dr07"))Record(22);
                if(RomanceState.WorkState>0)Record(23);
                if(DualState.Entry==1 && DualState.Done("dr08") && RomanceState.RomanceConfirmed && RomanceState.LocketWithEleanor
                    && !RomanceState.LocketGiven && !RomanceState.LocketSold)Flag("locket.pending",true);
            }
            if(RomanceState.MotherVisited) Record(18);
            // Historical completion is preserved; do not replay an old order.
            if(Done(21)) CloseCooperation();
            if(Done(24) && !RomanceState.PerfumeGiven)Flag("perfume.pending",true);
            Flag("initialized",true);
        }
        public static void CompleteReplacement(int chapter)
        {
            Record(chapter);
            if(RomanceState.PendingChapter==chapter)RomanceState.PendingChapter=0;
            // Do not leap over cooperation just because work played first.
            if(RomanceState.Chapter==chapter)
            { RomanceState.Chapter=chapter+1; RomanceState.NextVisitDay=DualState.Today+1; }
        }
    }
}
