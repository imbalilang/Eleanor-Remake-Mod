using System;

namespace EleanorRomance
{
    public static class StartingPerkRegistration
    {
        private const string IconKey = ModConstants.ModId + ":eleanor_locket";

        public static void Register()
        {
            try
            {
                // Keep the old scenario resolvable by the save browser without listing it for new games.
                StartingScenario.Register(new StartingScenario
                {
                    id = ModConstants.LegacyScenarioId,
                    startType = NewGameData.StartType.MODDED,
                    source = ModConstants.ModId,
                    titleProvider = () => L.Get("er_early_start_name"),
                    iconKey = IconKey,
                    hidden = true
                });

                if (!VersionGate.IsSupported) return;

                ModHelper.AddStartingPerk(
                    ModConstants.ModId,
                    ModConstants.EarlyStartPerkId,
                    cost: 0,
                    type: StartingPerk.StartingPerkType.NEUTRAL,
                    name: () => L.Get("er_early_start_name"),
                    description: () => L.Get("er_early_start_desc"),
                    iconKey: IconKey);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to register Early Start perk: " + ex);
            }
        }

        public static void Unregister()
        {
            try
            {
                ModHelper.RemoveStartingPerks(ModConstants.ModId);
                ModHelper.RemoveStartingScenarios(ModConstants.ModId);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to unregister Early Start perk: " + ex);
            }
        }

        public static bool IsEarlyStartRun()
        {
            return ModHelper.IsStartingPerkActive(ModConstants.EarlyStartPerkId)
                || ModHelper.IsStartingScenario(ModConstants.LegacyScenarioId);
        }
    }
}
