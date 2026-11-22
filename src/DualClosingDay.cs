using System;
using HarmonyLib;
namespace EleanorRomance
{
    /// <summary>The paid closure begins at the first free morning shutter opening.</summary>
    internal static class DualClosingDay
    {
        private const string Scene = "d06"; // legacy route; v2 uses dv10
        private const string SleepDay = "closure.sleep_day";
        private const string Paid = "closure.paid";
        private const string OldClientId = "er_story_dual_d06";
        private const string V2ClientId = "er_story_dual_dv10";
        private static object sleepTicket;
        internal static bool Enabled { private get; set; }

        private static string ClosingScene => DualDirector.UsesV2 ? "dv10" : Scene;

        internal static void PayOnce()
        {
            if (DualState.Flag(Paid) || PlayerStore.Instance == null) return;
            ModHelper.AddCredit(2000, false);
            DualState.Flag(Paid, true);
        }

        internal static void Reset() => sleepTicket = null;

        internal static bool IsVisit(StoreClient client)
            => client != null
                && (client.identifier == OldClientId || client.identifier == V2ClientId)
                && VanillaEleanorBridge.IsOurClient(client);

        internal static void OnLoaded()
        {
            Reset();
            if (!DualState.Active || !DualState.Flag("d.route")) return;
            if (DualState.Done("d05") || DualState.Done("dv10")) PayOnce();
            var store = PlayerStore.Instance;
            // A closure date serialized in the current-client slot can block
            // every later visitor when its arrival/dialog never resumes. The
            // date is short and replayable; discard the stale presentation so
            // the next shutter opening can queue it again.
            if (store != null && DualState.PendingId == ClosingScene
                && IsVisit(store.currentClientInstance?.GetClientBlueprint()))
            {
                store.currentClientInstance = null;
                if (StoreUIManager.Instance is StoreUIManager ui) ui.HideClient();
                if (DialogUIManager.Instance is DialogUIManager dialogue)
                {
                    dialogue.currentDialog = null;
                    dialogue.InterruptDialog();
                    dialogue.HideChoiceBubble();
                    dialogue.ClearPausedConversation();
                }
                DualState.PendingId = "";
                DualState.PendingScene = 0;
                DualState.NextSceneDay = -1;
                ModEntry.Log("Discarded stale dual closure client on load: " + ClosingScene);
            }
            // Legacy d06 intentionally slept through the whole day.  The v2
            // date must return the evening even if an old save still carries a
            // sleep marker from a previously installed build.
            if (!DualDirector.UsesV2 && DualState.Int(SleepDay, -1) == DualState.Today
                && store?.storeState == PlayerStore.StoreState.CLOSED)
                ScheduleSleep(store, DualState.Today);
        }

        internal static bool OnShutterOpened()
        {
            var store = PlayerStore.Instance;
            if (!Enabled || !VersionGate.IsSupported || !DualState.Active || !DualState.Flag("d.route")
                || store?.storeState != PlayerStore.StoreState.OPEN) return false;
            int day = DualState.Today;
            var manager = store.storeClientManager;
            if (manager?.clientStack == null) return false;
            string scene = ClosingScene;
            // A closure left over from an earlier day without a live client is
            // stale. Clear it so opening the shop can retry instead of blocking
            // every later visit through PendingId.
            if (DualState.PendingId == scene && DualState.NextSceneDay >= 0 && DualState.NextSceneDay < day
                && !IsVisit(store.currentClientInstance?.GetClientBlueprint()))
            {
                manager.clientStack.RemoveAll(IsVisit);
                DualState.PendingId = "";
                DualState.PendingScene = 0;
                DualState.NextSceneDay = -1;
            }
            if (DualState.PendingId == scene
                && (manager.clientStack.Exists(IsVisit)
                    || IsVisit(store.currentClientInstance?.GetClientBlueprint()))) return true;
            // Native urgent scenes keep their priority. Never insert a routine visit
            // ahead of them, or leave a closure client in the day's ordinary queue.
            manager.clientStack.RemoveAll(IsVisit);
            if (DualDirector.NextScene(day) != scene
                || store.currentClientInstance != null || store.isClientBeingArrested || store.isHandlingShootout
                || store.playerLocation != PlayerStore.PlayerLocation.IN_STORE
                || manager.clientStack.Exists(c => c != null && !c.dismissable)) return false;
            var client = DualStory.CreateScene(scene);
            ModHelper.SetClientSource(client, ModConstants.ModId);
            // Let the native arrival pipeline present the date. Setting
            // currentClientInstance directly could leave an invisible customer
            // holding the slot forever after a shutter/UI timing change.
            manager.AddNextClient(client);
            if (!manager.clientStack.Contains(client)) return false;
            DualState.PendingId = scene;
            DualState.PendingScene = DualDirector.SceneNumber(scene);
            DualState.NextSceneDay = day;
            RomanceState.LastQueuedDay = day;
            store.callNextWhenPossible = false;
            ModEntry.Log("Dual closing day queued: " + scene + " on day " + day);
            return true;
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
        /// close the daytime business through the native path, then return the
        /// player to the normal evening/after-hours state.  The date must not
        /// sleep the player into the next morning.
        /// </summary>
        internal static void FinishV2()
        {
            if (!DualState.Flag("d.route") || DualState.Done("dv10")) return;
            var store = PlayerStore.Instance;
            int day = DualState.Today;
            PayOnce();
            DualDirector.CompleteScene("dv10", () => DualState.Flag("d.act3", true));
            if (store == null || store.storeState != PlayerStore.StoreState.OPEN) return;
            store.DismissCurrentClient(callNextWhenPossible: false, interruptDialog: false);
            StoreShutterButton.Instance.EndAndClose();
            store.visitLeftTonight = 0;
            EndingPageUI.ShowStoryScene("er_story_cg_date_title", "er_event_harem_lydia_date_cg",
                "er_story_cg_date_text");
            ModEntry.Log("Dual date finished; daytime business closed, evening returned. day=" + day);
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

    /// <summary>
    /// The shutter button is not the only route into an open shop (shortcuts,
    /// automation and other UI paths can call the store state method directly).
    /// Run the same closure check from the central open call as well.
    /// </summary>
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.OpenShutter))]
    internal static class DualMorningClosureStorePatch
    {
        private static void Postfix() => DualClosingDay.OnShutterOpened();
    }
}
