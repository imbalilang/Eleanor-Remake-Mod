using System;

namespace EleanorRomance
{
    /// <summary>
    /// The two fully produced route endings get a standalone full-screen page at
    /// the end of their final event.  The native GameOver page still shows the
    /// regular failure reasons; it only reuses these pages for older saves that
    /// completed the route before the standalone page existed.
    /// </summary>
    public static class EndingPages
    {
        public sealed class Page
        {
            public readonly string TitleKey;
            public readonly string CgKey;
            public readonly string Text;
            public Page(string titleKey, string cgKey, params string[] textKeys)
            {
                TitleKey = titleKey;
                CgKey = cgKey;
                Text = string.Join("\n\n", Array.ConvertAll(textKeys, key => L.Get(key)));
            }
        }

        private static Page HaremPage() => DualState.Flag("d.route") && RomanceState.WorkState <= 0
            ? new Page("er_ending_harem_title", "er_ending_harem_cg", "er_ending_harem_02", "er_ending_harem_03")
            : new Page("er_ending_harem_title", "er_ending_harem_cg",
                "er_ending_harem_01", "er_ending_harem_02", "er_ending_harem_03");

        private static Page MaidPage() => new Page("er_ending_maid_title", "er_ending_maid_cg",
            "er_ending_maid_01", "er_ending_maid_02", "er_ending_maid_03", "er_ending_maid_04");

        private static Page RevolutionPage() => RomanceState.RevVersion >= 2
            ? new Page("er_ending_rev_v2_title", "er_ending_revolution_cg",
                "er_ending_rev_v2_01", "er_ending_rev_v2_02", "er_ending_rev_v2_03", "er_ending_rev_v2_04")
            : new Page("er_ending_rev_title", "er_ending_revolution_cg",
                "er_ending_rev_01", "er_ending_rev_02", "er_ending_rev_03", "er_ending_rev_04");

        private static Page LovePage() => new Page("er_ending_love_title", "er_ending_love_wedding",
            "er_ending_love_01",
            RomanceState.Route == 1 ? "er_ending_love_02_research_returned" : "er_ending_love_02_research_public",
            "er_ending_love_03");

        /// <summary>
        /// Shared harem completion: old dual saves finish at dr20, the D rewrite
        /// finishes at d09; both are E4 and must not be double-granted.
        /// </summary>
        private static bool DualCompleted()
        {
            if (!DualState.Writable || !DualState.HasHistory || !DualState.Confirmed) return false;
            return DualState.Done("dr20") || DualState.Flag("d.act4");
        }

        /// <summary>Route-completion page for dr20 / D d09; independent of the run's GameOver timing.</summary>
        public static Page TryDualCompletion()
        {
            if (!RomanceState.StoryActive) return null;
            if (!DualCompleted()) return null;
            return HaremPage();
        }

        /// <summary>Route-completion page for m28; independent of the run's GameOver timing.</summary>
        public static Page TryMaidCompletion()
        {
            if (!RomanceState.StoryActive) return null;
            if (!(MaidState.Active && MaidState.Lovers && MaidState.Contract == 3 && MaidState.Progress >= 28))
                return null;
            return MaidPage();
        }

        /// <summary>Route-completion page for R14; never granted by the old short route C.</summary>
        public static Page TryRevolutionCompletion()
        {
            if (!RomanceState.StoryActive) return null;
            if (RomanceState.Route != 3 || RomanceState.MotherState != 2) return null;
            if (!RomanceState.RevComplete || RomanceState.RevEndingShown) return null;
            return RevolutionPage();
        }

        /// <summary>Route-completion page for the solo romance wedding (E1).</summary>
        public static Page TryLoveCompletion()
        {
            if (!RomanceState.StoryActive) return null;
            if (!LoveState.Writable || !LoveState.Started || !LoveState.Act3Done) return null;
            if (RomanceState.Route != 1 && RomanceState.Route != 2) return null;
            if (RomanceState.MotherState != 1) return null;
            if (LoveState.A3EndingShown) return null;
            return LovePage();
        }

        /// <summary>
        /// True once any of the five written route endings has been reached in
        /// this run.  Used by the archivist's post-route reset offer; this is
        /// deliberately independent of whether the ending page asset loaded.
        /// </summary>
        public static bool HasExperiencedRouteEnding()
        {
            if (!RomanceState.StoryActive) return false;

            if (LoveState.Writable && LoveState.A3EndingShown) return true;
            if (MaidState.Flag("ending_card_shown") || DualState.Flag("ending_card_shown")) return true;
            if (RomanceState.RevEndingShown) return true;

            if (LoveState.Writable && LoveState.Started && LoveState.Act3Done
                && (RomanceState.Route == 1 || RomanceState.Route == 2)
                && RomanceState.MotherState == 1) return true;
            // Route finales count even when the relationship outcome is not the
            // lovers variant; the archivist is offering another route, not
            // re-labelling the ending.
            if (MaidState.HasHistory && MaidState.Progress >= 28) return true;
            if (DualState.HasHistory
                && (DualState.Done("dr20") || DualState.Flag("d.act4"))) return true;
            if (RomanceState.Route == 3 && RomanceState.MotherState == 2 && RomanceState.RevComplete)
                return true;

            // Pre-LoveState E1 saves: the core wedding route is complete but the
            // later solo-romance visit state was never initialized.
            if (LoveState.FeatureVersion == 0 && RomanceState.Chapter >= RomanceState.ChapterComplete
                && RomanceState.RomanceConfirmed && RomanceState.MotherState == 1
                && (RomanceState.Route == 1 || RomanceState.Route == 2)
                && !MaidState.HasHistory && !DualState.HasHistory) return true;

            // E2 friends: the common line reached its ending without a partner
            // route taking over.
            if (RomanceState.Chapter >= RomanceState.ChapterComplete
                && RomanceState.Romance == 3
                && !MaidState.HasHistory && !DualState.HasHistory
                && RomanceState.Route != 3) return true;

            return false;
        }

        /// <summary>
        /// Native GameOver fallback.  Once a route has shown its standalone page,
        /// do not show the same CG again behind the failure panel.  Old saves that
        /// completed the route before the standalone page can still see it here.
        /// </summary>
        public static Page TryGet(string description)
        {
            if (!RomanceState.StoryActive) return null;
            if (Terminal(description)) return null;
            if (RomanceState.Route == 3)
            {
                // Fallback only if the standalone R14 page did not manage to show
                // and the reported result is not a death/arrest/departure.
                return RomanceState.MotherState == 2
                    ? TryRevolutionCompletion()
                    : null;
            }
            if ((RomanceState.Route != 1 && RomanceState.Route != 2) || RomanceState.MotherState != 1) return null;

            if (DualCompleted())
                return DualState.Flag("ending_card_shown") ? null : HaremPage();

            if (MaidState.Active && MaidState.Lovers && MaidState.Contract == 3 && MaidState.Progress >= 28)
                return MaidState.Flag("ending_card_shown") ? null : MaidPage();

            return TryLoveCompletion();
        }

        private static bool Terminal(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return Contains(text, "死亡", "死了", "dead", "died", "killed")
                || Contains(text, "被捕", "监禁", "失去自由", "prison", "arrest", "captured", "detained")
                || Contains(text, "离开", "远行", "流放", "leave", "depart", "exile");
        }

        private static bool Contains(string text, params string[] words)
        {
            foreach (string word in words)
                if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
