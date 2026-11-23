using System;
using System.Collections.Generic;

namespace EleanorRomance
{
    /// <summary>Runtime for the revolution route.</summary>
    public static class RevolutionV2Story
    {
        public const int ChR01 = 280, ChR02 = 281, ChR03 = 282, ChR04 = 283, ChR05 = 284,
            ChR06 = 285, ChR07 = 286, ChR08 = 287, ChR09 = 288, ChR10 = 289, ChR11 = 290;

        private sealed class ResultTerminal
        {
            public Dialogue Node;
            public string Result;
        }

        private static readonly Dictionary<int, string> SceneByCode = new Dictionary<int, string>
        {
            {280,"v2_r1c1"}, {281,"v2_r1c2"},
            {282,"v2_r2c1"}, {283,"v2_r2c2"}, {284,"v2_r2c3"},
            {285,"v2_r3c1"}, {286,"v2_r3c2"},
            {287,"v2_r4c1"}, {288,"v2_r4c2"}, {289,"v2_r4c3"}, {290,"v2_r4c4"},
        };

        private static readonly Dictionary<int, int> NextCode = new Dictionary<int, int>
        {
            {280,281}, {281,282}, {282,283}, {283,284}, {284,285},
            {285,286}, {286,287}, {287,288}, {288,289}, {289,290},
        };

        public static bool IsChapter(int chapter) => chapter >= ChR01 && chapter <= ChR11;
        public static bool UseMorningQueue(int chapter) => chapter == ChR06 || chapter == ChR11;

        public static StoreClient CreateChapter(int chapter)
        {
            if (!RevolutionV2State.Enabled || !IsChapter(chapter)) return null;
            string id;
            if (!SceneByCode.TryGetValue(chapter, out id)) return null;
            return CreateScene(id, chapter);
        }

