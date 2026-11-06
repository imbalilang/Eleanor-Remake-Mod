using UnityEngine;

namespace EleanorRomance
{
    public static class DebugTools
    {
        private static float resetArmedUntil = -1f;

        public static void Register()
        {
            ModHelper.AddDebugFunction(
                ModConstants.ModId,
                ModHelper.DEBUG_CATEGORY_COMMAND,
                "重置埃莉诺剧情线 / Reset Eleanor story",
                ResetStory,
                tall: true);

            ModHelper.AddDebugFunction(
                ModConstants.ModId,
                ModHelper.DEBUG_CATEGORY_COMMAND,
                "立即触发下一章埃莉诺剧情 / Queue next Eleanor chapter",
                QueueNextChapter);

            ModHelper.AddDebugFunction(
                ModConstants.ModId,
                ModHelper.DEBUG_CATEGORY_COMMAND,
                "埃莉诺好感 +5 / Eleanor affection +5",
                AddAffection);

            ModHelper.AddDebugFunction(
                ModConstants.ModId,
                ModHelper.DEBUG_CATEGORY_COMMAND,
                "解锁旧日回响开局（测试） / Unlock Old Echo (test)",
                UnlockScenario);
        }

        public static void Unregister()
        {
            try
            {
                ModHelper.RemoveDebugFunctions(ModConstants.ModId);
            }
            catch (System.Exception ex)
            {
                ModEntry.LogError("Failed to remove debug functions: " + ex);
            }
        }

        private static void ResetStory()
        {
            if (Time.realtimeSinceStartup < resetArmedUntil)
            {
                resetArmedUntil = -1f;
                RomanceState.ManualResetOldSave();
            }
            else
            {
                resetArmedUntil = Time.realtimeSinceStartup + 10f;
                StoryDirector.Notify(L.Get("er_notice_reset_arm"));
            }
        }

        private static void QueueNextChapter()
        {
            if (!RomanceState.StoryActive)
            {
                StoryDirector.Notify(L.Get("er_notice_story_inactive"));
                return;
            }

            if (StoreStation.Instance != null)
            {
                RomanceState.NextVisitDay = StoreStation.Instance.dayCounter;
                StoryDirector.Notify(L.Get("er_notice_queue_now"));
            }
        }

        private static void AddAffection()
        {
            if (!RomanceState.StoryActive)
            {
                StoryDirector.Notify(L.Get("er_notice_story_inactive"));
                return;
            }

            RomanceState.AddAffection(5);
            StoryDirector.Notify("Affection: " + RomanceState.Affection);
        }

        private static void UnlockScenario()
        {
            MetaProgress.UnlockTrueEnding();
            ScenarioRegistration.RefreshAfterUnlock();
        }
    }
}
