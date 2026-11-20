using System;
using System.Collections.Generic;

namespace EleanorRomance
{
    /// <summary>Runtime for the maid v2 rewrite.  Old FeatureVersion==1 saves stay on MaidStory.</summary>
    public static class MaidV2Story
    {
        private sealed class ResultTerminal
        {
            public Dialogue Node;
            public string Result;
        }

        private static readonly Dictionary<int, string> SceneByCode = new Dictionary<int, string>
        {
            {8,"v2_a0c1"}, {12,"v2_a0c2"},
            {13,"v2_a1c1"}, {14,"v2_a1c2"}, {15,"v2_a1c3"}, {16,"v2_a1c4"}, {17,"v2_a1c5"},
            {18,"v2_a2c1"}, {19,"v2_a2c2"},
            {20,"v2_a3c1"}, {21,"v2_a3c2"}, {22,"v2_a3c3"},
            {25,"v2_a4c1"}, {26,"v2_a4c2"}, {27,"v2_a4c3"}, {28,"v2_a4c4"},
        };

        public static bool Enabled => MaidState.V2;

        public static StoreClient CreateEntry()
        {
            if (!Enabled) return null;
            if (MaidV2State.CoWork && MaidState.Contract == 1)
            {
                var resume = MaidV2Text.Build("v2_a0c2_s1");
                MaidV2Text.Finish(resume, () => StoryDirector.AdvanceChapter(12, 13));
                return Ready(ClientFor("v2_no_gap", "eleanor_scav", "DIALOGUE"), resume);
            }
            if (!MaidV2State.GapPaid)
            {
                var root = RomanceState.LocketSold
                    ? MaidText.Build(RomanceState.Savings < 800 ? "m11_b002" : "m11_b001", "m11_b003", "m11_b004")
                    : StoryEngine.Build(StoryV06Content.B_C11_B001);
                if (RomanceState.Savings < 800)
                {
                    RomanceState.Savings = 800;
                    RomanceState.SavingsAutoFilled = true;
                }
                MaidV2Text.Finish(root, () => StoryDirector.AdvanceChapter(12, 13));
                return Ready(ClientFor("v2_no_gap", "eleanor_scav", "DIALOGUE"), root);
            }
            return CreateScene("v2_a0c2", 12);
        }

        public static StoreClient CreateChapter(int code)
        {
            if (!Enabled) return null;
            // Chapter 12 is the entry gate, not the unconditional job offer:
            // an unpaid/declined detective-fee choice must keep the common C11
            // finale instead of forcing v2_a0c2's "you covered the shortfall".
            if (code == 12) return CreateEntry();
            string id;
            if (!SceneByCode.TryGetValue(code, out id)) return null;
            if (code >= 13 && code <= 28 && !MaidState.Active) return null;
            return CreateScene(id, code);
        }

        public static StoreClient CreateEvent(int id)
        {
            if (!Enabled || id < 25 || id > 28) return null;
            return CreateChapter(id);
        }

        public static int EventForDay(int day)
        {
            if (!Enabled || !MaidState.Active || !RomanceState.EpilogueDone) return 0;
            int progress = MaidState.Progress;
            if (progress < 24 || progress >= 28) return 0;
            int next = progress == 24 ? 25 : progress + 1;
            // v2_a3c3 ends with "I'll be at work tomorrow".  The post-story
            // visits follow the previous visit's day; absolute target days can
            // leave a week of silence after the main story and look like a
            // permanent pause.
            int due = MaidState.ProgressDay + (progress == 24 ? 1 : 2);
            if (due <= 0) due = MaidState.ProgressDay + 2;
            return day >= due ? next : 0;
        }

