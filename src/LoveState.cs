using System;

namespace EleanorRomance
{
    /// <summary>
    /// State for the solo Eleanor romance line that starts after the C20
    /// confession. Kept out of RomanceState.Schema so old saves keep loading.
    /// </summary>
    public static class LoveState
    {
        private const string Prefix = "er.love.";
        public const int CurrentVersion = 1;

        public static int Int(string key, int fallback = 0) => ModHelper.GetModDataInt(ModConstants.ModId, Prefix + key, fallback);
        public static void SetInt(string key, int value) { if (Writable) ModHelper.SetModDataInt(ModConstants.ModId, Prefix + key, value); }
        public static bool Flag(string key) => ModHelper.GetModDataBool(ModConstants.ModId, Prefix + key, false);
        public static void Flag(string key, bool value) { if (Writable) ModHelper.SetModDataBool(ModConstants.ModId, Prefix + key, value); }
        public static string Text(string key) => ModHelper.GetModData(ModConstants.ModId, Prefix + key, "");
        public static void Text(string key, string value) { if (Writable) ModHelper.SetModData(ModConstants.ModId, Prefix + key, value); }

        public static bool Writable => FeatureVersion <= CurrentVersion;
        public static int FeatureVersion { get => Int("version"); set => SetInt("version", value); }
        public static bool Started => FeatureVersion == CurrentVersion;
        public static bool Active => Started && !Act3Done;

        public static int LastImportantDay { get => Int("last_important", -1); set => SetInt("last_important", value); }
        public static int NextVisitDay { get => Int("next_visit_day", -1); set => SetInt("next_visit_day", value); }
        public static int PendingVisit { get => Int("pending_visit"); set => SetInt("pending_visit", value); }

        public static bool T1Done { get => Flag("a1.t1.done"); set => Flag("a1.t1.done", value); }
        public static bool T2Done { get => Flag("a1.t2.done"); set => Flag("a1.t2.done", value); }
        public static bool ClosingDone { get => Flag("a1.closing.done"); set => Flag("a1.closing.done", value); }
        public static bool Act1Done { get => Flag("a1.done"); set => Flag("a1.done", value); }
        public static bool T1Traded { get => Flag("a1.t1.traded"); set => Flag("a1.t1.traded", value); }
        public static bool T2Traded { get => Flag("a1.t2.traded"); set => Flag("a1.t2.traded", value); }

        // 0 no sale, 1 fair, 2 discount, 3 overcharge.
        public static int T1Outcome { get => Int("a1.t1.outcome"); set => SetInt("a1.t1.outcome", value); }
        public static int T2Outcome { get => Int("a1.t2.outcome"); set => SetInt("a1.t2.outcome", value); }

        public static bool A2T1Done { get => Flag("a2.t1.done"); set => Flag("a2.t1.done", value); }
        public static bool A2T2Done { get => Flag("a2.t2.done"); set => Flag("a2.t2.done", value); }
        public static bool A2T3Done { get => Flag("a2.t3.done"); set => Flag("a2.t3.done", value); }
        public static bool Act2Done { get => Flag("a2.done"); set => Flag("a2.done", value); }
        public static bool A2T1Traded { get => Flag("a2.t1.traded"); set => Flag("a2.t1.traded", value); }
        public static bool A2T2Traded { get => Flag("a2.t2.traded"); set => Flag("a2.t2.traded", value); }
        public static bool A2T3Traded { get => Flag("a2.t3.traded"); set => Flag("a2.t3.traded", value); }
        public static int A2T1Outcome { get => Int("a2.t1.outcome"); set => SetInt("a2.t1.outcome", value); }
        public static int A2T2Outcome { get => Int("a2.t2.outcome"); set => SetInt("a2.t2.outcome", value); }
        public static int A2T3Outcome { get => Int("a2.t3.outcome"); set => SetInt("a2.t3.outcome", value); }

