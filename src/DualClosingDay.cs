namespace EleanorRomance
{
    /// <summary>The paid closure begins at the first free morning shutter opening.</summary>
    internal static class DualClosingDay
    {
        internal const string Scene = "dv10";
        private const string ClientId = "er_story_dual_dv10";
        private const string Paid = "closure.paid";
        internal static bool Enabled { private get; set; }

        internal static void PayOnce()
        {
            if (DualState.Flag(Paid) || PlayerStore.Instance == null) return;
            ModHelper.AddCredit(2000, false);
            DualState.Flag(Paid, true);
        }

        internal static bool IsVisit(StoreClient client)
            => client != null
                && client.identifier == ClientId
                && VanillaEleanorBridge.IsOurClient(client);

        internal static void OnLoaded()
        {
            if (!DualState.Active) return;
            if (DualState.Done(Scene)) PayOnce();
            var store = PlayerStore.Instance;
            // A closure date serialized in the current-client slot can block
            // every later visitor when its arrival/dialog never resumes. The
            // date is short and replayable; discard the stale presentation so
            // the next shutter opening can queue it again.
            if (store != null && DualState.PendingId == Scene
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
                ModEntry.Log("Discarded stale dual closure client on load: " + Scene);
            }
        }

        /// <summary>
        /// Lou-style morning special visit: queue the closure date while the
        /// native customer generator builds today's queue. It is inserted at
        /// the front and presented by the normal opening flow; the player does
        /// not need a special door scene or a separate trigger.
        /// </summary>
        internal static bool TryQueueMorning(StoreClientManager manager)
        {
            var store = PlayerStore.Instance;
            if (!Enabled || !VersionGate.IsSupported || store == null
                || !DualState.Active) return false;
            if (manager?.clientStack == null) return false;
            int day = DualState.Today;
            if (DualDirector.NextScene(day) != Scene) return false;
            if (store.currentClientInstance != null || store.isClientBeingArrested || store.isHandlingShootout) return false;
            bool already = manager.clientStack.Exists(IsVisit);
            if (DualState.PendingId == Scene && already)
            {
                DualState.NextSceneDay = day;
                RomanceState.LastQueuedDay = day;
                return true;
            }
            // Drop an old closure entry before rebuilding today's front slot.
            manager.clientStack.RemoveAll(IsVisit);
            var client = DualStory.CreateScene(Scene);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddNextClient(client);
            if (!manager.clientStack.Contains(client)) return false;
            DualState.PendingId = Scene;
            DualState.PendingScene = DualDirector.SceneNumber(Scene);
            DualState.NextSceneDay = day;
            RomanceState.LastQueuedDay = day;
            ModEntry.Log("Dual closing day queued at morning generation: " + Scene + " on day " + day);
            return true;
        }

        /// <summary>
        /// Harem-line v2 paid day: dv10 is the agreed closure morning. Pay once,
        /// close the daytime business through the native path, then return the
        /// player to the normal evening/after-hours state.  The date must not
        /// sleep the player into the next morning.
        /// </summary>
        internal static void Finish()
        {
            if (!DualState.Active || DualState.Done(Scene)) return;
            var store = PlayerStore.Instance;
            int day = DualState.Today;
            PayOnce();
            DualDirector.CompleteScene(Scene, () => DualState.Flag("d.act3", true));
            if (store == null || store.storeState != PlayerStore.StoreState.OPEN) return;
            store.DismissCurrentClient(callNextWhenPossible: false, interruptDialog: false);
            StoreShutterButton.Instance.EndAndClose();
            store.visitLeftTonight = 0;
            EndingPageUI.ShowStoryScene("er_story_cg_date_title", "er_event_harem_lydia_date_cg",
                "er_story_cg_date_text");
            ModEntry.Log("Dual date finished; daytime business closed, evening returned. day=" + day);
        }

    }
}
