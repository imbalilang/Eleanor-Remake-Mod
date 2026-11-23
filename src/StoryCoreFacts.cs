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
    }
}
