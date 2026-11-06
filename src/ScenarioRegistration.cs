using System;

namespace EleanorRomance
{
    public static class ScenarioRegistration
    {
        public static void RegisterIfUnlocked()
        {
            try
            {
                ModHelper.RemoveStartingScenarios(ModConstants.ModId);
                if (!VersionGate.IsSupported || !MetaProgress.IsTrueEndingUnlocked())
                {
                    return;
                }

                ModHelper.AddStartingScenario(
                    ModConstants.ModId,
                    ModConstants.ScenarioId,
                    title: () => L.Get("er_scenario_title"),
                    description: () => L.Get("er_scenario_desc"),
                    startWith: new Func<string>[]
                    {
                        () => L.Get("er_scenario_start_1"),
                        () => L.Get("er_scenario_start_2"),
                        () => L.Get("er_scenario_start_3"),
                        () => L.Get("er_scenario_start_4")
                    },
                    onStart: StoryDirector.ApplyScenarioStart,
                    iconKey: ModConstants.ModId + ":eleanor_locket",
                    tag: () => L.Get("er_scenario_tag"),
                    tagTone: StartingScenario.TagTone.Positive);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to register starting scenario: " + ex);
            }
        }

        public static void Unregister()
        {
            try
            {
                ModHelper.RemoveStartingScenarios(ModConstants.ModId);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to unregister starting scenario: " + ex);
            }
        }

        public static void RefreshAfterUnlock()
        {
            RegisterIfUnlocked();
            StoryDirector.Notify(L.Get("er_notice_true_unlock"));
        }
    }
}
