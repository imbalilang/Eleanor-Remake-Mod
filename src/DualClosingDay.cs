using System;
using HarmonyLib;
namespace EleanorRomance
{
    /// <summary>The paid closure begins at the first free morning shutter opening.</summary>
    internal static class DualClosingDay
    {
        private const string Scene = "d06";
        private const string SleepDay = "closure.sleep_day";
        private const string Paid = "closure.paid";
        private const string ClientId = "er_story_dual_d06";
        private static object sleepTicket;
        internal static bool Enabled { private get; set; }

        internal static void PayOnce()
        {
            if (DualState.Flag(Paid) || PlayerStore.Instance == null) return;
            ModHelper.AddCredit(2000, false);
            DualState.Flag(Paid, true);
        }

        internal static void Reset() => sleepTicket = null;

        internal static bool IsVisit(StoreClient client)
            => client?.identifier == ClientId && VanillaEleanorBridge.IsOurClient(client);

        internal static void OnLoaded()
        {
            Reset();
            if (!DualState.Active || !DualState.Flag("d.route")) return;
            if (DualState.Done("d05") || DualState.Done("dv10")) PayOnce();
            var store = PlayerStore.Instance;
            if (DualState.Int(SleepDay, -1) == DualState.Today
                && store?.storeState == PlayerStore.StoreState.CLOSED)
                ScheduleSleep(store, DualState.Today);
        }

        internal static void OnShutterOpened()
        {
            var store = PlayerStore.Instance;
            if (!Enabled || !VersionGate.IsSupported || !DualState.Active || !DualState.Flag("d.route")
                || store?.storeState != PlayerStore.StoreState.OPEN) return;
            int day = DualState.Today;
            var manager = store.storeClientManager;
            if (manager?.clientStack == null) return;
            // Native urgent scenes keep their priority. Never insert a routine visit
            // ahead of them, or leave a closure client in the day's ordinary queue.
            manager.clientStack.RemoveAll(IsVisit);
            if (DualDirector.NextScene(day) != Scene
                || store.currentClientInstance != null || store.isClientBeingArrested || store.isHandlingShootout
                || store.playerLocation != PlayerStore.PlayerLocation.IN_STORE
                || manager.clientStack.Exists(c => c != null && !c.dismissable)) return;
            var client = DualStory.CreateScene(Scene);
            ModHelper.SetClientSource(client, ModConstants.ModId);
            store.currentClientInstance = StoreClientInstance.CreateClientInstance(client);
            DualState.PendingId = Scene;
            DualState.PendingScene = DualDirector.SceneNumber(Scene);
            DualState.NextSceneDay = day;
            RomanceState.LastQueuedDay = day;
            store.callNextWhenPossible = false;
            StoreUIManager.Instance.QueueClientArriveAnimation();
        }

        internal static void Finish()
        {
            var store = PlayerStore.Instance;
            if (!IsVisit(store?.currentClientInstance?.GetClientBlueprint())
                || store.storeState != PlayerStore.StoreState.OPEN || DualState.Done(Scene)) return;
            int day = DualState.Today;
            PayOnce();
            DualDirector.CompleteScene(Scene);
            DualState.SetInt(SleepDay, day);
            // No normal business remains today. Preserve the game's close, nightly
            // costs, inventory cycle, autosave and blackout into the next morning.
            store.DismissCurrentClient(callNextWhenPossible: false, interruptDialog: false);
            StoreShutterButton.Instance.EndAndClose();
            ScheduleSleep(store, day);
        }

        /// <summary>
        /// Harem-line v2 paid day: dv10 is the agreed closure morning. Pay once,
        /// close the store through the native path and skip the rest of the day.
        /// </summary>
        internal static void FinishV2()
        {
            if (!DualState.Flag("d.route") || DualState.Done("dv10")) return;
            var store = PlayerStore.Instance;
            int day = DualState.Today;
            PayOnce();
            DualDirector.CompleteScene("dv10", () => DualState.Flag("d.act3", true));
            DualState.SetInt(SleepDay, day);
            if (store == null || store.storeState != PlayerStore.StoreState.OPEN) return;
            store.DismissCurrentClient(callNextWhenPossible: false, interruptDialog: false);
            StoreShutterButton.Instance.EndAndClose();
            store.visitLeftTonight = 0;
            StoreUIManager.Instance.EnableBackingBlock();
            if (!EndingPageUI.ShowStoryScene("er_story_cg_date_title", "er_event_harem_lydia_date_cg",
                "er_story_cg_date_text", () => ScheduleSleep(store, day)))
                ScheduleSleep(store, day);
        }

        private static void ScheduleSleep(PlayerStore store, int day)
        {
            if (sleepTicket != null) return;
            var ticket = new object();
            sleepTicket = ticket;
            store.visitLeftTonight = 0;
            StoreUIManager.Instance.EnableBackingBlock();
            ModHelper.StartCoroutine(ModConstants.ModId, MetaHelper.WaitAndDo(4.1f, () =>
            {
                if (!Enabled || !ReferenceEquals(sleepTicket, ticket)) return;
                sleepTicket = null;
                if (DualState.Today != day || store.storeState != PlayerStore.StoreState.CLOSED) return;
                EndOfDayUIManager.Instance.InitUI();
                EndOfDayUIManager.Instance.OnEndDayButtonClick();
            }));
        }
    }

    [HarmonyPatch(typeof(StoreShutterButton), nameof(StoreShutterButton.OpenShutter))]
    internal static class DualMorningClosurePatch
    {
        private static void Postfix() => DualClosingDay.OnShutterOpened();
    }
}
