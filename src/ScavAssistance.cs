namespace EleanorRomance
{
    /// <summary>The existing reward flag unlocks a native, one-day landfill event.</summary>
    public static class ScavAssistance
    {
        public const string EventId = "er_eleanor_scav_assistance";
        public const float DailyChance = 70f;
        public const int ExtraAttempts = 2;

        public static bool Eligible => RomanceState.StoryActive && RomanceState.ScavAssist && !DualState.Flag("scav.transferred");

        // Registration only stores definitions and deferred providers. It is safe
        // in the mod menu, where the lazy PlayerStore singleton must not be read.
        public static void Register()
        {
            ModHelper.AddAfterhourEvent(ModConstants.ModId, EventId, AfterhourLocation.DUMPING_GROUND,
                title: () => L.Get("er_scav_event_title"), durationDays: 1,
                description: () => L.Get("er_scav_event_desc"),
                effects: new[]
                {
                    ModHelper.AfterhourEffect(() => L.Get("er_scav_event_attempts"),
                        ScavHelper.MODIFIER_SCAV_ATTEMPTS, ExtraAttempts, AfterhourEffectPolarity.Good),
                    ModHelper.AfterhourEffect(() => L.Get("er_scav_event_safe"),
                        ScavHelper.MODIFIER_INJURY_IMMUNITY, 1f, AfterhourEffectPolarity.Good)
                },
                dailyChancePercent: DailyChance,
                canStart: () => Eligible && ScavHelper.GetMaxScavAttempts() > 0);
        }

        public static void Unregister() => ModHelper.RemoveAfterhourEvents(ModConstants.ModId);
    }
}
