namespace EleanorRomance
{
    /// <summary>Route-local state for the revolution v2 rewrite; old RevVersion==1 stays on RevolutionStory.</summary>
    public static class RevolutionV2State
    {
        private const string P = "revv2.";
        public static bool Enabled => RomanceState.Route == 3 && RomanceState.RevVersion >= 2;
        public static bool TeamLoss { get => ModHelper.GetModDataBool(ModConstants.ModId, P + "team_loss", false); set => ModHelper.SetModDataBool(ModConstants.ModId, P + "team_loss", value); }
        public static bool FirstKill { get => ModHelper.GetModDataBool(ModConstants.ModId, P + "first_kill", false); set => ModHelper.SetModDataBool(ModConstants.ModId, P + "first_kill", value); }
        public static bool Expedition { get => ModHelper.GetModDataBool(ModConstants.ModId, P + "expedition", false); set => ModHelper.SetModDataBool(ModConstants.ModId, P + "expedition", value); }
        public static bool Ended { get => ModHelper.GetModDataBool(ModConstants.ModId, P + "ended", false); set => ModHelper.SetModDataBool(ModConstants.ModId, P + "ended", value); }
        public static int Progress { get => ModHelper.GetModDataInt(ModConstants.ModId, P + "progress", 0); set => ModHelper.SetModDataInt(ModConstants.ModId, P + "progress", value); }
    }
}
