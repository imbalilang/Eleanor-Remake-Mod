using System;
using System.Collections.Generic;

namespace EleanorRomance
{
    /// <summary>Builds solo-romance dialogue from the generated LoveContent table.</summary>
    public static class LoveText
    {
        public static Dialogue Build(params string[] ids) => BuildBackup(L.Eleanor, ids);

        public static Dialogue BuildBackup(string companionName, params string[] ids)
        {
            Dialogue head = null, tail = null;
            foreach (string id in ids)
            {
                LoveContent.Line[] lines = LoveContent.Get(id);
                if (lines == null)
                {
                    ModEntry.LogError("Missing love story block: " + id);
                    continue;
                }

                int cursor = LoveState.Int("cursor." + id);
                int position = 0;
                foreach (LoveContent.Line line in lines)
                {
                    string speaker = SpeakerName(line.Speaker);
                    string text = L.Get(line.Key);

                    foreach (string paragraph in StoryEngine.SplitParagraphs(text))
                    {
                        int next = ++position;
                        if (next <= cursor) continue;
                        Dialogue node = StoryEngine.Text(speaker, paragraph);
                        if (line.Speaker == "SEC") StoryPresentation.MarkCompanion(node, companionName);
                        PortraitExpression.Apply(node, line.Key, speaker);
                        node.SetEndAction(() => LoveState.SetInt("cursor." + id, Math.Max(next, LoveState.Int("cursor." + id))));
                        if (head == null) head = node;
                        else tail.SetNextDialogue(node);
                        tail = node;
                    }
                }

                StoryPresentationSpec presentation = LoveContent.GetPresentation(id);
                if (presentation != null && head != null)
                    head.SetStartAction(() => StoryPresentation.Apply(presentation));
            }
            return head ?? StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_love_resume"));
        }

        public static string SpeakerName(string speaker)
        {
            switch (speaker)
            {
                case "Player": return L.Player;
                case "Eleanor": return L.Eleanor;
                case "Sophia": return L.Get("er_name_sophia");
                case "Mother": return L.Get("er_name_mother");
                case "SEC": return "SEC";
                case "Narrator": return L.Get("er_name_narrator");
                default: return L.Get("er_name_" + (speaker ?? "narrator").ToLowerInvariant());
            }
        }

        public static Dialogue Append(Dialogue head, params string[] ids)
        {
            Dialogue next = Build(ids);
            if (head == null) return next;
            if (next != null) StoryEngine.Tail(head).SetNextDialogue(next);
            return head;
        }
    }
}
