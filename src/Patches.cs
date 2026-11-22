using System;
using HarmonyLib;
using UnityEngine;

namespace EleanorRomance
{
    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.CheckStoryLineEleanor))]
    internal static class Patch_CheckStoryLineEleanor
    {
        private static bool Prefix(StoreClientManager __instance)
        {
            if (!StoryDirector.ShouldSuppressOriginal) return true;
            VanillaEleanorBridge.ScheduleRescue(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.AddClient))]
    internal static class Patch_NativeEleanorQueue
    {
        // Live takeover is handled at the native scheduler methods; this only
        // prepares the bail/freed rescue pair and blocks a duplicate while one
        // is already active.  Other clients (including other mods' events) pass.
        private static bool Prefix(StoreClientManager __instance, StoreClient __0)
            => VanillaEleanorBridge.PrepareScheduledClient(__instance, __0);
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.AddNextClient))]
    internal static class Patch_NativeEleanorQueueNext
    {
        // Same rescue preparation for the secondary queue path.
        private static bool Prefix(StoreClientManager __instance, StoreClient __0)
            => VanillaEleanorBridge.PrepareScheduledClient(__instance, __0);
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetNegociatedValue))]
    internal static class Patch_PreludeBraceletPrice
    {
        private static bool Prefix(GameItem __instance, ref long __result)
        {
            // Only the two story buyers make a fixed offer for this instance.
            if (__instance.identifier != ModConstants.BraceletItemId) return true;
            var client = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            int price = EleanorStory.PreludeFixedPrice(client, __instance);
            if (price <= 0) return true;
            __result = price;
            return false;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(GameItem __instance, ref long __result)
        {
            // Other mods (e.g. Agri Expansion's uniform trade bonus) may scale
            // prices in a postfix. Re-assert the story's fixed offer last so the
            // prelude's exact-price state check cannot be broken.
            if (__instance == null || __instance.identifier != ModConstants.BraceletItemId) return;
            var client = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            int price = EleanorStory.PreludeFixedPrice(client, __instance);
            if (price > 0) __result = price;
        }
    }

    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.StartArrestSequence))]
    internal static class Patch_PresentationOnArrest
    {
        private static void Prefix()
        {
            StoryPresentation.Clear();
        }
    }

    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.StartShootoutSequence))]
    internal static class Patch_PresentationOnShootout
    {
        private static void Prefix() => StoryPresentation.Clear();
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.DismissCurrentClient))]
    internal static class Patch_PresentationOnDeparture
    {
        private static void Prefix(PlayerStore __instance)
        {
            if (StoryPresentation.IsShown)
                StoryPresentation.OnCustomerLeaving(__instance.currentClientInstance?.GetClientBlueprint());
        }
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
        // Bridge our own local/locally bundled keys, then replace only native
        // Eleanor keys whose result is still vanilla art.  Another mod's key or
        // already-applied foreign texture is left alone.
        private static void Prefix(string key) => PortraitCompat.EnsureKey(key);

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(string key, ref Sprite __result)
        {
            if (PortraitCompat.TryNativeEleanorOverride(key, __result, out Sprite sprite))
                __result = sprite;
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.GetNextClient))]
    internal static class Patch_EnforceNextClientPortrait
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix() => PortraitCompat.EnforceCooperativeClientPortrait();
    }

    [HarmonyPatch(typeof(StoreClientMono), nameof(StoreClientMono.OnArrived))]
    internal static class Patch_EnforceArrivedPortrait
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix() => PortraitCompat.EnforceCooperativeClientPortrait();
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
                if (dialogue == null) return;
                // If another portrait mod currently owns the visible image,
                // leave it alone and do not switch expressions on top of it.
                if (PortraitCompat.IsForeignPortraitOnCurrentClient()) return;
                Sprite portrait = PortraitExpression.Resolve(dialogue);
                if (portrait == null)
                {
                    // Never let a foreign Dialogue.portrait flash on Eleanor
                    // before the final postfix restores our own art.
                    StoreClient current = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
                    if (!PortraitCompat.IsEleanorClient(current)) portrait = dialogue.portrait;
                }
                if (portrait == null) return;
                StoreClientMono mono = StoreClientMono.Instance;
                if (mono != null && mono.image != null) mono.image.sprite = portrait;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Expression portrait failed: " + ex);
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Dialogue dialogue) => PortraitCompat.EnforceCooperativeClientPortrait(dialogue);
    }
}
