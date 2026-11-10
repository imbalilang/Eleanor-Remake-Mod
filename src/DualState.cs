using System;

namespace EleanorRomance
{
    /// <summary>
    /// Independent state for the Lydia dual-romance extension. It deliberately
    /// stays out of RomanceState.Schema so older saves keep loading.
    /// </summary>
    public static class DualState
    {
        private const string Prefix = "er.dual.";
        public const int CurrentVersion = 2;

        public static int Int(string key, int fallback = 0) => ModHelper.GetModDataInt(ModConstants.ModId, Prefix + key, fallback);
        public static void SetInt(string key, int value) { if (Writable) ModHelper.SetModDataInt(ModConstants.ModId, Prefix + key, value); }
        public static bool Flag(string key) => ModHelper.GetModDataBool(ModConstants.ModId, Prefix + key, false);
        public static void Flag(string key, bool value) { if (Writable) ModHelper.SetModDataBool(ModConstants.ModId, Prefix + key, value); }
        public static string Text(string key) => ModHelper.GetModData(ModConstants.ModId, Prefix + key, "");
        public static void Text(string key, string value) { if (Writable) ModHelper.SetModData(ModConstants.ModId, Prefix + key, value); }
        public static bool Writable => FeatureVersion <= CurrentVersion;
        public static string PendingId { get => Text("pending_id"); set => Text("pending_id", value); }

        public static int FeatureVersion { get => Int("version"); set => SetInt("version", value); }
        // 0 not offered, 1 accepted from the pre-confession path,
        // 2 accepted from an old lover save, 3 explicitly declined.
        public static int Entry { get => Int("entry"); set => SetInt("entry", value); }
        // 0 undiscussed, 1 talked, 2 confirmed, 3 rejected/withdrawn.
        public static int Relation { get => Int("relation"); set => SetInt("relation", value); }

        public static int LastImportantDay { get => Int("last_important", -1); set => SetInt("last_important", value); }
        public static int NextSceneDay { get => Int("next_scene_day", -1); set => SetInt("next_scene_day", value); }
        public static int PendingScene { get => Int("pending_scene"); set => SetInt("pending_scene", value); }

        public static bool PlaqueReceived { get => Flag("plaque.received"); set => Flag("plaque.received", value); }
        public static bool PlaqueLost { get => Flag("plaque.lost"); set => Flag("plaque.lost", value); }
        public static int PlaqueRetryDay { get => Int("plaque.retry_day", -1); set => SetInt("plaque.retry_day", value); }

        // 0 absent, 1 arranged, 2 arrived, 3 traded, 4 ended without a trade.
        public static int FirstBusiness { get => Int("biz.first"); set => SetInt("biz.first", value); }
        public static int SecondBusiness { get => Int("biz.second"); set => SetInt("biz.second", value); }
        public static bool FirstTraded { get => Flag("biz.first.traded"); set => Flag("biz.first.traded", value); }
        public static bool SecondTraded { get => Flag("biz.second.traded"); set => Flag("biz.second.traded", value); }
        public static bool SupplyOffered { get => Flag("biz.supply"); set => Flag("biz.supply", value); }
        public static int BusinessDueDay { get => Int("biz.due", -1); set => SetInt("biz.due", value); }
        public static int BusinessOfferDay { get => Int("biz.offer", -1); set => SetInt("biz.offer", value); }

        public static bool FeedbackPending { get => Flag("feedback.pending"); set => Flag("feedback.pending", value); }
        public static bool FeedbackShown { get => Flag("feedback.shown"); set => Flag("feedback.shown", value); }
        public static int ReferralDay { get => Int("referral.day", -1); set => SetInt("referral.day", value); }
        public static bool ReferralRoll { get => Flag("referral.roll"); set => Flag("referral.roll", value); }
        public static int LastExtraBuyerDay { get => Int("referral.last_buyer", -1); set => SetInt("referral.last_buyer", value); }
        public static bool ReferralBlockedToday { get => Flag("referral.blocked"); set => Flag("referral.blocked", value); }