        private static StoreClient CreateScene(string id, int code)
        {
            var scene = MaidV2Content.GetScene(id);
            if (scene == null)
            {
                ModEntry.LogError("MaidV2 scene missing: " + id);
                return null;
            }
            if (scene.Trades != null && scene.Trades.Length > 0)
                return CreateTradeScene(scene, code);

            var client = ClientFor(scene.Id, scene.Portrait, scene.Intent);
            var terminals = new List<ResultTerminal>();
            Dialogue head = BuildFlow(scene, 0, terminals);
            if (head == null) return null;
            MaidV2Text.Finish(head, () => CompleteScene(scene), () =>
            {
                if (scene.Id == "v2_a4c3")
                    EndingPageUI.ShowStoryScene("er_story_cg_gala_title", "er_event_maid_gala_cg", "er_story_cg_gala_text");
            });
            foreach (var terminal in terminals)
            {
                string result = terminal.Result;
                terminal.Node.SetEndAction(() =>
                {
                    ApplyResult(scene, result);
                    StoryEngine.Dismiss();
                });
            }
            client.mainDialogue = head;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateTradeScene(MaidV2Content.Scene scene, int code)
        {
            var client = ClientFor(scene.Id, scene.Portrait, scene.Intent);
            var tracker = new StoryEngine.DealTracker();
            bool seller = scene.Intent == "SELL";
            bool gapVisit = scene.Id == "v2_a0c1";
            if (gapVisit)
            {
                tracker.Any = MaidV2State.TradeSold;
                tracker.Lowball = MaidState.Flag("v2.gap_lowball");
                tracker.Discount = MaidState.Flag("v2.gap_markup");
                tracker.Fair = MaidState.Flag("v2.gap_fair");
            }
            client.onItemSold += (item, paid) =>
            {
                tracker.Record(item, paid, seller);
                if (scene.Id == "v2_a0c1" && paid > 0)
                {
                    RomanceState.Savings = RomanceState.Savings + Math.Max(0, paid);
                    MaidV2State.TradeSold = true;
                    MaidState.Flag("v2.gap_lowball", tracker.Lowball);
                    MaidState.Flag("v2.gap_markup", tracker.Discount);
                    MaidState.Flag("v2.gap_fair", tracker.Fair);
                }
            };
            client.onItemBought += (item, paid) => tracker.Record(item, paid, seller);
            if (seller) client.SetWholesale(scene.Id == "v2_a0c1" ? 30 : 0);
            else
            {
                client.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
                client.SetBudget(220);
                client.SetVariety();
            }

            Dialogue main = BuildFlow(scene, 0, new List<ResultTerminal>());
            if (gapVisit)
            {
                StoryEngine.OnDialogueFinished(main, () =>
                {
                    if (RomanceState.ScavStockSpawned2) return;
                    RomanceState.ScavStockSpawned2 = true;
                    StoreClientList.AddScavGeneralStock();
                });
            }
            client.mainDialogue = main;
            bool finished = false;
            Action finish = () =>
            {
                if (finished) return;
                finished = true;
                EleanorStory.RecordDeal(tracker, noSaleIsVisit: true);
                CompleteScene(scene);
            };
            StoryEngine.InterceptDismiss(client, () =>
            {
                if (!gapVisit) return TradeOutcome(scene, tracker);
                int gap = Math.Max(0, 800 - RomanceState.Savings);
                bool canSettle = MaidV2State.GapPaid || (MaidV2State.GapChoice == 1
                    && MaidV2State.TradeSold && gap > 0 && StoryEngine.HasCash(gap));
                Dialogue wrap = canSettle || (MaidV2State.GapChoice == 1 && !tracker.Any)
                    ? TradeOutcome(scene, tracker) : CommonScavOutcome(tracker);
                if (canSettle && !MaidV2State.GapPaid)
                {
                    wrap.SetStartAction(() =>
                    {
                        if (MaidV2State.GapPaid) return;
                        int actualGap = Math.Max(0, 800 - RomanceState.Savings);
                        if (actualGap > 0 && StoryEngine.Spend(actualGap))
                        {
                            MaidV2State.GapAmount = actualGap;
                            MaidV2State.GapPaid = true;
                            RomanceState.Savings = 800;
                            return;
                        }
                        // Cash may have changed after a native wrap was prepared.
                        // Keep the price reaction, then use the common response.
                        Dialogue fallback = CommonScavOutcome(tracker);
                        wrap.SetNextDialogue(fallback);
                        StoryEngine.OnDialogueFinished(fallback, () =>
                        {
                            finish();
                            StoryEngine.Dismiss();
                        });
                    });
                }
                return wrap;
            }, finish);
            client.CompleteClientCreation();
            return client;
        }

        private static Dialogue CommonScavOutcome(StoryEngine.DealTracker tracker) => StoryEngine.Build(
            !tracker.Any ? StoryV06Content.B_C07_B005
            : tracker.Lowball ? StoryV06Content.B_C07_B007
            : tracker.Discount ? StoryV06Content.B_C07_B004 : StoryV06Content.B_C07_B006);

        private static Dialogue TradeOutcome(MaidV2Content.Scene scene, StoryEngine.DealTracker tracker)
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
            {
                foreach (var pair in trade.Outcomes) { blockId = pair.Value; break; }
            }
            return MaidV2Text.Build(blockId);
        }

