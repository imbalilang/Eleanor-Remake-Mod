using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>
    /// Keeps the prelude bracelet count in sync with actual trade events. The
    /// count is the route fact; inventory lookups are only used for cleanup.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.OnItemBought))]
    internal static class PreludeBraceletAcquiredPatch
    {
        private static void Postfix(GameItem __0)
            => EleanorStory.NotePreludeBraceletAcquired(__0);
    }

    [HarmonyPatch(typeof(StoreClient), nameof(StoreClient.OnItemBought))]
    internal static class PreludeBraceletReleasedPatch
    {
        private static void Postfix(GameItem __0)
            => EleanorStory.NotePreludeBraceletReleased(__0);
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.BuyAllItemFromTable))]
    internal static class PreludeBraceletBuyAllPatch
    {
        private static void Prefix(out List<GameItem> __state)
        {
            __state = null;
            var counter = EmporiumEntry.Instance?.backInvinvElement;
            if (counter == null || counter.childItems == null) return;
            __state = counter.childItems
                .Where(item => item != null
                    && item.identifier == ModConstants.BraceletItemId
                    && !GeneralHelper.IsItemOwned(item))
                .ToList();
        }

        private static void Postfix(List<GameItem> __state)
        {
            if (__state == null) return;
            foreach (GameItem item in __state)
            {
                if (item != null && GeneralHelper.IsItemOwned(item))
                    EleanorStory.NotePreludeBraceletAcquired(item);
            }
        }
    }
}
