using System;
using HarmonyLib;
using UnityEngine;

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
        private static bool Prefix(string key, ref Sprite __result)
        {
            // Bundled beauty portraits replace vanilla keys such as eleanor1.
            if (PortraitCompat.TryBundledBeauty(key, out Sprite beauty))
            {
                __result = beauty;
                return false;
            }
            PortraitCompat.EnsureKey(key);
            return true;
        }
    }

    [HarmonyPatch(typeof(GameOverUIManager), nameof(GameOverUIManager.DisplayUI))]
    internal static class Patch_GameOverDisplayUI
    {
        private static void Prefix(ref string givenReason, ref string givenDescription)
        {
            try
            {
                EndingPages.Page page = EndingPages.TryGet(givenDescription);
                EndingPageUI.SetPage(page);
                if (page == null) return;
                givenReason = L.Get(page.TitleKey);
                givenDescription = page.Text;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Ending page patch failed: " + ex);
                EndingPageUI.Hide();
            }
        }
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.DisplayClientText))]
    internal static class Patch_DisplayClientText
    {
        private static void Prefix(Dialogue dialogue)
        {
            try
            {
                if (dialogue != null) PortraitExpression.Resolve(dialogue);
                StoreClientMono mono = StoreClientMono.Instance;
                if (dialogue != null && dialogue.portrait != null && mono != null && mono.image != null)
                    mono.image.sprite = dialogue.portrait;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Expression portrait failed: " + ex);
            }
        }
    }
}
