using System;
using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>
    /// Act 2 water-shortage close: Sophia is queued as the last ordinary
    /// customer of the day. After her trade and the robbery beat, the CG plays
    /// and the shop is closed for the night.
    /// </summary>
    internal static class LoveA2Closing
    {
        private const string Done = "a2.robbery.done";
        private const string SleepDay = "a2.robbery.sleep_day";
        private static bool finishing, sleepStarted;
        private static object sleepTicket;

        internal static bool Enabled { get; set; }

        internal static void Reset()
        {
            finishing = false;
            sleepStarted = false;
            sleepTicket = null;
        }

        internal static void OnLoaded()
        {
            Reset();
            if (!Enabled || !LoveState.Active) return;
            var store = PlayerStore.Instance;
            if (LoveState.Flag(Done) && LoveState.Int(SleepDay, -1) == LoveState.Today
                && store?.storeState == PlayerStore.StoreState.CLOSED)
            {
                finishing = true;
                store.visitLeftTonight = 0;
                StoreUIManager.Instance.EnableBackingBlock();
                ScheduleSleep(store, LoveState.Today);
            }
        }

        /// <summary>Called when the robbery dialogue ends; the client is the last customer.</summary>
        internal static void FinishFromVisit()
        {
            if (finishing) return;
            finishing = true;
            LoveState.Flag(Done, true);
            LoveDirector.Complete(4);
            int day = LoveState.Today;
            LoveState.SetInt(SleepDay, day);
            var store = PlayerStore.Instance;
            if (store != null) store.callNextWhenPossible = false;

            if (!EndingPageUI.ShowStoryScene("er_story_cg_love_water_cut_title",
                "er_event_love_water_cut_cg", "er_story_cg_love_water_cut_text",
                () => CloseAndSleep(store, day)))
            {
                CloseAndSleep(store, day);
            }
        }

        private static void CloseAndSleep(PlayerStore store, int day)
        {
            if (store == null) return;
            try
            {
                StoreShutterButton.Instance.EndAndClose();
                store.visitLeftTonight = 0;
                StoreUIManager.Instance.EnableBackingBlock();
                ScheduleSleep(store, day);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Love a2 robbery close interrupted: " + ex);
            }
        }

        private static void ScheduleSleep(PlayerStore store, int day)
        {
            if (sleepTicket != null || sleepStarted) return;
            var ticket = new object();
            sleepTicket = ticket;
            sleepStarted = true;
            store.visitLeftTonight = 0;
            StoreUIManager.Instance.EnableBackingBlock();
            ModHelper.StartCoroutine(ModConstants.ModId, MetaHelper.WaitAndDo(4.1f, () =>
            {
                if (!Enabled || !ReferenceEquals(sleepTicket, ticket)) return;
                sleepTicket = null;
                if (LoveState.Today != day || store.storeState != PlayerStore.StoreState.CLOSED) return;
                EndOfDayUIManager.Instance.InitUI();
                EndOfDayUIManager.Instance.OnEndDayButtonClick();
            }));
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.BeginDay))]
    internal static class LoveA2NextDayPatch
    {
        private static void Postfix() => LoveA2Closing.Reset();
    }
}
