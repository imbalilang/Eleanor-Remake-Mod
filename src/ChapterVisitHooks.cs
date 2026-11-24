using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EleanorRomance
{
    [HarmonyPatch(typeof(StoreShutterButton), nameof(StoreShutterButton.OpenShutter))]
    internal static class ChapterOpenShutterPatch
    {
        private static bool Prefix()
        {
            if (!LoveA2Closing.Enabled || PlayerStore.Instance?.storeState != PlayerStore.StoreState.MORNING) return true;
            ChapterMorningPatch.CheckMorning();
            return !EndingPageUI.PageOpen;
        }
    }

    // At the bell the native opening has finished building the queue, including future/special clients.
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.TryCallNextClient))]
    internal static class ChapterDayOffBellPatch
    {
        private static bool Prefix(PlayerStore __instance)
        {
            if (!LoveA2Closing.Enabled || !RomanceState.StoryActive) return true;
            if (EndingPageUI.PageOpen) return false;
            if (!ChapterVisits.DayOffAvailable(__instance) || !__instance.CanNextClient()) return true;
            return !ChapterVisits.OfferDayOff(__instance.TryCallNextClient, StoreShutterButton.Instance.EndAndClose);
        }
    }

    [HarmonyPatch(typeof(StoreShutterButton), nameof(StoreShutterButton.EndAndClose))]
    internal static class ChapterCloseShutterPatch
    {
        private static bool Prefix(StoreShutterButton __instance)
        {
            if (!LoveA2Closing.Enabled || !RomanceState.StoryActive) return true;
            if (EndingPageUI.PageOpen || PlayerStore.Instance?.storeState != PlayerStore.StoreState.OPEN) return false;
            if (LoveA2Closing.OwnsTonight) return true;
            if (ChapterVisits.DayOffAvailable(PlayerStore.Instance))
                return !ChapterVisits.OfferDayOff(__instance.EndAndClose, __instance.EndAndClose);
            return !ChapterVisits.AtClosing(__instance.EndAndClose);
        }
    }

    // The game adds late, future and mod customers after the ordinary list. Recheck at service
    // time as well, since existing saves or a mid-day arrival can change the queue again.
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.GetNextClient))]
    internal static class ChapterServingOrderPatch
    {
        private static void Prefix(PlayerStore __instance)
        {
            if (!LoveA2Closing.Enabled || !RomanceState.StoryActive) return;
            LoveDirector.EnsureA2FirstLast(__instance.storeClientManager);
            DualClosingDay.ServeReservedGuestFirst(__instance.storeClientManager);
        }
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.HandleNormalClient))]
    internal static class ChapterTradeQueuePatch
    {
        [ThreadStatic] internal static HashSet<StoreClient> WalkIns;
        private static StoreClientManager queueOwner;
        private static HashSet<StoreClient> todaysWalkIns;
        private static int queueDay;
        private static void Prefix() => WalkIns = ChapterVisits.Active ? new HashSet<StoreClient>() : null;
        private static void Postfix(StoreClientManager __instance)
        {
            queueOwner = __instance;
            queueDay = DualState.Today;
            todaysWalkIns = WalkIns;
            WalkIns = null;
            TryInsert(__instance);
        }
        internal static bool OnlyOrdinaryGuestsRemain(StoreClientManager manager)
        {
            if (manager?.clientStack == null) return false;
            if (manager.clientStack.Count == 0) return true;
            return manager == queueOwner && queueDay == DualState.Today && todaysWalkIns != null
                && manager.clientStack.All(c => c == null || (todaysWalkIns.Contains(c) && IsOrdinary(c)));
        }

        internal static bool IsOrdinary(StoreClient client) => client != null && !client.isWanted && !client.isSecurity
            && !client.isSentenceOfficer && client.dismissable && client.clientIntent != StoreClient.ClientIntent.DIALOGUE
            && string.IsNullOrEmpty(client.sourceModId) && string.IsNullOrEmpty(client.attractedItemId)
            && string.IsNullOrEmpty(client.eventSourceId) && !VanillaEleanorBridge.IsOurClient(client);

        internal static void TryInsert(StoreClientManager manager)
        {
            if (manager == null || manager != queueOwner || queueDay != DualState.Today) return;
            var picked = todaysWalkIns;
            var stack = manager.clientStack;
            if (picked == null || stack == null || stack.Any(ChapterVisits.IsChapterClient)
                || ChapterVisits.IsChapterClient(PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint())) return;
            var due = ChapterVisits.TradeDue();
            if (due == null) return;
            for (int i = 1; i < stack.Count; i++)
            {
                // Keep a walk-in between the preceding main chapter and this independent transaction.
                if (!picked.Contains(stack[i - 1]) || !picked.Contains(stack[i])
                    || !IsOrdinary(stack[i - 1]) || !IsOrdinary(stack[i])) continue;
                stack[i] = ChapterVisits.Trade(due);
                break;
            }
        }
        private static void Finalizer() => WalkIns = null;
    }

    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.PickClient))]
    internal static class ChapterRandomWalkInPatch
    {
        private static void Postfix(StoreClient __result)
        {
            if (ChapterTradeQueuePatch.IsOrdinary(__result))
                ChapterTradeQueuePatch.WalkIns?.Add(__result);
        }
    }
}
