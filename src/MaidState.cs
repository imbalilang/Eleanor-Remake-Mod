using System;
using System.Globalization;

namespace EleanorRomance
{
    /// <summary>Additive per-run state; the established global save schema remains 2.</summary>
    public static class MaidState
    {
        private const string Prefix = "er.maid.";
        public const int CurrentVersion = 1;
        public static int Int(string key, int fallback = 0) => ModHelper.GetModDataInt(ModConstants.ModId, Prefix + key, fallback);
        public static void SetInt(string key, int value) => ModHelper.SetModDataInt(ModConstants.ModId, Prefix + key, value);
        public static bool Flag(string key) => ModHelper.GetModDataBool(ModConstants.ModId, Prefix + key, false);
        public static void Flag(string key, bool value) => ModHelper.SetModDataBool(ModConstants.ModId, Prefix + key, value);
        public static string Text(string key) => ModHelper.GetModData(ModConstants.ModId, Prefix + key, "");
        public static void Text(string key, string value) => ModHelper.SetModData(ModConstants.ModId, Prefix + key, value);
        public static double Number(string key) => double.TryParse(Text(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0;
        public static void Number(string key, double value) => Text(key, value.ToString("R", CultureInfo.InvariantCulture));

        public static int FeatureVersion { get => Int("version"); set => SetInt("version", value); }
        // Contract: 0 absent, 1 active, 2 ended, 3 fulfilled and voluntarily renewed.
        public static int Contract { get => Int("contract"); set => SetInt("contract", value); }
        // Entry: 0 absent, 1 new cash payment, 2 verified old callback's waived entry.
        public static int EntryKind { get => Int("entry_kind"); set => SetInt("entry_kind", value); }
        public static int EntryPaidCash { get => Int("entry_cash"); set => SetInt("entry_cash", value); }
        // Confession: 0 not asked, 1 maid lovers, 2 ordinary lovers, 3 retired
        // deferred value (migrated on load), 4 friends.
        public static int Confession { get => Int("confession"); set => SetInt("confession", value); }
        public static bool HasHistory => EntryKind != 0;
        public static bool Active => HasHistory && FeatureVersion==CurrentVersion && (Contract == 1 || Contract == 3)
            && RomanceState.Route != 3 && RomanceState.Route != 4 && RomanceState.MotherState != 2;
        public static bool Lovers => Active && Confession == 1 && RomanceState.RomanceConfirmed;
        public static bool BasicComplete { get => Flag("basic_complete"); set => Flag("basic_complete", value); }
        public static int BasicDay { get => Int("basic_day", -1); set => SetInt("basic_day", value); }
        public static bool DebutShown { get => Flag("debut"); set => Flag("debut", value); }
        public static int MotivationVersion { get => Int("motivation.version"); set => SetInt("motivation.version",value); }
        public static bool AddressAccepted { get => Flag("motivation.address"); set => Flag("motivation.address",value); }
        public static int Progress { get => Int("progress", 23); set => SetInt("progress", value); }
        public static int ProgressDay { get => Int("progress_day", -1); set => SetInt("progress_day", value); }
        public static int PendingVisit { get => Int("pending_visit"); set => SetInt("pending_visit", value); }
        public static int NextVisitDay { get => Int("next_visit", -1); set => SetInt("next_visit", value); }
        public static int LastImportantDay { get => Int("last_important", -1); set => SetInt("last_important", value); }
        public static int LastChatDay { get => Int("last_chat", -1); set => SetInt("last_chat", value); }
        public static int LegacyResume { get => Int("legacy_resume"); set => SetInt("legacy_resume", value); }
        public static int LegacyDueDay { get => Int("legacy_due", -1); set => SetInt("legacy_due", value); }
        public static int BuyerState { get => Int("buyer_state"); set => SetInt("buyer_state", value); }
        public static int BuyerDueDay { get => Int("buyer_due", -1); set => SetInt("buyer_due", value); }
        public static bool BuyerTraded { get => Flag("buyer_traded"); set => Flag("buyer_traded", value); }
        public static bool BuyerFeedbackShown { get => Flag("buyer_feedback"); set => Flag("buyer_feedback", value); }
        public static bool SafeMother => RomanceState.MotherState == 1 && (RomanceState.Route == 1 || RomanceState.Route == 2);
        public static int Today => StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;

        public static void Enter(int kind)
        {
            FeatureVersion = CurrentVersion;
            EntryKind = kind;
            EntryPaidCash = kind == 1 ? 800 : 0;
            Contract = 1;
            MotivationVersion=1;
            RomanceState.Romance = 0;
        }

        public static void End(string reason, int relationship)
        {
            Contract = 2;
            Text("stop_reason", reason);
            Confession = relationship == 1 ? 2 : relationship == 2 ? 0 : 4;
            RomanceState.Romance = relationship;
            PendingVisit = 0;
            // A paid P1 retains its result even when the relationship changes.
        }
    }
}
