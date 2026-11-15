using System;

namespace EleanorRomance
{
    /// <summary>Block text builder for the revolution v2 rewrite.</summary>
    public static class RevolutionV2Text
    {
        public static Dialogue Build(string blockId)
        {
            var block = RevolutionV2Content.GetBlock(blockId);
            if (block == null) throw new ArgumentException("Missing revolution v2 block: " + blockId);
            Dialogue head = null, tail = null;
            foreach (var line in block.Lines)
            {
                string speaker = Speaker(line.Speaker);
                foreach (string paragraph in StoryEngine.SplitParagraphs(L.Get(line.Key)))
                {
                    Dialogue node = StoryEngine.Text(speaker, paragraph);
                    PortraitExpression.Apply(node, line.Key, speaker);
                    if (head == null) head = node; else tail.SetNextDialogue(node);
                    tail = node;
                }
            }
            return head;
        }

        public static Dialogue Append(Dialogue head, params string[] blockIds)
        {
            foreach (string id in blockIds)
            {
                Dialogue next = Build(id);
                if (next == null) continue;
                StoryEngine.Tail(head).SetNextDialogue(next);
            }
            return head;
        }

        public static Dialogue Finish(Dialogue head, Action commit, Action afterDismiss = null)
        {
            StoryEngine.OnDialogueFinished(head, () =>
            {
                commit?.Invoke();
                StoryEngine.Dismiss();
                afterDismiss?.Invoke();
            });
            return head;
        }

        public static string Speaker(string speaker)
        {
            switch (speaker)
            {
                case "Player": return L.Player;
                case "Eleanor": return L.Eleanor;
                case "RevQuartermaster": return L.Get("er_name_rev_quartermaster");
                case "RevFighter": return L.Get("er_name_rev_fighter");
                case "RevGuerrilla": return L.Get("er_name_rev_guerrilla");
                                default: return L.Get("er_name_narrator");
            }
        }
    }
}