        public static bool A3T1Done { get => Flag("a3.t1.done"); set => Flag("a3.t1.done", value); }
        public static bool A3T2Done { get => Flag("a3.t2.done"); set => Flag("a3.t2.done", value); }
        public static bool A3T3Done { get => Flag("a3.t3.done"); set => Flag("a3.t3.done", value); }
        public static bool WeddingDone { get => Flag("a3.wedding.done"); set => Flag("a3.wedding.done", value); }
        public static bool Act3Done { get => Flag("a3.done"); set => Flag("a3.done", value); }
        public static bool A3T1Traded { get => Flag("a3.t1.traded"); set => Flag("a3.t1.traded", value); }
        public static bool A3T2Traded { get => Flag("a3.t2.traded"); set => Flag("a3.t2.traded", value); }
        public static bool A3T3Traded { get => Flag("a3.t3.traded"); set => Flag("a3.t3.traded", value); }
        public static int A3T1Outcome { get => Int("a3.t1.outcome"); set => SetInt("a3.t1.outcome", value); }
        public static int A3T2Outcome { get => Int("a3.t2.outcome"); set => SetInt("a3.t2.outcome", value); }
        public static int A3T3Outcome { get => Int("a3.t3.outcome"); set => SetInt("a3.t3.outcome", value); }
        public static bool A3SmallWedding { get => Flag("a3.small_wedding"); set => Flag("a3.small_wedding", value); }
        public static bool A3EndingShown { get => Flag("a3.ending_shown"); set => Flag("a3.ending_shown", value); }

        public static int Today => StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;

        public static void Start(int day)
        {
            FeatureVersion = CurrentVersion;
            PendingVisit = 0;
            NextVisitDay = day;
        }

        public static void Complete(int visit)
        {
            int day = Today;
            PendingVisit = 0;
            LastImportantDay = day;
            if (visit == 1) T1Done = true;
            else if (visit == 2) T2Done = true;
            else if (visit == 3)
            {
                ClosingDone = true;
                Act1Done = true;
            }
            else if (visit == 4) A2T1Done = true;
            else if (visit == 5) A2T2Done = true;
            else if (visit == 6)
            {
                A2T3Done = true;
                Act2Done = true;
            }
            else if (visit == 7) A3T1Done = true;
            else if (visit == 8) A3T2Done = true;
            else if (visit == 9) A3T3Done = true;
            else if (visit == 10)
            {
                WeddingDone = true;
                Act3Done = true;
            }
            NextVisitDay = Act3Done ? -1 : day + 1;
        }

        public static void Reset()
        {
            var keys = new System.Collections.Generic.List<string>(ModHelper.GetModDataKeys(ModConstants.ModId));
            foreach (string key in keys)
                if (key.StartsWith(Prefix, StringComparison.Ordinal))
                    ModHelper.SetModData(ModConstants.ModId, key, "");
            FeatureVersion = 0;
            SetInt("last_important", -1);
            SetInt("next_visit_day", -1);
            SetInt("pending_visit", 0);
            Flag("a1.t1.done", false);
            Flag("a1.t2.done", false);
            Flag("a1.closing.done", false);
            Flag("a1.done", false);
            Flag("a1.t1.traded", false);
            Flag("a1.t2.traded", false);
            SetInt("a1.t1.outcome", 0);
            SetInt("a1.t2.outcome", 0);
            Flag("a2.t1.done", false);
            Flag("a2.t2.done", false);
            Flag("a2.t3.done", false);
            Flag("a2.done", false);
            Flag("a2.t1.traded", false);
            Flag("a2.t2.traded", false);
            Flag("a2.t3.traded", false);
            SetInt("a2.t1.outcome", 0);
            SetInt("a2.t2.outcome", 0);
            SetInt("a2.t3.outcome", 0);
            Flag("a3.t1.done", false);
            Flag("a3.t2.done", false);
            Flag("a3.t3.done", false);
            Flag("a3.wedding.done", false);
            Flag("a3.done", false);
            Flag("a3.t1.traded", false);
            Flag("a3.t2.traded", false);
            Flag("a3.t3.traded", false);
            SetInt("a3.t1.outcome", 0);
            SetInt("a3.t2.outcome", 0);
            SetInt("a3.t3.outcome", 0);
            Flag("a3.small_wedding", false);
            Flag("a3.ending_shown", false);
        }
    }
}
