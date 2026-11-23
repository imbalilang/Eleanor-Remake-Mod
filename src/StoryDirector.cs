using System;
using System.Linq;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Reachability, queueing and chapter progression.  A visit is considered
    /// complete only when the chapter's wrap-up dialogue commits it; merely
    /// queueing a client never advances the story.
    /// </summary>
    public static class StoryDirector
    {
        // Keep vanilla Eleanor from taking over an incompatible mod save while its story is paused.
        public static bool ShouldSuppressOriginal => VersionGate.IsSupported && !RomanceState.OldBlocked
            && (RomanceState.Initialized || RomanceState.IncompatibleSave);

        public static void OnGameLoadedEarly()
        {
            LoveA2Closing.Reset();
            EndingPageUI.HideRoutePage();
            StoryPresentation.Clear();
            VersionGate.Refresh();
            DualShowcaseEffects.OnLoaded();
            PortraitCompat.Refresh();

            if (RomanceState.IncompatibleSave)
            {
                ModEntry.Log("Eleanor story paused: save schema " + RomanceState.SchemaVersion
                    + ", expected " + RomanceState.Schema + ". Existing story data kept.");
                Notify(L.Get("er_notice_save_schema", RomanceState.SchemaVersion, RomanceState.Schema));
                return;
            }
            if (RomanceState.PredatesStoryRewrite)
            {
                ModEntry.Log("Eleanor story paused: save predates the story rewrite.");
                Notify(L.Get("er_notice_save_before_rewrite"));
                return;
            }

            RomanceState.InitializeForSave();
            RecoverStuckBellLock();
            PortraitCompat.SanitizeLoadedClients();
            // The vanilla keepsake is part of the ending, not a separate claim.
            // Old saves that deferred it receive it as soon as the save loads.
            if (EndgamePerfumeDue) StoryEngine.GiveSelfRefillingPerfume();
            DualDirector.OnLoaded();
            VanillaEleanorBridge.OnLoaded();
            MaidProjects.OnLoaded();
            MaidDividends.Accrue();
            DualClosingDay.OnLoaded();
            LoveA2Closing.OnLoaded();
            MaidWorldEvents.TickNews(StoreStation.Instance?.storeEventManager);
        }

        public static void OnNewGameStarted(string scenarioId)
        {
            LoveA2Closing.Reset();
            EndingPageUI.HideRoutePage();
            StoryPresentation.Clear();
            VersionGate.Refresh();

            if (!RomanceState.Initialized)
            {
                RomanceState.ResetForNewGame(StartingPerkRegistration.IsEarlyStartRun());
                if (RomanceState.EarlyStart)
                {
                    Notify(L.Get("er_notice_early_start"));
                }
            }
            DualShowcaseEffects.OnLoaded();
            ModEntry.Log("New game. scenario=" + scenarioId + ", chapter=" + RomanceState.Chapter
                + ", next=" + RomanceState.NextVisitDay);
            MaidProjects.OnLoaded();
            DualDirector.OnLoaded();
        }

        public static void OnGenerateCustomerVeryEarly(StoreClientManager manager)
        {
            if (manager == null) return;

            try
            {
                if (RomanceState.IncompatibleSave || RomanceState.PredatesStoryRewrite) return;
                RecoverStuckBellLock();

                LoveDirector.EnsureA2FirstLast(manager);
                LoveWaterShortage.Ensure(StoreStation.Instance?.storeEventManager);
                PortraitCompat.SanitizeLoadedClients();
                // Refresh the project rows after the game has set up the shop.
                // This also announces newly visible projects on the next opening.
                MaidProjects.RefreshStates(true);
                MaidProjects.NotifyRepairNotice();
                MaidDividends.Accrue();
                if (RomanceState.OldBlocked)
                {
                    TryQueueOldSavePrompt(manager);
                    return;
                }

                if (!RomanceState.StoryActive) return;
                EleanorStory.EnsurePreludeBraceletCountInitialized();
                RomanceState.SyncNativeSavings();

                VanillaEleanorBridge.SynchronizeRescue();
                // Queue the rescue from our own daily hook.  Vanilla's
                // CheckStoryLineEleanor may be suppressed by both this mod and
                // another mod, so the rescue cannot depend on that patch order.
                VanillaEleanorBridge.ScheduleRescue(manager);
                if (VanillaEleanorBridge.RescuePending) return;

                // Lou-style special morning visit: put the paid closure date at
                // the front of today's generated queue before ordinary visits.
                if (DualClosingDay.TryQueueMorning(manager)) return;

                int day = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
                if (EndgamePerfumeDue) StoryEngine.GiveSelfRefillingPerfume();
                DualProgression.Check();
                DualShowcaseEffects.FlushPending();
                DualDirector.RecoverPending(manager);
                LoveDirector.RecoverPending(manager);
                bool eveningFollowUp = DualDirector.SameDayFollowUpDue(day);
                if (RomanceState.LastQueuedDay == day && !eveningFollowUp) return;
                if (MaidState.LastImportantDay == day && !eveningFollowUp) return;
                if (HasModClient(manager)) return;
                if (MaidStory.HasUrgentClient(manager)) return;
                MaidWorldEvents.TickNews(StoreStation.Instance?.storeEventManager);
                DualDirector.RecoverPending(manager);
                if (DualDirector.TryQueue(manager, day)) return;
                if (DualState.Active && DualDirector.NextScene(day) == DualClosingDay.Scene) return;
                if (MaidStory.TryQueue(manager, day)) return;
                if (DualBusiness.TryQueue(manager, day)) return;
                if (LoveDirector.TryQueue(manager, day)) return;
                if (TryQueueRouteResetPrompt(manager, day)) return;
                DualDirector.ReconcileMainProgression(day);

                if (RomanceState.RomanceLocked) return;

                int chapter = PendingChapterForDay(day);
                if (chapter <= 0)
                {
                    if (MaidBusiness.TryQueue(manager,day)) return;
                    if (TryQueueEpilogue(manager, day)) RomanceState.LastQueuedDay = day;
                    return;
                }

                if (chapter == 11 && RomanceState.DPreludeState == 1 && RomanceState.DPreludeStep > 0
                    && !EleanorStory.HasPreludeBracelet())
                {
                    RomanceState.PendingChapter = 11;
                    AdvanceChapter(11, 12, EleanorStory.MarkPreludeBraceletUnavailable);
                    return;
                }

                StoreClient client = EleanorStory.CreateClient(chapter);
                if (client == null)
                {
                    LogError("CreateClient returned null for chapter " + chapter);
                    return;
                }

                ModHelper.SetClientSource(client, ModConstants.ModId);
                if (RomanceState.Route == 3 && RevolutionV2Story.UseMorningQueue(chapter))
                    manager.AddNextClient(client);
                else
                    manager.AddClient(client);

                RomanceState.PendingChapter = chapter;
                RomanceState.LastQueuedDay = day;
                MaidState.LastImportantDay = day;
                Log("Queued chapter " + chapter + " on day " + day + ".");
            }
            catch (Exception ex)
            {
                LogError("Queueing failed: " + ex);
            }
        }

        /// <summary>
        /// The native bell lock counts customer arrive/leave animations.  A
        /// save taken while that count is up restores it with no animation left
        /// to lower it, and an interrupted leave can leave it up across a day
        /// change.  With no client on screen there is nothing to protect, so
        /// clear the stale counter; a present client keeps its normal path.
        /// </summary>
        private static void RecoverStuckBellLock()
        {
            PlayerStore store = PlayerStore.Instance;
            if (store == null || !RomanceState.StoryActive) return;
            if (store.currentClientInstance != null || store.lockNextBellFlag <= 0) return;
            int previous = store.lockNextBellFlag;
            store.lockNextBellFlag = 0;
            Log("Recovered stuck bell lock (" + previous + ") with no current client.");
        }

        /// <summary>
        /// Returns the chapter that should appear today, or 0.  Chapters are
        /// gated by the previous wrap-up, not by a parallel absolute calendar.
        /// </summary>
        public static int PendingChapterForDay(int day)
        {
            if (!RomanceState.StoryActive || RomanceState.Chapter >= RomanceState.ChapterComplete) return 0;

            int chapter = RomanceState.PendingChapter > 0
                ? RomanceState.PendingChapter
                : RomanceState.Chapter == 0 ? 1 : RomanceState.Chapter;

            // The investigator's money can be completed by her own sales at any
            // time; the deadline visit only happens once 800 is reached or the
            // story's day 42 has arrived (her other work fills the gap).  A v2
            // player who already answered the detective-fee choice must not be
            // locked out while a declined/unsettled gap waits for day 42.
            bool maidGapAnswered = MaidV2State.GapPaid || MaidV2State.GapChoice != 0;
            if (chapter == 12 && !maidGapAnswered
                && RomanceState.Savings < 800 && day < 42 - StoryOffset()) return 0;

            bool attendantRoute = MaidState.Active;
            if (!attendantRoute && chapter == 18 && RomanceState.MotherState != 1) return 19;
            if (!attendantRoute && chapter == 19 && RomanceState.MotherState != 1) return 0;

            // v2: P2 is the first and only mainline Wilde project; Act 3 waits for it.
            if (attendantRoute && chapter == 20 && !MaidProjects.Paid(2)) return 0;

            if (chapter == 20 && RomanceState.Route == 3) return 0;
            // The dual extension replaces the c21/c22/c23 visits while it owns
            // the post-rescue schedule; its own director queues those scenes.
            if (DualDirector.SuppressMainChapter(chapter)) return 0;
            if (LoveDirector.SuppressMainChapter(chapter, day)) return 0;

            if (day < RomanceState.NextVisitDay) return 0;
            return chapter;
        }

        public static void AdvanceChapter(int expected, int next, Action flags = null)
        {
            if (RomanceState.PendingChapter != expected && RomanceState.Chapter != expected - 1)
            {
                Log("Ignoring out-of-order completion " + expected
                    + " (pending=" + RomanceState.PendingChapter + ", chapter=" + RomanceState.Chapter + ").");
                return;
            }

            flags?.Invoke();
            StoryCoreFacts.Record(expected);
            RomanceState.PendingChapter = 0;

            int day = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;

            if (next >= RomanceState.ChapterComplete)
            {
                CompleteCore();
                return;
            }

            RomanceState.Chapter = next;
            int due = Math.Max(TargetDay(next) - StoryOffset(), day + DelayAfter(next));
            due = Math.Max(1, due);
            RomanceState.NextVisitDay = due;
            Log("Chapter " + expected + " complete; next " + next + " due day " + due + ".");
        }

        public static int StoryOffset()
        {
            // Early Start begins on day 12 instead of day 23: shift the whole
            // chapter schedule by 11 days while keeping the same relative gaps.
            return RomanceState.EarlyStart ? 11 : 0;
        }

        public static int StoryDay(int day)
        {
            return day - StoryOffset();
        }

        public static void SetRoute(int route)
        {
            RomanceState.Route = route;
        }

        public static void CompleteCore()
        {
            RomanceState.Chapter = RomanceState.ChapterComplete;
            RomanceState.PendingChapter = 0;
            RomanceState.NextVisitDay = -1;
            RomanceState.EpilogueDone = true;
            RomanceState.LastEpilogueDay = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;

            bool partnerEnding = (RomanceState.Route == 1 || RomanceState.Route == 2)
                && RomanceState.MotherState == 1
                && RomanceState.RomanceConfirmed;

            // The harem ending page is its own completion signal.
            bool dualOwnsEnding = DualState.Writable && DualState.HasHistory && DualState.Active;

            if (partnerEnding)
            {
                // Unlock the native landfill event; this is no longer a permanent stat bonus.
                bool firstUnlock = !RomanceState.ScavAssist;
                RomanceState.ScavAssist = true;
                if (firstUnlock && !dualOwnsEnding) Notify(L.Get("er_scav_event_unlocked"));
            }

            if (partnerEnding && !RomanceState.TrueUnlocked)
            {
                RomanceState.TrueUnlocked = true;
                MetaProgress.UnlockTrueEnding();
                if (!dualOwnsEnding) Notify(L.Get("er_notice_true_unlock"));
            }

            Log("Core story complete. route=" + RomanceState.Route
                + ", mother=" + RomanceState.MotherState
                + ", romance=" + RomanceState.Romance);
        }

        public static void AbandonStory()
        {
            if (MaidState.HasHistory) MaidState.End("story-abandoned",4);
            RomanceState.RomanceLocked = true;
            RomanceState.Chapter = RomanceState.ChapterComplete;
            RomanceState.PendingChapter = 0;
            RomanceState.NextVisitDay = -1;
            Log("Story abandoned.");
        }

        private static bool TryQueueEpilogue(StoreClientManager manager, int day)
        {
            if (MaidState.Active) return false;
            if (LoveState.Act3Done) return false;
            // The dual extension supplies its own confirmed ending/epilogue;
            // an exited dual route falls back to the ordinary epilogue.
            if (DualState.Active && DualState.Relation != 3) return false;
            if (!RomanceState.EpilogueDone || RomanceState.RomanceLocked) return false;
            if (RomanceState.Route == 3) return false;
            if (RomanceState.MotherState != 1) return false;
            if (RomanceState.LastEpilogueDay >= 0 && day < RomanceState.LastEpilogueDay + 8) return false;

            int index = RomanceState.EpilogueIndex;
            int maxIndex = RomanceState.RomanceConfirmed ? 3 : 4;
            if (index > maxIndex) return false;

            StoreClient client = EleanorStory.CreateEpilogueClient(index);
            if (client == null) return false;

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            RomanceState.EpilogueIndex = index + 1;
            RomanceState.LastEpilogueDay = day;
            Log("Queued epilogue " + index + " on day " + day + ".");
            return true;
        }

        /// <summary>
        /// After any written route ending, the archivist offers to rewind the run
        /// to the common line.  The offer can be deferred or permanently
        /// suppressed; the reset itself clears all route state, keeps items and
        /// meta unlocks, and rebuilds new-game caches.
        /// </summary>
        private static bool TryQueueRouteResetPrompt(StoreClientManager manager, int day)
        {
            if (manager == null || RomanceState.RouteResetSuppressed) return false;
            PlayerStore store = PlayerStore.Instance;
            if (store == null || store.isClientBeingArrested || store.isHandlingShootout) return false;
            if (!EndingPages.HasExperiencedRouteEnding()) return false;
            if (RomanceState.RouteResetNextOfferDay >= 0 && day < RomanceState.RouteResetNextOfferDay) return false;
            if (RomanceState.LastQueuedDay == day) return false;
            if (HasModClient(manager)) return false;

            StoreClient client = EleanorStory.CreateRouteResetPromptClient();
            if (client == null) return false;

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddNextClient(client);
            // A queued prompt that is lost on save/load may be offered again
            // tomorrow; "later" advances this to day + 8 explicitly.
            RomanceState.RouteResetNextOfferDay = day + 1;
            RomanceState.LastQueuedDay = day;
            Log("Queued route-reset prompt on day " + day + ".");
            return true;
        }

        private static bool TryQueueOldSavePrompt(StoreClientManager manager)
        {
            if (RomanceState.OldPromptDone) return false;
            PlayerStore store = PlayerStore.Instance;
            if (store == null || store.endlessMode) return false;

            int day = StoreStation.Instance.dayCounter;
            if (RomanceState.LastQueuedDay == day) return false;
            if (HasModClient(manager)) return false;

            StoreClient client = EleanorStory.CreateOldSavePromptClient();
            if (client == null) return false;

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddNextClient(client);
            RomanceState.LastQueuedDay = day;
            Log("Queued old-save prompt on day " + day + ".");
            return true;
        }

        private static bool HasModClient(StoreClientManager manager)
        {
            StoreClient current=PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            if(current!=null && VanillaEleanorBridge.IsOurClient(current))return true;
            return manager.clientStack != null
                && manager.clientStack.Any(c => c != null
                    && !string.IsNullOrEmpty(c.identifier)
                    && VanillaEleanorBridge.IsOurClient(c));
        }

        /// <summary>Vanilla Eleanor9's perfume belongs to every successful ending.</summary>
        private static bool EndgamePerfumeDue => RomanceState.StoryActive && !RomanceState.PerfumeGiven
            && (StoryCoreFacts.PerfumePending
                || (MaidState.HasHistory && MaidState.BasicComplete)
                || LoveState.Act3Done
                || RomanceState.RevComplete
                || DualDirector.RouteStoryFinished);

        public static int TargetDay(int chapter)
        {
            switch (chapter)
            {
                case 1: return 23;
                case 2: return 25;
                case 3: return 27;
                case 4: return 28;
                case 5: return 29;
                case 6: return 30;
                case 7: return 31;
                case 8: return 33;
                case 9: return 35;
                case 10: return 36;
                case 11: return 37;
                case 12: return 39;
                case 13: return 41;
                case 14: return 43;
                case 15: return 44;
                case 16: return 45;
                case 17: return 46;
                case 18: return 48;
                case 19: return 50;
                case 20: return 52;
                case 21: return 54;
                case 22: return 55;
                case 23: return 57;
                case 24: return 59;
                case 25: return 61;
                case 26: return 63;
                case 27: return 65;
                case 28: return 67;
                case 280: return 46;
                case 281: return 48;
                case 282: return 51;
                case 283: return 54;
                case 284: return 57;
                case 285: return 60;
                case 286: return 62;
                case 287: return 64;
                case 288: return 66;
                case 289: return 69;
                case 290: return 72;
                default: return 0;
            }
        }

        private static int DelayAfter(int chapter)
        {
            switch (chapter)
            {
                case 4: return 1;
                case 5: return 1;
                case 6: return 1;
                case 15: return 1;
                case 16: return 1;
                default: return 1;
            }
        }

        public static void Notify(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            try
            {
                if (StoreUIManager.Instance != null) StoreUIManager.Instance.Notify(message);
            }
            catch (Exception ex)
            {
                LogError("Notify failed: " + ex);
            }
        }

        public static void Log(string message) => ModEntry.Log(message);
        public static void LogError(string message) => ModEntry.LogError(message);
    }
}