        public static int CooldownUntil { get => Int("cooldown.until", -1); set => SetInt("cooldown.until", value); }
        public static bool CoreTakenOver { get => Flag("core.taken_over"); set => Flag("core.taken_over", value); }
        public static bool OriginalCooperationDone { get => Flag("core.cooperation_done"); set => Flag("core.cooperation_done", value); }
        public static bool CoreCommitted { get => Flag("core.committed"); set => Flag("core.committed", value); }
        public static int MigrationVersion { get => Int("migration"); set => SetInt("migration", value); }
        public static int ScavSource { get => Int("scav.source"); set => SetInt("scav.source", value); }
        public static int DailyIndex { get => Int("daily.index"); set => SetInt("daily.index", value); }
        public static int LastDailyDay { get => Int("daily.last_day", -1); set => SetInt("daily.last_day", value); }

        public static bool HasHistory => Entry == 1 || Entry == 2;
        public static bool Active => FeatureVersion == CurrentVersion && HasHistory && Relation != 3
            && RomanceState.MotherState == 1
            && (RomanceState.Route == 1 || RomanceState.Route == 2)
            && !MaidState.Active
            && !RomanceState.RomanceLocked
            && RomanceState.Romance != 4;
        public static bool Confirmed => Active && Relation == 2;
        public static bool Ended => Relation == 3 || Entry == 3;
        /// <summary>The plaque keeps its base referral effect after the romance ends.</summary>
        public static bool PlaqueActive => FeatureVersion == CurrentVersion && PlaqueReceived;

        public static bool Done(string scene) => Flag("scene." + scene);
        public static void Complete(string scene)
        {
            Flag("scene." + scene, true);
            SetSceneDay(scene, Today);
            if (scene != "daily" && !scene.StartsWith("wait_", StringComparison.Ordinal)
                && scene != "claim" && scene != "coop_buyer") LastImportantDay = Today;
        }
        public static int SceneDay(string scene) => Int("scene_day." + scene, -1);
        public static void SetSceneDay(string scene, int day) => SetInt("scene_day." + scene, day);

        public static int Today => StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;

        public static void Accept(int entryKind)
        {
            FeatureVersion = CurrentVersion;
            Entry = entryKind;
            Relation = 0;
            MigrationVersion = CurrentVersion;
            Flag("entry.explicit", true);
        }

        public static void Reset()
        {
            DualShowcaseEffects.ClearMemory();
            var keys = new System.Collections.Generic.List<string>(ModHelper.GetModDataKeys(ModConstants.ModId));
            foreach (string key in keys)
                if (key.StartsWith(Prefix, StringComparison.Ordinal))
                    ModHelper.SetModData(ModConstants.ModId, key, "");
            // Only the dual extension's own keys are cleared. Global story facts
            // and the ordinary romance are intentionally untouched.
            SetInt("version", 0);
            SetInt("entry", 0);
            SetInt("relation", 0);
            SetInt("last_important", -1);
            SetInt("next_scene_day", -1);
            SetInt("pending_scene", 0);
            Flag("plaque.received", false);
            Flag("plaque.lost", false);
            SetInt("plaque.retry_day", -1);
            SetInt("biz.first", 0);
            SetInt("biz.second", 0);
            Flag("biz.first.traded", false);
            Flag("biz.second.traded", false);
            Flag("biz.supply", false);
            SetInt("biz.due", -1);
            SetInt("biz.offer", -1);
            Flag("feedback.pending", false);
            Flag("feedback.shown", false);
            SetInt("referral.day", -1);
            Flag("referral.roll", false);
            SetInt("referral.last_buyer", -1);
            Flag("referral.blocked", false);
            SetInt("cooldown.until", -1);
            Flag("core.taken_over", false);
            Flag("core.cooperation_done", false);
            Flag("core.committed", false);
            SetInt("migration", 0);
            SetInt("scav.source", 0);
            SetInt("daily.index", 0);
            SetInt("daily.last_day", -1);
            PendingId = "";
        }
    }
}
