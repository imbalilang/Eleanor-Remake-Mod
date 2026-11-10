using System;
using System.Collections.Generic;
using System.Text;

namespace EleanorRomance
{
    /// <summary>Builds dual-route dialogue from the generated DualContent table.</summary>
    public static class DualText
    {
        public static Dialogue Build(params string[] ids)
        {
            Dialogue head = null, tail = null;
            foreach (string id in ids)
            {
                DualContent.Line[] lines = DualContent.Get(id);
                if (lines == null)
                {
                    ModEntry.LogError("Missing dual story block: " + id);
                    continue;
                }
                int cursor=DualState.Int("cursor."+id);
                int position=0;
                foreach (DualContent.Line line in lines)
                {
                    string speaker = SpeakerName(line.Speaker);
                    foreach (string paragraph in L.Get(line.Key).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        int next=++position;
                        if(next<=cursor)continue;
                        Dialogue node = StoryEngine.Text(speaker, paragraph);
                        node.SetEndAction(()=>DualState.SetInt("cursor."+id,Math.Max(next,DualState.Int("cursor."+id))));
                        if (head == null) head = node;
                        else tail.SetNextDialogue(node);
                        tail = node;
                    }
                }
            }
            return head ?? StoryEngine.Text(L.Get("er_name_narrator"),L.Get("er_dual_entry_resume"));
        }

        public static string SpeakerName(string speaker)
        {
            switch (speaker)
            {
                case "Player": return L.Player;
                case "Eleanor": return L.Eleanor;
                case "Lydia": return L.Get("er_name_lydia");
                case "Mother": return L.Get("er_name_mother");
                case "Uncle": return L.Get("er_name_uncle");
                case "Thug": return L.Get("er_name_thug");
                case "Officer": return "SEC";
                case "News": return L.Get("er_name_news");
                case "SecurityNotice": return L.Get("er_name_security_notice");
                case "UpperBuyer": return L.Get("er_name_upper_buyer");
                case "Narrator": return L.Get("er_name_narrator");
                default: return L.Get("er_name_" + (speaker ?? "narrator").ToLowerInvariant());
            }
        }

        public static string Plain(string id)
        {
            var lines = DualContent.Get(id);
            if (lines == null) return "";
            var result = new StringBuilder();
            foreach (var line in lines)
            {
                if (result.Length > 0) result.Append("\n\n");
                result.Append(L.Get(line.Key));
            }
            return result.ToString();
        }

        public static Dialogue Append(Dialogue head, params string[] ids)
        {
            Dialogue next = Build(ids);
            if (head == null) return next;
            StoryEngine.Tail(head).SetNextDialogue(next);
            return head;
        }

        public static Dialogue Finish(Dialogue head, Action commit)
        {
            return FinishAll(head, commit);
        }

        /// <summary>Commits once when any branch reaches a leaf.</summary>
        public static Dialogue FinishAll(Dialogue head, Action commit)
        {
            bool done = false;
            Action finish = () =>
            {
                if (done) return;
                done = true;
                commit?.Invoke();
                StoryEngine.Dismiss();
            };
            var seen = new System.Collections.Generic.HashSet<Dialogue>();
            Action<Dialogue> walk = null;
            walk = node =>
            {
                if (node == null || !seen.Add(node)) return;
                if (node.nextDialogue != null) walk(node.nextDialogue);
                foreach (var choice in node.choices) walk(choice.Item2);
                if (node.nextDialogue == null && node.choices.Count == 0)
                {
                    Action previous = node.endAction;
                    node.SetEndAction(() => { previous?.Invoke(); finish(); });
                }
            };
            walk(head);
            return head;
        }

        public static StoryEngine.Choice Choice(string id, Dialogue next, Action action = null, Func<bool> enabled = null)
        {
            return Option("er_dual_choice_" + id + "_label",
                "er_dual_choice_" + id + "_desc", next, action, enabled);
        }

        public static StoryEngine.Choice Option(string labelKey, string descriptionKey, Dialogue next, Action action = null, Func<bool> enabled = null)
        {
            string label = L.Get(labelKey);
            // Choice labels are echoed as the following Player line; skip the
            // duplicate only when the graph explicitly begins with that line.
            if (next != null && next.title == L.Player && next.plainText == label && next.nextDialogue != null)
                next = next.nextDialogue;
            return new StoryEngine.Choice(label, L.Get(descriptionKey), next, action, enabled);
        }

        public static void Choices(Dialogue head, string promptKey, params StoryEngine.Choice[] choices)
        {
            StoryEngine.AddChoices(head, L.Get(promptKey), choices);
        }
    }
}
