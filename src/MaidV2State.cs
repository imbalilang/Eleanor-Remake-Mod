namespace EleanorRomance
{
    /// <summary>Additive state for the maid v2 rewrite; old FeatureVersion==1 saves stay on v1.</summary>
    public static class MaidV2State
    {
        private const string P = "v2.";
        public static bool Enabled => MaidState.FeatureVersion == 2;
        public static bool GapPaid { get => MaidState.Flag(P + "gap_paid"); set => MaidState.Flag(P + "gap_paid", value); }
        public static bool TradeSold { get => MaidState.Flag(P + "trade_sold"); set => MaidState.Flag(P + "trade_sold", value); }
        public static int GapChoice { get => MaidState.Int(P + "gap_choice"); set => MaidState.SetInt(P + "gap_choice", value); }
        public static int GapAmount { get => MaidState.Int(P + "gap_amount"); set => MaidState.SetInt(P + "gap_amount", value); }
        public static bool CoWork { get => MaidState.Flag(P + "cowork"); set => MaidState.Flag(P + "cowork", value); }
        public static bool Act2Done { get => MaidState.Flag(P + "act2_done"); set => MaidState.Flag(P + "act2_done", value); }
        public static bool Act3Done { get => MaidState.Flag(P + "act3_done"); set => MaidState.Flag(P + "act3_done", value); }
        public static bool Residency { get => MaidState.Flag(P + "residency"); set => MaidState.Flag(P + "residency", value); }
        public static bool GalaInvited { get => MaidState.Flag(P + "gala_invited"); set => MaidState.Flag(P + "gala_invited", value); }
        public static bool Ended { get => MaidState.Flag(P + "ended"); set => MaidState.Flag(P + "ended", value); }

        public static void Enter()
        {
            MaidState.FeatureVersion = 2;
            MaidState.EntryKind = 3;
            MaidState.EntryPaidCash = GapAmount;
            MaidState.Contract = 1;
            MaidState.MotivationVersion = 1;
            MaidState.Progress = 12;
            MaidState.ProgressDay = MaidState.Today;
            MaidState.LastImportantDay = MaidState.Today;
            CoWork = true;
            RomanceState.Romance = 0;
        }
    }
}