        private static Dialogue BuildRange(MaidV2Content.Scene scene, int start, int end)
        {
            Dialogue head = null, tail = null;
            for (int i = start; i < end; i++)
            {
                var seg = scene.Segments[i];
                if (seg.Type != "lines") continue;
                Dialogue part = MaidV2Text.Build(seg.BlockId);
                if (head == null) head = part; else tail.SetNextDialogue(part);
                tail = StoryEngine.Tail(part);
            }
            return head;
        }

        private static Dialogue BuildFlow(MaidV2Content.Scene scene, int index, List<ResultTerminal> terminals)
        {
            if (index >= scene.Segments.Length) return null;
            var seg = scene.Segments[index];
            if (seg.Type == "lines")
            {
                string blockId = seg.BlockId;
                if (RomanceState.Route == 1)
                {
                    if (blockId == "v2_a1c5_s0") blockId = "v2_a1c5_exchange";
                    if (blockId == "v2_a4c2_s0") blockId = "v2_a4c2_exchange";
                }
                Dialogue head = MaidV2Text.Build(blockId);
                Dialogue next = BuildFlow(scene, index + 1, terminals);
                if (next != null) StoryEngine.Tail(head).SetNextDialogue(next);
                return head;
            }
            if (seg.Type == "result")
            {
                Dialogue node = StoryEngine.Text(L.Get("er_name_narrator"), seg.Result);
                terminals.Add(new ResultTerminal { Node = node, Result = seg.Result });
                return node;
            }
            if (seg.Type != "choice") return BuildFlow(scene, index + 1, terminals);

            if (scene.Id == "v2_a0c1" && seg.Choice.Id == "C1" && MaidV2State.GapChoice != 0)
                return MaidV2State.GapChoice == 1 ? BuildFlow(scene, index + 1, terminals)
                    : MaidV2Text.Build("v2_a0c1_c1_o2");

            Dialogue continuation = BuildFlow(scene, index + 1, terminals);
            Dialogue prompt = StoryEngine.Text(L.Player, L.Get(seg.Choice.PromptKey));
            for (int i = 0; i < seg.Choice.Options.Length; i++)
            {
                var option = seg.Choice.Options[i];
                Dialogue next;
                if (option.Mainline) next = continuation;
                else if (!string.IsNullOrEmpty(option.BlockId))
                {
                    next = MaidV2Text.Build(option.BlockId);
                    if (!string.IsNullOrEmpty(option.Result))
                        terminals.Add(new ResultTerminal { Node = StoryEngine.Tail(next), Result = option.Result });
                    else if (continuation != null)
                        StoryEngine.Tail(next).SetNextDialogue(continuation);
                }
                else next = continuation;

                Dialogue reply = StoryEngine.PlayerReply(L.Get(option.LabelKey), next);
                Action action = ChoiceAction(scene, seg.Choice, i + 1);
                Func<bool> enabled = ChoiceEnabled(scene, seg.Choice, i + 1);
                string description = scene.Id == "v2_a0c1" && seg.Choice.Id == "C1" && i + 1 == 1
                    ? L.Get("er_maid_v2_gap_desc") : "";
                prompt.AddChoice(DirectoryMaster.Item("yes"), L.Get(option.LabelKey), description, reply, action, enabled);
            }
            return prompt;
        }

