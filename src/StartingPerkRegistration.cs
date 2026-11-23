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
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to unregister Early Start perk: " + ex);
            }
        }

        public static bool IsEarlyStartRun()
        {
            return ModHelper.IsStartingPerkActive(ModConstants.EarlyStartPerkId);
        }
    }
}
