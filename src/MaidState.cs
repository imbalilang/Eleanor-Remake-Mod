using System;
using System.Globalization;

namespace EleanorRomance
{
    /// <summary>Additive per-run state; the established global save schema remains 2.</summary>
    public static class MaidState
    {
        private const string Prefix = "er.maid.";
        public const int V2Version = 2;
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
        // Entry: 0 absent, 3 hired after covering the detective fee.
        public static int EntryKind { get => Int("entry_kind"); set => SetInt("entry_kind", value); }
        public static int EntryPaidCash { get => Int("entry_cash"); set => SetInt("entry_cash", value); }
        // Confession: 0 not asked, 1 lovers.
        public static int Confession { get => Int("confession"); set => SetInt("confession", value); }
        public static bool HasHistory => EntryKind != 0;
        public static bool V2 => FeatureVersion == V2Version;
        public static bool Active => HasHistory && V2 && RomanceState.StoryActive
            && (Contract == 1 || Contract == 3)
            && RomanceState.Route != 3 && RomanceState.MotherState != 2;
        public static bool Lovers => Active && Confession == 1 && RomanceState.RomanceConfirmed;
        public static bool BasicComplete { get => Flag("basic_complete"); set => Flag("basic_complete", value); }
        public static int Progress { get => Int("progress", 23); set => SetInt("progress", value); }
        public static int ProgressDay { get => Int("progress_day", -1); set => SetInt("progress_day", value); }
        public static int PendingVisit { get => Int("pending_visit"); set => SetInt("pending_visit", value); }
        public static int NextVisitDay { get => Int("next_visit", -1); set => SetInt("next_visit", value); }
        public static int LastImportantDay { get => Int("last_important", -1); set => SetInt("last_important", value); }
        public static bool SafeMother => RomanceState.MotherState == 1 && (RomanceState.Route == 1 || RomanceState.Route == 2);
        public static int Today => StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;

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
