using System;
using System.Collections.Generic;

namespace EleanorRomance
{
    /// <summary>
    /// Post-ending rotating daily window visits for Love, Maid, and Revolution routes
    /// (matching DualDirector's every-4-days post-ending companionship loop).
    /// </summary>
    public static class PostEndingDaily
    {
        private const string Prefix = "er.postdaily.";
        private const int IntervalDays = 4;

        private static int GetInt(string key, int def = -1) => ModHelper.GetModDataInt(ModConstants.ModId, Prefix + key, def);
        private static void SetInt(string key, int val) => ModHelper.SetModDataInt(ModConstants.ModId, Prefix + key, val);

        public static void Reset()
        {
            foreach (string route in new[] { "love", "maid", "rev" })
            {
                SetInt("idx." + route, 0);
                SetInt("last." + route, -1);
            }
        }

        private static string ActiveCompletedRoute(out int baselineDay, out string sprite, out bool useLovePortrait)
        {
            baselineDay = -1;
            sprite = ModConstants.EleanorPlainCasualPortrait;
            useLovePortrait = true;

            if (DualState.Active) return null;

            if (MaidState.Active && MaidV2State.Ended)
            {
                baselineDay = MaidState.LastImportantDay;
                sprite = "er_portrait_eleanor_maid_female_beauty";
                useLovePortrait = true;
                return "maid";
            }

            if (LoveState.Started && LoveState.Act3Done && LoveState.A3EndingShown && !MaidState.Active)
            {
                baselineDay = LoveState.LastImportantDay;
                sprite = ModConstants.EleanorPlainCasualPortrait;
                useLovePortrait = true;
                return "love";
            }

            if (RevolutionV2State.Enabled && RomanceState.RevComplete
                && (RomanceState.RevEndingShown || ChapterFinales.KnockFinished || !LoveA2Closing.Enabled))
            {
                int reunionDay = ModHelper.GetModDataInt(ModConstants.ModId, "revv2.cg.reunion_day", -1);
                baselineDay = reunionDay >= 0 ? reunionDay + 4 : -1;
                sprite = "eleanor_rev";
                useLovePortrait = false;
                return "rev";
            }

            return null;
        }

        public static bool TryQueue(StoreClientManager manager, int day)
        {
            if (manager == null || !RomanceState.StoryActive) return false;

            string route = ActiveCompletedRoute(out int baselineDay, out string sprite, out bool useLovePortrait);
            if (string.IsNullOrEmpty(route)) return false;

            string[] ids = PostEndingDailyContent.ForRoute(route);
            if (ids == null || ids.Length == 0) return false;

            int lastDay = GetInt("last." + route, -1);
            int anchor = lastDay >= 0 ? lastDay : baselineDay;
            if (anchor >= 0 && day < anchor + IntervalDays) return false;

            int idx = GetInt("idx." + route, 0);
            if (idx < 0) idx = 0;
            int slot = idx % ids.Length;
            string sceneId = ids[slot];

            Dialogue root = BuildDialogue(sceneId);
            if (root == null) return false;

            StoreClient client = EleanorStory.NewEleanorClient(
                "post_daily_" + sceneId,
                sprite,
                StoreClient.ClientIntent.DIALOGUE,
                useLovePortrait);
            if (client == null) return false;

            client.mainDialogue = root;
            client.dismissable = true;
            StoryEngine.OnDialogueFinished(root, () =>
            {
                SetInt("idx." + route, (slot + 1) % ids.Length);
                SetInt("last." + route, DualState.Today);
                StoryEngine.Dismiss();
            });
            client.CompleteClientCreation();
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            StoryDirector.Log("Queued post-ending daily " + sceneId + " (" + route + ") on day " + day + ".");
            return true;
        }

        private static Dialogue BuildDialogue(string sceneId)
        {
            PostEndingDailyContent.Line[] lines = PostEndingDailyContent.Get(sceneId);
            if (lines == null || lines.Length == 0) return null;

            Dialogue head = null, tail = null;
            foreach (PostEndingDailyContent.Line line in lines)
            {
                string speaker = SpeakerName(line.Speaker);
                string text = L.Get(line.Key);
                foreach (string paragraph in StoryEngine.SplitParagraphs(text))
                {
                    Dialogue node = StoryEngine.Text(speaker, paragraph);
                    PortraitExpression.Apply(node, line.Key, speaker);
                    if (head == null) head = node;
                    else tail.SetNextDialogue(node);
                    tail = node;
                }
            }
            return head;
        }

        private static string SpeakerName(string speaker)
        {
            switch (speaker)
            {
                case "Player": return L.Player;
                case "Eleanor": return L.Eleanor;
                case "Narrator": return L.Get("er_name_narrator");
                default: return L.Get("er_name_" + (speaker ?? "narrator").ToLowerInvariant());
            }
        }
    }
}