        private static StoreClient CreateScene(string id, int code)
        {
            var scene = RevolutionV2Content.GetScene(id);
            if (scene == null)
            {
                ModEntry.LogError("RevolutionV2 scene missing: " + id);
                return null;
            }
            if (scene.Trades != null && scene.Trades.Length > 0)
                return CreateTradeScene(scene, code);

            var client = ClientFor(scene);
            Dialogue head = BuildFlow(scene, 0, new List<ResultTerminal>());
            if (head == null) return null;
            RevolutionV2Text.Finish(head, () => CompleteScene(scene), code == ChR06 ? ShowFirstKillScene : (Action)null);
            client.mainDialogue = head;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateTradeScene(RevolutionV2Content.Scene scene, int code)
        {
            var client = ClientFor(scene);
            var tracker = new StoryEngine.DealTracker();
            bool seller = scene.Intent == "SELL";
            client.onItemSold += (item, paid) => tracker.Record(item, paid, seller);
            client.onItemBought += (item, paid) => tracker.Record(item, paid, seller);
            if (seller)
                client.SetWholesale(scene.Code == 280 ? 25 : 20);
            else
            {
                var trade = scene.Trades[0];
                foreach (string tag in Tags(trade.Type)) client.clientBuyingTagList.Add(tag);
                int budget;
                if (int.TryParse(trade.Budget, out budget) && budget > 0) client.SetBudget(budget);
                client.SetVariety();
            }

            Dialogue main = BuildFlow(scene, 0, new List<ResultTerminal>());
            if (seller) StoryEngine.OnDialogueFinished(main, () => SpawnStock(scene));
            client.mainDialogue = main;
            StoryEngine.InterceptDismiss(client,
                () => TradeOutcome(scene, tracker),
                () => CompleteScene(scene));
            client.CompleteClientCreation();
            return client;
        }

        private static void SpawnStock(RevolutionV2Content.Scene scene)
        {
            string[] ids;
            switch (scene.Code)
            {
                case 280: ids = new[] { "screwdriver", "wire_cutter" }; break;
                case 287: ids = new[] { "scrap_metal", "scrap_metal", "metal_ingot" }; break;
                case 288: ids = new[] { "revolver", "revolver", "small_pistol_ammo_nl" }; break;
                case 289: ids = new[] { "combat_knife", "pipe_weapon" }; break;
                default: return;
            }
            for (int i = 0; i < ids.Length; i++)
            {
                string key = "revv2.stock." + scene.Code + "." + i;
                if (ModHelper.GetModDataBool(ModConstants.ModId, key, false)) continue;
                try
                {
                    GameItem item = ItemSpawner.Spawn(ids[i]);
                    if (item == null) throw new InvalidOperationException("Missing item " + ids[i]);
                    ItemFeatureHelper.SetSecondHand(item);
                    PlayerStore.Instance.AddDirectSellingItemToTable(item, isOwend: false,
                        isStolen: scene.Code != 287);
                    ModHelper.SetModDataBool(ModConstants.ModId, key, true);
                }
                catch (Exception ex)
                {
                    ModEntry.LogError("Revolution v2 stock " + ids[i] + ": " + ex);
                }
            }
        }

        private static int FirstChoice(RevolutionV2Content.Scene scene)
        {
            for (int i = 0; i < scene.Segments.Length; i++)
                if (scene.Segments[i].Type == "choice") return i;
            return -1;
        }

        private static IEnumerable<string> Tags(string type)
        {
            if (type == "MEDICAL") yield return "MEDICAL";
            else if (type == "WEAPON") yield return "WEAPON";
            else if (type == "MELEE_WEAPON") yield return "MELEE_WEAPON";
            else if (type == "TOOL") yield return "TOOL";
            else if (type == "MATERIAL") yield return "MATERIAL";
            else if (type == "FOOD") yield return "FOOD";
        }

        private static Dialogue TradeOutcome(RevolutionV2Content.Scene scene, StoryEngine.DealTracker tracker)
        {
            string outcome = "公道";
            if (!tracker.Any) outcome = "未成交";
            else if (scene.Intent == "SELL")
            {
                if (tracker.Lowball) outcome = "压价";
                else if (tracker.Discount) outcome = "溢价";
            }
            else
            {
                if (tracker.Overcharge) outcome = "高价";
                else if (tracker.Discount) outcome = "让利";
            }
            string blockId = null;
            var trade = scene.Trades[0];
            if (!trade.Outcomes.TryGetValue(outcome, out blockId))
                foreach (var pair in trade.Outcomes) { blockId = pair.Value; break; }
            return RevolutionV2Text.Build(blockId);
        }

        private static Dialogue BuildRange(RevolutionV2Content.Scene scene, int start, int end)
        {
            Dialogue head = null, tail = null;
            for (int i = start; i < end; i++)
            {
                var seg = scene.Segments[i];
                if (seg.Type != "lines") continue;
                var part = RevolutionV2Text.Build(seg.BlockId);
                if (head == null) head = part; else tail.SetNextDialogue(part);
                tail = StoryEngine.Tail(part);
            }
            return head;
        }

        private static Dialogue BuildFlow(RevolutionV2Content.Scene scene, int index, List<ResultTerminal> terminals)
        {
            if (index >= scene.Segments.Length) return null;
            var seg = scene.Segments[index];
            if (seg.Type == "lines")
            {
                var head = RevolutionV2Text.Build(seg.BlockId);
                var next = BuildFlow(scene, index + 1, terminals);
                if (next != null) StoryEngine.Tail(head).SetNextDialogue(next);
                return head;
            }
            if (seg.Type == "result")
            {
                var node = StoryEngine.Text(L.Get("er_name_narrator"), seg.Result);
                terminals.Add(new ResultTerminal { Node = node, Result = seg.Result });
                return node;
            }
            if (seg.Type != "choice") return BuildFlow(scene, index + 1, terminals);

            var continuation = BuildFlow(scene, index + 1, terminals);
            var choiceHead = seg.BlockId == null
                ? StoryEngine.Text(L.Player, L.Get(seg.Choice.PromptKey))
                : RevolutionV2Text.Build(seg.BlockId);
            var prompt = StoryEngine.Tail(choiceHead);
            for (int i = 0; i < seg.Choice.Options.Length; i++)
            {
                var option = seg.Choice.Options[i];
                Dialogue next;
                if (!string.IsNullOrEmpty(option.BlockId))
                {
                    next = RevolutionV2Text.Build(option.BlockId);
                    if (!string.IsNullOrEmpty(option.Result))
                        terminals.Add(new ResultTerminal { Node = StoryEngine.Tail(next), Result = option.Result });
                    else if (continuation != null)
                        StoryEngine.Tail(next).SetNextDialogue(continuation);
                }
                else next = continuation;
                var reply = StoryEngine.PlayerReply(L.Get(option.LabelKey), next);
                prompt.AddChoice(DirectoryMaster.Item("yes"), L.Get(option.LabelKey), "", reply, null, null);
            }
            return choiceHead;
        }

        private static void ShowFirstKillScene()
        {
            EndingPageUI.ShowStoryScene("er_story_cg_revolution_embrace_title",
                "er_event_revolution_embrace_cg", "er_story_cg_revolution_embrace_text");
        }

        private static void CompleteScene(RevolutionV2Content.Scene scene)
        {
            RevolutionV2State.Progress = scene.Code;
            switch (scene.Code)
            {
                case 284: RevolutionV2State.TeamLoss = true; break;
                case 285: RevolutionV2State.FirstKill = true; break;
                case 287:
                case 288:
                case 289: RevolutionV2State.Expedition = true; break;
                case 290:
                    RevolutionV2State.Ended = true;
                    RomanceState.RevComplete = true;
                    RomanceState.RevEndingShown = false;
                    StoryEngine.GiveSelfRefillingPerfume();
                    StoryDirector.AdvanceChapter(290, RomanceState.ChapterComplete);
                    EndingPageUI.ShowRevolutionCompletion();
                    return;
            }
            int next;
            if (NextCode.TryGetValue(scene.Code, out next) && next > 0)
                StoryDirector.AdvanceChapter(scene.Code, next);
        }

        private static string PortraitFor(string key)
        {
            // Content stores the symbolic vest name; resolve it to the bundled
            // beauty asset with the ordinary revolution sprite as fallback.
            if (key == "EleanorVestPortrait")
                return PortraitCompat.ResolveOutfit(ModConstants.EleanorVestPortrait, "eleanor_rev");
            return key;
        }

        private static StoreClient ClientFor(RevolutionV2Content.Scene scene)
        {
            StoreClient client;
            string portrait = PortraitFor(scene.Portrait);
            if (scene.Client == "Eleanor")
                client = EleanorStory.NewEleanorClient("revv2_" + scene.Id, portrait,
                    scene.Intent == "SELL" ? StoreClient.ClientIntent.SELL
                    : scene.Intent == "BUY" ? StoreClient.ClientIntent.BUY
                    : StoreClient.ClientIntent.DIALOGUE, upper: false);
            else
            {
                string display = scene.Client;
                if (display == "革命军军需官") display = L.Get("er_name_rev_quartermaster");
                else if (display == "革命军战士") display = L.Get("er_name_rev_fighter");
                else if (display == "革命军游击队") display = L.Get("er_name_rev_guerrilla");
                client = EleanorStory.NewNpcClient("revv2_" + scene.Id, display,
                    portrait, EleanorStory.FactionKind.Rev);
            }
            client.clientIntent = scene.Intent == "SELL" ? StoreClient.ClientIntent.SELL
                : scene.Intent == "BUY" ? StoreClient.ClientIntent.BUY
                : StoreClient.ClientIntent.DIALOGUE;
            return client;
        }
    }
}
