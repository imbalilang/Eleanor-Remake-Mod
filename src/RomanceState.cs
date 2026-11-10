using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Per-run story state.  The v0.6 script separates mother fate, research
    /// custody, romance status and work status; those are stored separately so
    /// one boolean never has to mean five different things.
    /// </summary>
    public static class RomanceState
    {
        private const string Prefix = "er.";
        public const int Schema = 2;

        // Chapters 1..24 are the common/route A/B/friends spine.
        // 101..108 are route C (public leak), 201..204 route D (betrayal/exit).
        public const int ChapterComplete = 300;
        public const int RouteCFirst = 101;
        public const int RouteDFirst = 201;

        public static bool StoryActive => Initialized && !OldBlocked
            && VersionGate.IsSupported && SchemaVersion == Schema;

        public static bool IncompatibleSave => SchemaVersion != Schema
            && ModHelper.GetModDataKeys(ModConstants.ModId).Count > 0;

        public static int SchemaVersion
        {
            get => GetInt("schema", 0);
            set => SetInt("schema", value);
        }

        public static bool Initialized
        {
            get => GetBool("initialized", false);
            set => SetBool("initialized", value);
        }

        public static bool OldBlocked
        {
            get => GetBool("old_blocked", false);
            set => SetBool("old_blocked", value);
        }

        public static bool OldPromptDone
        {
            get => GetBool("old_prompt_done", false);
            set => SetBool("old_prompt_done", value);
        }

        public static bool EarlyStart
        {
            get => GetBool("scenario_boost", false);
            set => SetBool("scenario_boost", value);
        }

        public static int Chapter
        {
            get => GetInt("chapter", 0);
            set => SetInt("chapter", value);
        }

        public static int PendingChapter
        {
            get => GetInt("pending_chapter", 0);
            set => SetInt("pending_chapter", value);
        }

        public static int Affection
        {
            get => GetInt("affection", 0);
            set => SetInt("affection", Mathf.Clamp(value, 0, 99));
        }

        public static int NextVisitDay
        {
            get => GetInt("next_visit_day", -1);
            set => SetInt("next_visit_day", value);
        }

        public static int LastQueuedDay
        {
            get => GetInt("last_queued_day", -1);
            set => SetInt("last_queued_day", value);
        }

        // --- Behaviour facts -------------------------------------------------
        public static int FairDeals
        {
            get => GetInt("fair_deals", 0);
            set => SetInt("fair_deals", Mathf.Max(0, value));
        }

        public static int Discounts
        {
            get => GetInt("discounts", 0);
            set => SetInt("discounts", Mathf.Max(0, value));
        }

        public static int Overcharges
        {
            get => GetInt("overcharges", 0);
            set => SetInt("overcharges", Mathf.Max(0, value));
        }

        public static int Lowballs
        {
            get => GetInt("lowballs", 0);
            set => SetInt("lowballs", Mathf.Max(0, value));
        }

        public static int NoSaleVisits
        {
            get => GetInt("no_sale_visits", 0);
            set => SetInt("no_sale_visits", Mathf.Max(0, value));
        }

        public static int Savings
        {
            get => GetInt("savings", 0);
            set
            {
                SetInt("savings", Mathf.Clamp(value, 0, 800));
                SyncNativeSavings();
            }
        }

        /// <summary>
        /// The original Eleanor storyline reads StoreClientData.eleanorSavings
        /// for its 800-credit progress.  Keep it in lockstep with the remake's
        /// state so native checks/tooltips and old-save bridges see the same
        /// amount even while the remake owns the chapters.
        /// </summary>
        public static void SyncNativeSavings()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                if (store == null || store.storeClientData == null) return;
                store.storeClientData.eleanorSavings = Savings;
            }
            catch
            {
                // Early load / tests may not have a PlayerStore yet.
            }
        }

        public static bool WarningGiftGiven
        {
            get => GetBool("warning_gift_given", false);
            set => SetBool("warning_gift_given", value);
        }

        // 0 undecided, 1 gift accepted, 2 bought for 20, 3 declined.
        // Persist delivery before playing the response so an interrupted visit
        // can resume without giving or charging a second time.
        public static int ReturnGiftChoice
        {
            get => GetInt("return_gift_choice", 0);
            set => SetInt("return_gift_choice", value);
        }

        public static bool HardshipAidGiven
        {
            get => GetBool("hardship_aid_given", false);
            set => SetBool("hardship_aid_given", value);
        }

        public static bool HardshipCashGiven
        {
            get => GetBool("hardship_cash_given", false);
            set => SetBool("hardship_cash_given", value);
        }

        public static bool NativeRescueLinked
        {
            get => GetBool("native_rescue_linked", false);
            set => SetBool("native_rescue_linked", value);
        }

        public static bool SavingsAutoFilled
        {
            get => GetBool("savings_auto_filled", false);
            set => SetBool("savings_auto_filled", value);
        }

        public static bool BelongingsStockSpawned
        {
            get => GetBool("belongings_stock_spawned", false);
            set => SetBool("belongings_stock_spawned", value);
        }

        public static bool ScavStockSpawned1
        {
            get => GetBool("scav_stock_spawned_1", false);
            set => SetBool("scav_stock_spawned_1", value);
        }

        public static bool ScavStockSpawned2
        {
            get => GetBool("scav_stock_spawned_2", false);
            set => SetBool("scav_stock_spawned_2", value);
        }

        public static bool ScavStockSpawned19
        {
            get => GetBool("scav_stock_spawned_19", false);
            set => SetBool("scav_stock_spawned_19", value);
        }

        public static bool LydiaBraceletSpawned
        {
            get => GetBool("lydia_bracelet_spawned", false);
            set => SetBool("lydia_bracelet_spawned", value);
        }

        public static bool UncleAccepted
        {
            get => GetBool("uncle_accepted", false);
            set => SetBool("uncle_accepted", value);
        }

        public static bool UncleInspected
        {
            get => GetBool("uncle_inspected", false);
            set => SetBool("uncle_inspected", value);
        }

        public static bool UncleReported
        {
            get => GetBool("uncle_reported", false);
            set => SetBool("uncle_reported", value);
        }

        public static bool UncleWarned
        {
            get => GetBool("uncle_warned", false);
            set => SetBool("uncle_warned", value);
        }

        public static bool CrateKept
        {
            get => GetBool("crate_kept", false);
            set => SetBool("crate_kept", value);
        }

        public static bool CrateSold
        {
            get => GetBool("crate_sold", false);
            set => SetBool("crate_sold", value);
        }

        public static bool TrapSprung
        {
            get => GetBool("trap_sprung", false);
            set => SetBool("trap_sprung", value);
        }

        public static bool Arrested
        {
            get => GetBool("arrested", false);
            set => SetBool("arrested", value);
        }

        public static bool Rescued
        {
            get => GetBool("rescued", false);
            set => SetBool("rescued", value);
        }

        public static bool KnewTrapUnwarned
        {
            get => GetBool("knew_trap_unwarned", false);
            set => SetBool("knew_trap_unwarned", value);
        }

        public static int RescueMethod
        {
            get => GetInt("rescue_method", 0);
            set => SetInt("rescue_method", value);
        }

        public static int TrapResumeChapter
        {
            get => GetInt("trap_resume_chapter", 6);
            set => SetInt("trap_resume_chapter", value);
        }

        public static bool ProtectionFailed
        {
            get => GetBool("protection_failed", false);
            set => SetBool("protection_failed", value);
        }

        public static int BetrayalKind
        {
            get => GetInt("betrayal_kind", 0);
            set => SetInt("betrayal_kind", value);
        }

        // 0 none, 1 player custody, 2 Eleanor custody, 3 returned (route A),
        // 4 protected/public (route B), 5 leaked (route C), 6 sold (route D),
        // 7 abandoned.
        public static int DriveState
        {
            get => GetInt("drive_state", 0);
            set => SetInt("drive_state", value);
        }

        public static bool HasDriveWithPlayer => DriveState == 1;

        public static bool ThreatRecorded
        {
            get => GetBool("threat_recorded", false);
            set => SetBool("threat_recorded", value);
        }

        public static int ProtectionChannel
        {
            get => GetInt("protection_channel", 0);
            set => SetInt("protection_channel", value);
        }

        public static bool ProtectionReady
        {
            get => GetBool("protection_ready", false);
            set => SetBool("protection_ready", value);
        }

        // 0 in custody, 1 safe in lower district, 2 dead, 3 released but estranged.
        public static int MotherState
        {
            get => GetInt("mother_state", 0);
            set => SetInt("mother_state", value);
        }

        public static bool MotherVisited
        {
            get => GetBool("mother_visited", false);
            set => SetBool("mother_visited", value);
        }

        // 1 route A (return), 2 route B (protect then publish), 3 route C
        // (publish without protection), 4 route D (betrayal/exit), 5 friends.
        public static int Route
        {
            get => GetInt("route", 0);
            set => SetInt("route", value);
        }

        // 0 developing, 1 confirmed, 2 slow, 3 friends, 4 locked.
        public static int Romance
        {
            get => GetInt("romance", 0);
            set => SetInt("romance", value);
        }

        public static bool RomanceConfirmed => Romance == 1;

        public static bool RomanceLocked
        {
            get => Romance == 4;
            set => Romance = value ? 4 : (Romance == 4 ? 0 : Romance);
        }

        public static int WorkState
        {
            get => GetInt("work_state", 0);
            set => SetInt("work_state", value);
        }

        public static bool LocketWithEleanor
        {
            get => GetBool("locket_with_eleanor", true);
            set => SetBool("locket_with_eleanor", value);
        }

        public static bool LocketSold
        {
            get => GetBool("locket_sold", false);
            set => SetBool("locket_sold", value);
        }

        public static bool LocketGiven
        {
            get => GetBool("locket_given", false);
            set => SetBool("locket_given", value);
        }

        public static bool PerfumeGiven
        {
            get => GetBool("perfume_given", false);
            set => SetBool("perfume_given", value);
        }

        // Backward-compatible unlock flag for the native landfill event.
        // Its presence alone no longer modifies scavenging attempts.
        public static bool ScavAssist
        {
            get => GetBool("scav_assist", false);
            set => SetBool("scav_assist", value);
        }

        public static bool LydiaFairBracelet
        {
            get => GetBool("lydia_fair_bracelet", false);
            set => SetBool("lydia_fair_bracelet", value);
        }

        public static bool LydiaVisited
        {
            get => GetBool("lydia_visited", false);
            set => SetBool("lydia_visited", value);
        }

        public static bool EpilogueDone
        {
            get => GetBool("epilogue_done", false);
            set => SetBool("epilogue_done", value);
        }

        public static int EpilogueIndex
        {
            get => GetInt("epilogue_index", 0);
            set => SetInt("epilogue_index", value);
        }

        public static int LastEpilogueDay
        {
            get => GetInt("last_epilogue_day", -1);
            set => SetInt("last_epilogue_day", value);
        }

        public static bool TrueUnlocked
        {
            get => GetBool("true_unlocked", false);
            set => SetBool("true_unlocked", value);
        }

        public static void AddAffection(int delta)
        {
            Affection = Affection + delta;
        }

        public static void RecordDeal(char kind)
        {
            switch (kind)
            {
                case 'F': FairDeals++; break;
                case 'D': Discounts++; break;
                case 'O': Overcharges++; break;
                case 'L': Lowballs++; break;
            }
        }

        public static void InitializeForSave()
        {
            if (Initialized)
            {
                if (OldBlocked) ModEntry.Log("Vanilla Eleanor progress found; story takeover disabled.");
                return;
            }

            if (HasOriginalEleanorProgress())
            {
                Initialized = true;
                SchemaVersion = Schema;
                OldBlocked = true;
                ModEntry.Log("Existing vanilla Eleanor progress detected; original line kept.");
                StoryDirector.Notify(L.Get("er_notice_old_save"));
                return;
            }

            ResetForNewGame(StartingPerkRegistration.IsEarlyStartRun());
        }

        public static void ResetForNewGame(bool earlyStart)
        {
            bool keepMeta = TrueUnlocked;
            ModHelper.ClearModData(ModConstants.ModId);

            SchemaVersion = Schema;
            Initialized = true;
            OldBlocked = false;
            OldPromptDone = false;
            EarlyStart = earlyStart;
            Chapter = 0;
            PendingChapter = 0;
            Affection = 0;
            NextVisitDay = earlyStart ? 12 : 23;
            LastQueuedDay = -1;
            FairDeals = 0;
            Discounts = 0;
            Overcharges = 0;
            Lowballs = 0;
            NoSaleVisits = 0;
            Savings = 0;
            SavingsAutoFilled = false;
            BelongingsStockSpawned = false;
            ScavStockSpawned1 = false;
            ScavStockSpawned2 = false;
            ScavStockSpawned19 = false;
            LydiaBraceletSpawned = false;
            UncleAccepted = false;
            UncleInspected = false;
            UncleReported = false;
            UncleWarned = false;
            WarningGiftGiven = false;
            ReturnGiftChoice = 0;
            HardshipAidGiven = false;
            HardshipCashGiven = false;
            NativeRescueLinked = false;
            CrateKept = false;
            CrateSold = false;
            TrapSprung = false;
            Arrested = false;
            Rescued = false;
            KnewTrapUnwarned = false;
            RescueMethod = 0;
            TrapResumeChapter = 6;
            ProtectionFailed = false;
            BetrayalKind = 0;
            DriveState = 0;
            ThreatRecorded = false;
            ProtectionChannel = 0;
            ProtectionReady = false;
            MotherState = 0;
            MotherVisited = false;
            Route = 0;
            Romance = 0;
            WorkState = 0;
            LocketWithEleanor = true;
            LocketSold = false;
            LocketGiven = false;
            PerfumeGiven = false;
            ScavAssist = false;
            LydiaFairBracelet = false;
            LydiaVisited = false;
            EpilogueDone = false;
            EpilogueIndex = 0;
            LastEpilogueDay = -1;
            TrueUnlocked = keepMeta;

            ModEntry.Log("Story state initialized. earlyStart=" + earlyStart + ", next=" + NextVisitDay);
        }

        public static bool HasOriginalEleanorProgress()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                if (store == null) return false;

                StoreClientData data = store.storeClientData;
                if (data != null)
                {
                    if (data.eleanorConclusionDay >= 0
                        || data.eleanorChoiceDay >= 0
                        || data.eleanorDriveVisitDay >= 0
                        || data.eleanorThreatVisitDay >= 0
                        || data.eleanorPlanDay >= 0
                        || data.eleanorSavingsDoneDay >= 0
                        || data.eleanorWarnedDay >= 0
                        || data.eleanorSavings > 0
                        || data.eleanorHelpedTimes > 0
                        || data.eleanorArrested
                        || data.isEleanorDead
                        || data.eleanorUncleReported)
                    {
                        return true;
                    }
                }

                StoreClientManager manager = store.storeClientManager;
                if (manager != null
                    && (HasEleanorTracker(manager.clientTracker)
                        || HasEleanorTracker(manager.permanentClientTracker)
                        || HasEleanorTracker(manager.uniqueClientTracker)))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Old-save detection failed: " + ex);
            }

            return false;
        }

        public static void ManualResetOldSave()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                if (store != null)
                {
                    StoreClientData data = store.storeClientData;
                    if (data != null)
                    {
                        data.eleanorFund = 500;
                        data.eleanorHelpedTimes = 0;
                        data.eleanorArrested = false;
                        data.isEleanorDead = false;
                        data.eleanorArrestDay = -1;
                        data.eleanorArrestVisit = 0;
                        data.eleanorBailChoice = 0;
                        data.eleanorBailDay = -1;
                        data.eleanorFreedVisitDay = -1;
                        data.eleanorUncleReported = false;
                        data.eleanorWarnedDay = -1;
                        data.eleanorSavings = 0;
                        data.eleanorPlanDay = -1;
                        data.eleanorScavLastDay = -1;
                        data.eleanorSavingsDoneDay = -1;
                        data.eleanorDriveVisitDay = -1;
                        data.eleanorThreatVisitDay = -1;
                        data.eleanorDriveChoice = 0;
                        data.eleanorChoiceDay = -1;
                        data.eleanorConclusionDay = -1;
                    }

                    StoreClientManager manager = store.storeClientManager;
                    if (manager != null)
                    {
                        RemoveTracker(manager.clientTracker);
                        RemoveTracker(manager.permanentClientTracker);
                        RemoveTracker(manager.uniqueClientTracker);
                        if (manager.clientStack != null)
                        {
                            manager.clientStack.RemoveAll(c => c != null && IsModClientId(c.identifier));
                        }
                        manager.arrestedClients.RemoveAll(IsModClientId);
                        manager.spriteShownHistory.RemoveAll(IsModClientId);
                        if (manager.spriteLastShownDay != null)
                        {
                            foreach (string key in manager.spriteLastShownDay.Keys.ToList())
                            {
                                if (IsModClientId(key)) manager.spriteLastShownDay.Remove(key);
                            }
                        }
                    }
                }

                ModHelper.ClearModData(ModConstants.ModId);
                Initialized = false;
                OldBlocked = false;
                InitializeForSave();
                StoryDirector.Notify(L.Get("er_notice_reset_done"));
                ModEntry.Log("Manual Eleanor story reset completed.");
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Manual reset failed: " + ex);
            }
        }

        private static bool HasEleanorTracker(Dictionary<string, int> tracker)
        {
            return tracker != null && tracker.Keys.Any(IsModClientId);
        }

        private static void RemoveTracker(Dictionary<string, int> tracker)
        {
            if (tracker == null) return;
            foreach (string key in tracker.Keys.ToList())
            {
                if (IsModClientId(key)) tracker.Remove(key);
            }
        }

        /// <summary>Only this mod's clients and the vanilla Eleanor line, never other mods.</summary>
        private static bool IsModClientId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return id.StartsWith("er_story_", StringComparison.Ordinal)
                || id.StartsWith("eleanor", StringComparison.OrdinalIgnoreCase);
        }

        private static bool GetBool(string key, bool fallback) => ModHelper.GetModDataBool(ModConstants.ModId, Prefix + key, fallback);
        private static void SetBool(string key, bool value) => ModHelper.SetModDataBool(ModConstants.ModId, Prefix + key, value);
        private static int GetInt(string key, int fallback) => ModHelper.GetModDataInt(ModConstants.ModId, Prefix + key, fallback);
        private static void SetInt(string key, int value) => ModHelper.SetModDataInt(ModConstants.ModId, Prefix + key, value);
    }
}