        private static Action ChoiceAction(MaidV2Content.Scene scene, MaidV2Content.Choice choice, int option)
        {
            if (scene.Id == "v2_a0c1" && choice.Id == "C1")
            {
                return () => MaidV2State.GapChoice = option;
            }
            if (scene.Id == "v2_a0c2" && choice.Id == "C2" && option == 1)
                return () => MaidV2State.Enter();
            if (scene.Id == "v2_a1c3" && choice.Id == "C1")
            {
                if (option == 1) return () => { RomanceState.Route = 1; RomanceState.MotherState = 1; };
                if (option == 2) return () => { RomanceState.Route = 2; };
                if (option == 3) return () => { RomanceState.Route = 3; RomanceState.MotherState = 2; };
            }
            if (scene.Id == "v2_a2c2" && choice.Id == "C2")
                return () => { RomanceState.Romance = 1; MaidState.Confession = 1; };
            return null;
        }

        private static Func<bool> ChoiceEnabled(MaidV2Content.Scene scene, MaidV2Content.Choice choice, int option)
        {
            if (scene.Id == "v2_a0c1" && choice.Id == "C1" && option == 1)
                return () =>
                {
                    int gap = Math.Max(0, 800 - RomanceState.Savings);
                    return gap > 0 && StoryEngine.HasCash(gap);
                };
            return null;
        }

        private static void ApplyResult(MaidV2Content.Scene scene, string result)
        {
            if (scene.Id == "v2_a1c3")
            {
                if (RomanceState.Route == 1)
                {
                    RomanceState.MotherState = 1;
                    RomanceState.DriveState = 3;
                    RomanceState.ProtectionReady = true;
                    StoryDirector.AdvanceChapter(15, 17);
                    return;
                }
                if (RomanceState.Route == 3)
                {
                    RomanceState.MotherState = 2;
                    RomanceState.DriveState = 5;
                    RomanceState.RevVersion = 2;
                    StoryEngine.FreeNutrifruitForLeak();
                    MaidState.End("public-leak", 0);
                    StoryDirector.AdvanceChapter(15, RevolutionV2Story.ChR01);
                    return;
                }
            }
            // Act 0 result branches (no gap / decline) still complete the
            // current chapter; only the route-result branches above diverge.
            CompleteScene(scene);
        }

