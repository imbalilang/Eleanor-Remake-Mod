using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EleanorRomance
{
    /// <summary>
    /// Keep the remake's chapters, but let vanilla own bail timing, payment,
    /// reputation checks, old-arrest migration, and its aid/reward definitions.
    /// </summary>
    public static class VanillaEleanorBridge
    {
        private static readonly ConditionalWeakTable<StoreClient, object> decorated
            = new ConditionalWeakTable<StoreClient, object>();
        private static bool capturingReward;
        private static GameItem capturedReward;

        public static bool RescuePending => RomanceState.StoryActive && !RomanceState.RomanceLocked
            && RomanceState.Arrested && !RomanceState.Rescued;

        public static bool AllowOriginalSchedule => StoryDirector.ShouldSuppressOriginal && RescuePending;

        public static bool IsOurClient(StoreClient client) => client != null
            && (client.sourceModId == ModConstants.ModId
                || (client.identifier ?? "").StartsWith("er_story_", StringComparison.Ordinal));

        public static void OnLoaded()
        {
            SynchronizeRescue();
            if (!RescuePending) return;
            StoreClientManager manager = PlayerStore.Instance.storeClientManager;
            if (manager == null || manager.clientStack == null) return;
            // Replace obsolete mod-only rescue offers with the native scheduler.
            manager.clientStack.RemoveAll(c => c != null && c.identifier == "er_story_rescue");
            foreach (StoreClient client in manager.clientStack)
                if (IsRescueClient(client)) DecorateRescueClient(client);
            StoreClient current = PlayerStore.Instance.currentClientInstance?.GetClientBlueprint();
            if (IsRescueClient(current)) DecorateRescueClient(current);
        }

        public static void RecordArrest(int resumeChapter)
        {
            StoreClientData data = PlayerStore.Instance.storeClientData;
            data.isEleanorDead = false; // The living remake visitor was just arrested.
            data.eleanorArrested = true;
            data.eleanorArrestDay = StoreStation.Instance.dayCounter;
            data.eleanorArrestVisit = resumeChapter;
            data.eleanorBailChoice = 0;
            data.eleanorBailDay = -1;
            data.eleanorFreedVisitDay = -1;
            RomanceState.NativeRescueLinked = true;
            RomanceState.Arrested = true;
            RomanceState.Rescued = false;
            RomanceState.TrapResumeChapter = Math.Max(6, resumeChapter);
            RomanceState.PendingChapter = 0;
            RomanceState.Chapter = 5;
            // Native CanEleanorBailOfficerVisit / CanEleanorFreedVisit own this window.
            RomanceState.NextVisitDay = -1;
        }

        public static void SynchronizeRescue()
        {
            if (!RescuePending) return;
            StoreClientData data = PlayerStore.Instance.storeClientData;
            if (!RomanceState.NativeRescueLinked)
            {
                // Pre-bridge MOD arrests never initialized the native bail fields.
                data.isEleanorDead = false;
                data.eleanorArrested = true;
                data.eleanorArrestDay = -1;
                data.eleanorArrestVisit = RomanceState.TrapResumeChapter;
                data.eleanorBailChoice = 0;
                data.eleanorBailDay = -1;
                data.eleanorFreedVisitDay = -1;
                StoreClientData.MigrateEleanorBail(StoreStation.Instance.dayCounter);
                RomanceState.NativeRescueLinked = true;
            }
            if (data.eleanorBailChoice == 3) StoryDirector.AbandonStory();
        }

        private static bool IsRescueClient(StoreClient client) => client != null
            && (client.identifier == "eleanorBailOfficer" || client.identifier == "eleanorFreed");

        /// <summary>Filter only vanilla Eleanor visitors while the remake owns her story.</summary>
        public static bool FilterScheduledClient(StoreClientManager manager, StoreClient client)
        {
            if (!StoryDirector.ShouldSuppressOriginal || client == null || IsOurClient(client)) return true;
            if (!string.IsNullOrEmpty(client.sourceModId)) return true;
            if (!(client.identifier ?? "").StartsWith("eleanor", StringComparison.OrdinalIgnoreCase)) return true;
            if (!RescuePending || !IsRescueClient(client)) return false;

            StoreClient current = PlayerStore.Instance.currentClientInstance?.GetClientBlueprint();
            if (IsRescueClient(current) || manager.clientStack.Any(IsOurClient)) return false;
            if (RomanceState.LastQueuedDay == StoreStation.Instance.dayCounter) return false;

            DecorateRescueClient(client);
            RomanceState.PendingChapter = 5;
            RomanceState.LastQueuedDay = StoreStation.Instance.dayCounter;
            return true;
        }

        private static void DecorateRescueClient(StoreClient client)
        {
            if (decorated.TryGetValue(client, out _)) return;
            decorated.Add(client, new object());
            ModHelper.SetClientSource(client, ModConstants.ModId);
            client.spriteName = PortraitCompat.Resolve(client.spriteName);
            // Shooting remains a separately designed story change; keep the remake's protection.
            client.nonShootable = true;
            client.forceShootable = false;

            if (client.identifier == "eleanorBailOfficer")
            {
                Dialogue choices = FindChoices(client.mainDialogue);
                Dialogue rescueLater = StoryEngine.PlayerReply(L.Get("er_choice_rescue_later"));
                rescueLater.SetEndAction(StoryEngine.Dismiss);
                choices.AddChoice(DirectoryMaster.Item("yes"), L.Get("er_choice_rescue_later"),
                    L.Get("er_choice_rescue_later_desc"), rescueLater, null);
                client.OnDismiss += SynchronizeRescue;
                return;
            }

            // Native marks the freed visit at its start. Commit that same action
            // with the remake's continuation at the end, so an interrupted visit
            // remains eligible for replay and cannot strand either state machine.
            Action markFreed = client.mainDialogue.startAction;
            client.mainDialogue.startAction = null;
            Dialogue tail = StoryEngine.Tail(client.mainDialogue);
            Action leave = tail.endAction;
            tail.endAction = null;
            StoryEngine.Append(client.mainDialogue,
                RomanceState.UncleInspected && !RomanceState.UncleWarned
                    ? StoryV06Content.B_C05_B010 : StoryV06Content.B_C05_B009);
            tail = StoryEngine.Tail(client.mainDialogue);
            tail.SetEndAction(() =>
            {
                if (!RescuePending) return;
                markFreed?.Invoke();
                RomanceState.RescueMethod = PlayerStore.Instance.storeClientData.eleanorBailChoice;
                RomanceState.Rescued = true;
                RomanceState.Arrested = false;
                StoryDirector.AdvanceChapter(5, Math.Max(6, RomanceState.TrapResumeChapter));
                leave?.Invoke();
            });
        }

        private static Dialogue FindChoices(Dialogue dialogue)
        {
            // HasChoices means "has an enabled choice", which is false while
            // the player cannot afford the native cash offer.
            while (dialogue != null && dialogue.choices.Count == 0) dialogue = dialogue.nextDialogue;
            return dialogue ?? throw new InvalidOperationException("Native Eleanor choices are unavailable.");
        }

        public static void AttachCashAid(StoreClient client, Dialogue target = null)
        {
            Dialogue nativeChoices = FindChoices(StoreClientUniqueList.Eleanor4().mainDialogue);
            var choice = nativeChoices.choices[0];
            Dialogue response = choice.Item2;
            Func<Dialogue, bool> nativeDisabled = response.isDisabledFunc;
            StoryEngine.Tail(response).SetEndAction(() =>
                PlayerStore.Instance.TryPlayerDismissCurrentClient(callNextWhenPossible: true));
            StoryEngine.Tail(target ?? client.mainDialogue).AddChoice(choice.Item1, choice.Item1.name,
                choice.Item1.shortDescription, response, () =>
                {
                    if (RomanceState.HardshipAidGiven) return;
                    StoreClientData data = PlayerStore.Instance.storeClientData;
                    int helped = data.eleanorHelpedTimes;
                    choice.Item3(); // Native funds check, payment and one-time aid accounting.
                    if (data.eleanorHelpedTimes > helped)
                    {
                        RomanceState.HardshipAidGiven = true;
                        RomanceState.HardshipCashGiven = true;
                    }
                }, () => !RomanceState.HardshipAidGiven
                    && (nativeDisabled == null || !nativeDisabled(response)));
        }

        public static bool GiveWarningGift(int option)
        {
            if (capturingReward) return false;
            capturedReward = null;
            GameItem reward;
            try
            {
                capturingReward = true;
                // Use the actual native reward action, including future balance changes.
                StoreClientUniqueList.EleanorWarned().barterOffer.offerSets[option]();
                reward = capturedReward;
            }
            catch (Exception ex)
            {
                if (capturedReward != null)
                    GraphUtils.DestroyIfUnplaced(capturedReward, "Native Eleanor reward failed");
                StoryDirector.LogError("Native Eleanor reward failed: " + ex);
                return false;
            }
            finally
            {
                capturingReward = false;
                capturedReward = null;
            }
            return reward != null && StoryEngine.DeliverToPlayer(reward);
        }

        // Intercept only the synchronous placement issued by our native reward
        // action. Everything else keeps the game's normal table placement.
        public static bool CaptureReward(GameItem item)
        {
            if (!capturingReward) return false;
            capturedReward = item;
            return true;
        }
    }
}
