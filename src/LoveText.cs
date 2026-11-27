using System;
using System.Collections.Generic;

namespace EleanorRomance
{
    /// <summary>Builds solo-romance dialogue from the generated LoveContent table.</summary>
    public static class LoveText
    {
        public static Dialogue Build(params string[] ids) => BuildBackup(L.Eleanor, ids);

        public static Dialogue BuildBackup(string companionName, params string[] ids) =>
            BuildBackup(companionName, ids, false);

        // A resumed visit still needs the spoken question that hosts its buttons.
        public static Dialogue BuildForChoice(string blockId) =>
            BuildBackup(L.Eleanor, new[] { blockId }, true);

        private static Dialogue BuildBackup(string companionName, string[] ids, bool repeatLastLine)
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

                string cursorKey = LoveContent.CursorKey(id);
                int cursor = LoveState.Int(cursorKey);
                int position = 0;
                foreach (LoveContent.Line line in lines)
                {
                    string speaker = SpeakerName(line.Speaker);
                    string text = L.Get(line.Key);

                    foreach (string paragraph in StoryEngine.SplitParagraphs(text))
                    {
                        int next = ++position;
                        if (next <= cursor && !(repeatLastLine && line.Key == lines[lines.Length - 1].Key)) continue;
                        Dialogue node = StoryEngine.Text(speaker, paragraph);
                        if (line.Speaker == "SEC") StoryPresentation.MarkCompanion(node, companionName);
                        PortraitExpression.Apply(node, line.Key, line.Speaker == "SEC" ? companionName : speaker);
                        node.SetEndAction(() => LoveState.SetInt(cursorKey, Math.Max(next, LoveState.Int(cursorKey))));
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