        private static void CompleteScene(MaidV2Content.Scene scene)
        {
            int day = MaidState.Today;
            MaidState.LastImportantDay = day;
            MaidState.Progress = scene.Code;
            MaidState.ProgressDay = day;
            if (MaidState.PendingVisit == scene.Code)
            {
                MaidState.PendingVisit = 0;
                MaidState.NextVisitDay = -1;
            }

            switch (scene.Id)
            {
                case "v2_a0c1":
                    StoryDirector.AdvanceChapter(8, 9);
                    break;
                case "v2_a0c2":
                    StoryDirector.AdvanceChapter(12, 13);
                    break;
                case "v2_a1c1":
                    RomanceState.DriveState = 2;
                    StoryDirector.AdvanceChapter(13, 14);
                    break;
                case "v2_a1c2":
                    StoryDirector.AdvanceChapter(14, 15);
                    break;
                case "v2_a1c3":
                    RomanceState.ThreatRecorded = true;
                    StoryDirector.AdvanceChapter(15, 16);
                    break;
                case "v2_a1c4":
                    RomanceState.MotherState = 1;
                    RomanceState.DriveState = 4;
                    RomanceState.ProtectionReady = true;
                    StoryEngine.TryFreeNutrifruit();
                    StoryDirector.AdvanceChapter(16, 17);
                    break;
                case "v2_a1c5":
                    RomanceState.MotherState = 1;
                    RomanceState.MotherVisited = true;
                    StoryDirector.AdvanceChapter(17, 18);
                    break;
                case "v2_a2c1":
                    MaidState.AddressAccepted = true;
                    MaidState.DebutShown = true;
                    StoryDirector.AdvanceChapter(18, 19);
                    break;
                case "v2_a2c2":
                    MaidV2State.Act2Done = true;
                    StoryDirector.AdvanceChapter(19, 20);
                    break;
                case "v2_a3c1":
                    MaidProjects.ShowResult(2);
                    StoryDirector.AdvanceChapter(20, 21);
                    break;
                case "v2_a3c2":
                    StoryDirector.AdvanceChapter(21, 22);
                    break;
                case "v2_a3c3":
                    MaidState.BasicComplete = true;
                    MaidV2State.Act3Done = true;
                    MaidState.Progress = 24;
                    MaidState.ProgressDay = day;
                    StoryDirector.CompleteCore();
                    break;
                case "v2_a4c1":
                    MaidV2State.Residency = true;
                    MaidState.Progress = 25;
                    break;
                case "v2_a4c2":
                    MaidState.Progress = 26;
                    break;
                case "v2_a4c3":
                    MaidV2State.GalaInvited = true;
                    MaidState.Progress = 27;
                    break;
                case "v2_a4c4":
                    MaidV2State.Ended = true;
                    MaidState.Progress = 28;
                    MaidState.Contract = 3;
                    EndingPageUI.ShowMaidCompletion();
                    break;
            }
            CompleteOptionalProjects();
            CollectDividends();
            // Mainline v2 scenes used to update the Wilde rows silently, so the
            // project that unblocks the next chapter was easy to miss.  The tip
            // itself is sent by the next daily refresh, when no dialogue is up.
            MaidProjects.RefreshStates();
        }

        /// <summary>
        /// P3/P4 stay optional for the story (P1 is now required before P2).
        /// Their old scene-specific result visits are retired in v2, so
        /// acknowledge each purchased project once with the generic Wilde
        /// feedback and let the existing dividend system start.
        /// </summary>
        private static void CompleteOptionalProjects()
        {
            if (!MaidState.Active) return;
            for (int p = 1; p <= 4; p++)
            {
                if (p == 2 || !MaidProjects.ResultPending(p)) continue;
                MaidProjects.ShowResult(p);
                StoryDirector.Notify(L.Get("er_maid_v2_optional_project_done",
                    L.Get("er_maid_project_" + p)));
            }
        }

        private static void CollectDividends()
        {
            if (!MaidState.Active) return;
            MaidDividends.Accrue();
            MaidDividends.Collect();
        }

        private static StoreClient ClientFor(string id, string portrait, string intent)
        {
            StoreClient client;
            if (portrait == "maleBM5")
                client = EleanorStory.NewNpcClient("v2_" + id, L.Get("er_name_thug"),
                    "maleBM5", EleanorStory.FactionKind.BlackMarket);
            else if (portrait == "er_portrait_mother_female_beauty")
                client = EleanorStory.NewNpcClient("v2_" + id, L.Get("er_name_sophia"),
                    portrait, EleanorStory.FactionKind.Upper);
            else
                client = EleanorStory.NewEleanorClient("v2_" + id, portrait,
                    intent == "SELL" ? StoreClient.ClientIntent.SELL
                    : intent == "BUY" ? StoreClient.ClientIntent.BUY
                    : StoreClient.ClientIntent.DIALOGUE);

            client.clientIntent = intent == "SELL" ? StoreClient.ClientIntent.SELL
                : intent == "BUY" ? StoreClient.ClientIntent.BUY
                : StoreClient.ClientIntent.DIALOGUE;
            return client;
        }

        private static StoreClient Ready(StoreClient client, Dialogue root)
        {
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }
    }
}
