using System;

namespace EleanorRomance
{
    /// <summary>
    /// Only the two fully produced perfect routes get a custom ending page.
    /// Every other ending keeps the native GameOver reason/description.
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

        public static Page TryGet(string description)
        {
            if (!RomanceState.StoryActive) return null;
            if (Terminal(description)) return null;
            if (RomanceState.Route != 1 || RomanceState.MotherState != 1) return null;

            if (DualState.Writable && DualState.HasHistory && DualState.Confirmed && DualState.Done("dr20"))
                return new Page("er_ending_harem_title", "er_ending_harem_cg",
                    "er_ending_harem_01", "er_ending_harem_02", "er_ending_harem_03");

            if (MaidState.Active && MaidState.Lovers && MaidState.Contract == 3 && MaidState.Progress >= 28)
                return new Page("er_ending_maid_title", "er_ending_maid_cg",
                    "er_ending_maid_01", "er_ending_maid_02", "er_ending_maid_03", "er_ending_maid_04");

            return null;
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
