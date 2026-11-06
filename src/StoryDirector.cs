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
        public static bool ShouldSuppressOriginal => RomanceState.StoryActive;

        public static void OnGameLoadedInit()
        {
            VersionGate.Refresh();
            PortraitCompat.Refresh();
            PortraitCompat.SanitizeLoadedClients();
            RomanceState.InitializeForSave();

            if (RomanceState.Initialized && !RomanceState.OldBlocked
                && RomanceState.SchemaVersion != RomanceState.Schema)
            {
                ModEntry.Log("Old mod save schema " + RomanceState.SchemaVersion
                    + "; resetting to schema " + RomanceState.Schema + ".");
                RomanceState.ResetForNewGame(false);
            }

            if (!VersionGate.IsSupported)
            {
                Notify(L.Get("er_notice_version"));
            }
        }

        public static void OnNewGameStarted(string scenarioId)
        {
            VersionGate.Refresh();
            if (!VersionGate.IsSupported) return;

            if (!RomanceState.Initialized)
            {
                RomanceState.ResetForNewGame(scenarioId == ModConstants.ScenarioId);
            }

            ModEntry.Log("New game. scenario=" + scenarioId + ", chapter=" + RomanceState.Chapter
                + ", next=" + RomanceState.NextVisitDay);
        }

        public static void OnGenerateCustomerVeryEarly(StoreClientManager manager)
        {
            if (manager == null) return;

            try
            {
                PortraitCompat.SanitizeLoadedClients();

                if (RomanceState.OldBlocked)
                {
                    TryQueueOldSavePrompt(manager);
                    return;
                }

                if (!RomanceState.StoryActive) return;

                int day = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
                if (RomanceState.LastQueuedDay == day) return;
                if (HasModClient(manager)) return;

                int chapter = PendingChapterForDay(day);
                if (chapter <= 0)
                {
                    if (TryQueueEpilogue(manager, day)) RomanceState.LastQueuedDay = day;
                    return;
                }

                StoreClient client = EleanorStory.CreateClient(chapter);
                if (client == null)
                {
                    LogError("CreateClient returned null for chapter " + chapter);
                    return;
                }

                ModHelper.SetClientSource(client, ModConstants.ModId);
                manager.AddClient(client);

                RomanceState.PendingChapter = chapter;
                RomanceState.LastQueuedDay = day;
                Log("Queued chapter " + chapter + " on day " + day + ".");
            }
            catch (Exception ex)
            {
                LogError("Queueing failed: " + ex);
            }
        }

        /// <summary>
        /// Returns the chapter that should appear today, or 0.  Chapters are
        /// gated by the previous wrap-up, not by a parallel absolute calendar.
        /// </summary>
        public static int PendingChapterForDay(int day)
        {
            if (RomanceState.Chapter >= RomanceState.ChapterComplete) return 0;

            int chapter = RomanceState.PendingChapter > 0
                ? RomanceState.PendingChapter
                : RomanceState.Chapter == 0 ? 1 : RomanceState.Chapter;

            // The investigator's money can be completed by her own sales at any
            // time; the deadline visit only happens once 800 is reached or the
            // story's day 42 has arrived (her other work fills the gap).
            if (chapter == 12 && RomanceState.Savings < 800 && day < 42 - StoryOffset()) return 0;

            if (chapter == 18 && RomanceState.MotherState != 1) return 19;
            if (chapter == 19 && RomanceState.MotherState != 1) return 0;

            if (chapter == 20 && RomanceState.Route == 3) return 0;

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
            return RomanceState.ScenarioBoost ? 11 : 0;
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

            if (partnerEnding)
            {
                // Endgame reward D: Eleanor helps at the dumping ground.
                RomanceState.ScavAssist = true;
            }

            if (partnerEnding && !RomanceState.TrueUnlocked)
            {
                RomanceState.TrueUnlocked = true;
                MetaProgress.UnlockTrueEnding();
                ScenarioRegistration.RefreshAfterUnlock();
            }

            Log("Core story complete. route=" + RomanceState.Route
                + ", mother=" + RomanceState.MotherState
                + ", romance=" + RomanceState.Romance);
        }

        public static void AbandonStory()
        {
            RomanceState.RomanceLocked = true;
            RomanceState.Chapter = RomanceState.ChapterComplete;
            RomanceState.PendingChapter = 0;
            RomanceState.NextVisitDay = -1;
            Log("Story abandoned.");
        }

        public static void ApplyScenarioStart()
        {
            VersionGate.Refresh();
            RomanceState.ResetForNewGame(true);
            Notify(L.Get("er_notice_scenario_started"));
            Log("Old Echo experimental scenario started at day 5.");
        }

        private static bool TryQueueEpilogue(StoreClientManager manager, int day)
        {
            if (!RomanceState.EpilogueDone || RomanceState.RomanceLocked) return false;
            if (RomanceState.Route == 3 || RomanceState.Route == 4) return false;
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
            return manager.clientStack != null
                && manager.clientStack.Any(c => c != null
                    && !string.IsNullOrEmpty(c.identifier)
                    && c.identifier.StartsWith("er_story_", StringComparison.Ordinal));
        }

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
                case 101: return 46;
                case 102: return 48;
                case 103: return 50;
                case 104: return 52;
                case 105: return 54;
                case 106: return 55;
                case 107: return 57;
                case 108: return 59;
                case 201: return 45;
                case 202: return 46;
                case 203: return 48;
                case 204: return 50;
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
