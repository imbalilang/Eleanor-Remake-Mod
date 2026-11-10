using System;
using HarmonyLib;

namespace EleanorRomance
{
    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.CheckStoryLineEleanor))]
    internal static class Patch_CheckStoryLineEleanor
    {
        private static bool Prefix()
        {
            return !StoryDirector.ShouldSuppressOriginal || VanillaEleanorBridge.AllowOriginalSchedule;
        }
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.AddClient))]
    internal static class Patch_NativeEleanorQueue
    {
        private static bool Prefix(StoreClientManager __instance, StoreClient __0)
            => VanillaEleanorBridge.FilterScheduledClient(__instance, __0);
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.AddDirectSellingItemToTableMiddle))]
    internal static class Patch_NativeEleanorRewardPlacement
    {
        private static bool Prefix(GameItem __0) => !VanillaEleanorBridge.CaptureReward(__0);
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.HandleEleanorEarlyClient))]
    internal static class Patch_HandleEleanorEarlyClient
    {
        private static bool Prefix()
        {
            return !StoryDirector.ShouldSuppressOriginal;
        }
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.HandleEleanorUncleClient))]
    internal static class Patch_HandleEleanorUncleClient
    {
        private static bool Prefix()
        {
            return !StoryDirector.ShouldSuppressOriginal;
        }
    }

    [HarmonyPatch(typeof(SpriteDict), nameof(SpriteDict.GetSprite))]
    internal static class Patch_SpriteDictGetSprite
    {
        private static void Prefix(string key)
        {
            PortraitCompat.EnsureKey(key);
        }
    }

    [HarmonyPatch(typeof(GameOverUIManager), nameof(GameOverUIManager.DisplayUI))]
    internal static class Patch_GameOverDisplayUI
    {
        private static void Prefix(ref string givenDescription)
        {
            try
            {
                if (!RomanceState.StoryActive) return;
                if (RomanceState.Chapter < RomanceState.ChapterComplete && !MaidState.Lovers && !DualState.HasHistory) return;
                if (RomanceState.EpilogueDone && RomanceState.Route == 0) return;

                string extra = RomanceState.Route==4 && RomanceState.BetrayalKind==4
                    ? L.Get("er_maid_ending_exit") : DualOutcomes.Ending(givenDescription) ?? MaidStory.Ending(givenDescription);
                if (extra == null)
                {
                    string block = SelectEndingBlock(givenDescription);
                    extra = string.IsNullOrEmpty(block) ? "" : BlockText(block);
                }
                string world = MaidWorldEvents.Ending();
                if (!string.IsNullOrEmpty(world)) extra += "\n\n" + world;
                if (!string.IsNullOrEmpty(extra))
                {
                    givenDescription = (givenDescription ?? string.Empty) + "\n\n" + extra;
                }
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Ending epilogue patch failed: " + ex);
            }
        }

        private static string SelectEndingBlock(string description)
        {
            string text = description ?? string.Empty;

            if (RomanceState.Route == 4)
                return StoryV06Content.B_END_B007;

            if (ContainsAny(text, "死亡", "死了", "dead", "died", "killed"))
                return StoryV06Content.B_END_B004;

            if (ContainsAny(text, "被捕", "监禁", "失去自由", "prison", "arrest", "captured", "detained"))
                return StoryV06Content.B_END_B003;

            if (RomanceState.MotherState == 2)
                return StoryV06Content.B_END_B006;

            if (RomanceState.MotherState == 1 || RomanceState.MotherState == 3)
            {
                if (RomanceState.RomanceConfirmed)
                {
                    return ContainsAny(text, "离开", "远行", "流放", "leave", "depart", "exile")
                        ? StoryV06Content.B_END_B002
                        : StoryV06Content.B_END_B001;
                }
                return StoryV06Content.B_END_B005;
            }

            return null;
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static string BlockText(string blockId)
        {
            StoryLine[] lines = StoryV06Content.Get(blockId);
            if (lines == null) return string.Empty;
            string result = string.Empty;
            foreach (StoryLine line in lines)
            {
                string text = L.Get(line.Key);
                if (string.IsNullOrEmpty(text)) continue;
                if (result.Length > 0) result += "\n\n";
                result += text;
            }
            return result;
        }
    }
}
