using System;
using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>Owns only the optional gala's empty-shop close and native sleep.</summary>
    public static class GalaClosing
    {
        private static bool presenting, finishing, sleepStarted;
        private static StoreClient waitingClient;
        private static int sessionDay=-1;
        public static bool Enabled { get; set; }

        public static void Reset()
        {presenting=false;finishing=false;sleepStarted=false;waitingClient=null;sessionDay=-1;}

        public static bool IsGala(StoreClient client)=>client?.identifier=="er_story_gala"
            && (string.IsNullOrEmpty(client.sourceModId) || client.sourceModId==ModConstants.ModId);

        // Retire only the old gala queue entry, never the other sixteen clients.
        public static void RecoverQueuedVisits(StoreClientManager manager)
        {
            if(!Enabled || !RomanceState.StoryActive || manager?.clientStack==null)return;
            manager.clientStack.RemoveAll(c=>IsGala(c));
            if(MaidState.PendingVisit==GalaStory.VisitId && !presenting)
            {MaidState.PendingVisit=0;MaidState.NextVisitDay=-1;}
        }

        public static void OnLoaded()
        {
            Reset();
            if(!Enabled || !RomanceState.StoryActive)return;
            var store=PlayerStore.Instance;
            RecoverQueuedVisits(store?.storeClientManager);
            if(MaidState.Flag("gala.close_lock"))
            {
                if(store!=null && store.lockShutterButtonFlag>0)store.lockShutterButtonFlag--;
                MaidState.Flag("gala.close_lock",false);
            }
            if(IsGala(store?.currentClientInstance?.GetClientBlueprint()))
            {
                // Older versions serialized a normal queue visitor and its
                // party graph. Retain phase/cursor, discard that presentation.
                store.currentClientInstance=null;
                if(StoreUIManager.Instance is StoreUIManager ui)ui.HideClient();
                if(DialogUIManager.Instance is DialogUIManager dialogue)
                {
                    // InterruptDialog executes the current line's old end
                    // action while typing. Discard it before retiring the graph.
                    dialogue.currentDialog=null;
                    dialogue.InterruptDialog();dialogue.HideChoiceBubble();
                    dialogue.ClearPausedConversation();
                }
            }
            // Reload during the shutter/blackout window continues the accepted
            // night, rather than offering an extra evening or sleeping twice.
            if(GalaStory.Phase==4 && MaidState.Int("gala.departure_day",-1)==MaidState.Today
                && store?.storeState==PlayerStore.StoreState.CLOSED)
            {
                finishing=true;sessionDay=MaidState.Today;store.visitLeftTonight=0;
                StoreUIManager.Instance.EnableBackingBlock();ScheduleSleep();return;
            }
        }

        private static bool CanInvite(PlayerStore store)
        {
            return Enabled && VersionGate.IsSupported && RomanceState.StoryActive
                && store!=null && store.storeState==PlayerStore.StoreState.OPEN
                && store.playerLocation==PlayerStore.PlayerLocation.IN_STORE
                && store.lockShutterButtonFlag==0 && !store.isClientBeingArrested && !store.isHandlingShootout
                && store.currentClientInstance==null && store.storeClientManager?.clientStack!=null
                && store.storeClientManager.clientStack.Count==0
                && MaidState.PendingVisit==0 && GalaStory.Due(MaidState.Today);
        }

        private static bool Present(StoreClient client)
        {
            var ui=StoreUIManager.Instance;
            PlayerStore.Instance.currentClientInstance=StoreClientInstance.CreateClientInstance(client);
            ui.clientGameObject.image.sprite=SpriteDict.Instance.ShowClientSprite(client.GetSprite());
            // Use the normal left-to-counter walk. Native OnArrived starts the
            // main dialogue only after the customer reaches the counter.
            ui.QueueClientArriveAnimation();
            return true;
        }

        private static void SchedulePresentation(StoreClient client)
        {ModHelper.StartCoroutine(ModConstants.ModId,MetaHelper.WaitAndDo(0.1f,()=>ResumePresentation(client)));}

        private static void ResumePresentation(StoreClient client)
        {
            if(!ReferenceEquals(waitingClient,client) || !presenting)return;
            var store=PlayerStore.Instance;
            if(!Enabled || sessionDay!=MaidState.Today || store.storeState!=PlayerStore.StoreState.OPEN
                || store.currentClientInstance!=null || store.storeClientManager.clientStack.Count!=0)
            {Release(store);return;}
            // The native departure clears currentClientInstance before its
            // animation finishes. Let OnLeft hide the old sprite first.
            if(store.lockNextBellFlag>0){SchedulePresentation(client);return;}
            waitingClient=null;
            try
            {
                if(Present(client))return;
                throw new InvalidOperationException("Closing invitation could not be displayed");
            }
            catch(Exception ex)
            {
                Release(store);
                ModEntry.LogError("Gala invitation remains available for another close: "+ex);
                StoreShutterButton.Instance.EndAndClose();
            }
        }

        // True means the native close may continue. Only an empty OPEN shop can
        // be intercepted; early-closing confirmations keep their native behavior.
        public static bool BeforeClose()
        {
            if(!Enabled)return true;
            if(presenting || finishing)return false;
            var store=PlayerStore.Instance;
            RecoverQueuedVisits(store?.storeClientManager);
            if(!CanInvite(store))return true;
            try
            {
                var client=GalaStory.CreateVisit();if(client==null)return true;
                ModHelper.SetClientSource(client,ModConstants.ModId);
                presenting=true;sessionDay=MaidState.Today;
                MaidState.PendingVisit=GalaStory.VisitId;
                store.callNextWhenPossible=false;store.lockShutterButtonFlag++;
                MaidState.Flag("gala.close_lock",true);
                if(store.lockNextBellFlag>0)
                {waitingClient=client;SchedulePresentation(client);return false;}
                if(Present(client))return false;
                throw new InvalidOperationException("Closing invitation could not be displayed");
            }
            catch(Exception ex)
            {
                Release(store);
                ModEntry.LogError("Gala invitation remains available for another close: "+ex);
                return true;
            }
        }

        private static void Release(PlayerStore store)
        {
            if(store!=null)
            {
                if(IsGala(store.currentClientInstance?.GetClientBlueprint()))store.currentClientInstance=null;
                if(MaidState.Flag("gala.close_lock") && store.lockShutterButtonFlag>0)store.lockShutterButtonFlag--;
            }
            MaidState.Flag("gala.close_lock",false);
            if(MaidState.PendingVisit==GalaStory.VisitId)
            {MaidState.PendingVisit=0;MaidState.NextVisitDay=-1;}
            presenting=false;waitingClient=null;
        }

        public static void Finish(bool attend)
        {
            if(!presenting || finishing || sessionDay!=MaidState.Today)return;
            var store=PlayerStore.Instance;
            if(!IsGala(store?.currentClientInstance?.GetClientBlueprint()))return;
            finishing=true;
            Release(store);
            try
            {
                CloseNative(attend);
                if(!attend)Reset();
            }
            catch(Exception ex)
            {
                Reset();
                ModEntry.LogError("Gala close interrupted; saved invitation/acceptance retained: "+ex);
            }
        }

        private static void CloseNative(bool skipNight)
        {
            StoreUIManager.Instance.HideClient();
            // Keep native close hooks, stock cleanup, service-cost refresh and
            // shutter animation. Never empty the queue ourselves to force this.
            StoreShutterButton.Instance.EndAndClose();
            if(!skipNight)return;
            PlayerStore.Instance.visitLeftTonight=0;
            StoreUIManager.Instance.EnableBackingBlock();
            ScheduleSleep();
        }

        private static void ScheduleSleep()
        {
            // EndAndClose has callbacks through 2.75 + 1.25 seconds. Let them
            // finish before morning, so they cannot hide next day's front area.
            // Keep the native wait timing while letting the loader cancel it.
            ModHelper.StartCoroutine(ModConstants.ModId,MetaHelper.WaitAndDo(4.1f,SleepNative));
        }

        private static void SleepNative()
        {
            if(!Enabled || !finishing || sleepStarted || sessionDay!=MaidState.Today)return;
            sleepStarted=true;
            // Use the actual native overnight path: expenses, inventory cycles,
            // health, events, BeginDay, autosave and the next-morning panel.
            EndOfDayUIManager.Instance.InitUI();
            EndOfDayUIManager.Instance.OnEndDayButtonClick();
        }

        public static void OnNewDay()
        {
            if(!Enabled || !RomanceState.StoryActive)return;
            Reset();GalaStory.Initialize();
            GalaStory.TickNews(StoreStation.Instance?.storeEventManager);
        }
    }

    [HarmonyPatch(typeof(StoreShutterButton),nameof(StoreShutterButton.CloseShutter))]
    internal static class GalaClosePatch
    {private static bool Prefix()=>GalaClosing.BeforeClose();}

    [HarmonyPatch(typeof(PlayerStore),nameof(PlayerStore.BeginDay))]
    internal static class GalaNextDayPatch
    {private static void Postfix()=>GalaClosing.OnNewDay();}
}
