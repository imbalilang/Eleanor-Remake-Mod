using System;

namespace EleanorRomance
{
    /// <summary>
    /// Builds the solo-romance visits. Act1 is T1/T2 plus Eleanor's closing
    /// scene; Act2 is the water-shortage event with three type-based trades.
    /// Trade reactions come from the generated blocks when the player leaves.
    /// </summary>
    public static class LoveStory
    {
        public static StoreClient CreateVisit(int visit)
        {
            switch (visit)
            {
                case 1: return CreateFirstTrade();
                case 2: return CreateFruitTrade();
                case 3: return CreateClosing();
                case 4: return CreateAct2FirstTrade();
                case 5: return CreateAct2SecondTrade();
                case 6: return CreateAct2ThirdTrade();
                case 7: return CreateAct3FirstTrade();
                case 8: return CreateAct3SecondTrade();
                case 9: return CreateAct3ThirdTrade();
                case 10: return CreateAct3Wedding();
                default:
                    ModEntry.LogError("Unknown love visit: " + visit);
                    return null;
            }
        }

        private static StoreClient SophiaClient(string id)
        {
            return EleanorStory.NewNpcClient(id, L.Get("er_name_sophia"),
                EleanorStory.MotherSpriteKey(), EleanorStory.FactionKind.Upper);
        }

        private static StoreClient SophiaHomeClient(string id)
        {
            return EleanorStory.NewNpcClient(id, L.Get("er_name_sophia"),
                PortraitCompat.ResolveMotherHome(), EleanorStory.FactionKind.Upper);
        }

        private static StoryEngine.Choice Choice(string id, Dialogue next)
        {
            return new StoryEngine.Choice(
                L.Get("er_love_choice_" + id + "_label"),
                L.Get("er_love_choice_" + id + "_desc"),
                next, null);
        }

        private static StoryEngine.Choice Choice(string id, Dialogue next, Action action)
        {
            return new StoryEngine.Choice(
                L.Get("er_love_choice_" + id + "_label"),
                L.Get("er_love_choice_" + id + "_desc"),
                next, action);
        }

        private static StoreClient CreateFirstTrade()
        {
            StoreClient client = SophiaClient("love_a1_01");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            client.SetBudget(200);
            client.SetVariety();

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.Build("la1_s1_pre");
            Dialogue postMain = LoveText.Build("la1_s1_post_main");
            Dialogue common = LoveText.Build("la1_s1_common");
            Dialogue alt = LoveText.Build("la1_s1_c1_2");
            StoryEngine.Tail(postMain).SetNextDialogue(common);
            StoryEngine.Tail(alt).SetNextDialogue(common);
            StoryEngine.AddChoices(pre, L.Get("er_love_prompt_c1"),
                Choice("la1_c1_1", postMain),
                Choice("la1_c1_2", alt));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(1, tracker), () => LoveDirector.Complete(1));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateFruitTrade()
        {
            StoreClient client = SophiaClient("love_a1_02");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(120);

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.Build("la1_s2_pre");
            pre.SetStartAction(StoryPresentation.Clear);
            Dialogue postMain = LoveText.Build("la1_s2_post_main");
            Dialogue common = LoveText.Build("la1_s2_common");
            Dialogue research = LoveText.Build(RomanceState.Route == 1
                ? "la1_s2_research_e1" : "la1_s2_research");
            Dialogue handoff = LoveText.Build("la1_s2_handoff");
            Dialogue alt2 = LoveText.Build("la1_s2_c2_2");
            Dialogue alt3 = LoveText.Build("la1_s2_c2_3");
            StoryEngine.Tail(postMain).SetNextDialogue(common);
            StoryEngine.Tail(alt2).SetNextDialogue(common);
            StoryEngine.Tail(alt3).SetNextDialogue(common);
            StoryEngine.Tail(common).SetNextDialogue(research);
            StoryEngine.Tail(research).SetNextDialogue(handoff);
            StoryEngine.AddChoices(pre, L.Get("er_love_prompt_c2"),
                Choice("la1_c2_1", postMain),
                Choice("la1_c2_2", alt2),
                Choice("la1_c2_3", alt3));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(2, tracker), () => LoveDirector.Complete(2));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct2FirstTrade()
        {
            StoreClient client = SophiaClient("love_a2_01");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            client.SetBudget(150);
            client.SetVariety();

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.Build("la2_s1_pre");
            pre.SetStartAction(StoryPresentation.Clear);
            Dialogue postMain = LoveText.Build("la2_s1_post_main");
            Dialogue alt2 = LoveText.Build("la2_s1_c1_2");
            Dialogue alt3 = LoveText.Build("la2_s1_c1_3");
            StoryEngine.AddChoices(pre, L.Get("er_love_prompt_a2_c1"),
                Choice("la2_c1_1", postMain),
                Choice("la2_c1_2", alt2),
                Choice("la2_c1_3", alt3));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () =>
            {
                Dialogue reaction = TradeReaction(4, tracker);
                Dialogue robbery = LoveText.Build("la2_s1_common");
                StoryEngine.Tail(reaction).SetNextDialogue(robbery);
                StoryEngine.OnDialogueFinished(robbery, LoveA2Closing.FinishFromVisit);
                return reaction;
            }, () => LoveDirector.Complete(4));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct2SecondTrade()
        {
            StoreClient client = SophiaHomeClient("love_a2_02");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(120);

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.BuildForChoice("la2_s2_pre");
            pre.SetStartAction(StoryPresentation.Clear);
            Dialogue postMain = LoveText.Build("la2_s2_post_main");
            Dialogue common = LoveText.Build("la2_s2_common");
            Dialogue alt2 = LoveText.Build("la2_s2_c2_2");
            Dialogue alt3 = LoveText.Build("la2_s2_c2_3");
            StoryEngine.Tail(postMain).SetNextDialogue(common);
            StoryEngine.Tail(alt2).SetNextDialogue(common);
            StoryEngine.Tail(alt3).SetNextDialogue(common);
            StoryEngine.AddChoicesOn(StoryEngine.Tail(pre),
                Choice("la2_c2_1", postMain),
                Choice("la2_c2_2", alt2),
                Choice("la2_c2_3", alt3));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(5, tracker), () => LoveDirector.Complete(5));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct2ThirdTrade()
        {
            StoreClient client = SophiaHomeClient("love_a2_03");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("TREAT");
            client.SetBudget(100);

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.Build("la2_s3_pre");
            Dialogue postMain = LoveText.Build("la2_s3_post_main");
            Dialogue common = LoveText.Build("la2_s3_common");
            Dialogue alt2 = LoveText.Build("la2_s3_c3_2");
            Dialogue alt3 = LoveText.Build("la2_s3_c3_3");
            StoryEngine.Tail(postMain).SetNextDialogue(common);
            StoryEngine.Tail(alt2).SetNextDialogue(common);
            StoryEngine.Tail(alt3).SetNextDialogue(common);
            StoryEngine.AddChoices(pre, L.Get("er_love_prompt_a2_c3"),
                Choice("la2_c3_1", postMain),
                Choice("la2_c3_2", alt2),
                Choice("la2_c3_3", alt3));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(6, tracker), () => LoveDirector.Complete(6));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct3FirstTrade()
        {
            StoreClient client = EleanorStory.NewNpcClient("love_a3_01",
                L.Get("er_name_lydia"), EleanorStory.LydiaSpriteKey(), EleanorStory.FactionKind.Upper);
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("TREAT");
            client.SetBudget(150);
            client.SetVariety();

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue root = LoveText.Build("la3_s1");
            client.mainDialogue = root;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(7, tracker), () => LoveDirector.Complete(7));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct3SecondTrade()
        {
            StoreClient client = EleanorStory.NewEleanorClient(
                "love_a3_02", "eleanor1", StoreClient.ClientIntent.BUY);
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(150);

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.Build(RomanceState.Route == 1
                ? "la3_s2_pre_e1" : "la3_s2_pre");
            pre.SetStartAction(StoryPresentation.Clear);
            Dialogue post = LoveText.Build("la3_s2_post");
            Dialogue alt2 = LoveText.Build("la3_s2_c1_2");
            Dialogue alt3 = LoveText.Build("la3_s2_c1_3");
            StoryEngine.Tail(alt2).SetNextDialogue(post);
            StoryEngine.Tail(alt3).SetNextDialogue(post);
            StoryEngine.AddChoices(pre, L.Get("er_love_prompt_a3_c1"),
                Choice("la3_c1_1", post),
                Choice("la3_c1_2", alt2),
                Choice("la3_c1_3", alt3));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(8, tracker), () => LoveDirector.Complete(8));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct3ThirdTrade()
        {
            StoreClient client = SophiaHomeClient("love_a3_03");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            client.SetBudget(200);
            client.SetVariety();

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue pre = LoveText.Build("la3_s3_pre");
            Dialogue postMain = LoveText.Build("la3_s3_post_main");
            Dialogue common = LoveText.Build("la3_s3_common");
            Dialogue alt2 = LoveText.Build("la3_s3_c2_2");
            Dialogue alt3 = LoveText.Build("la3_s3_c2_3");
            Dialogue smallHandoff = LoveText.Build("la3_s3_small_handoff");
            StoryEngine.Tail(postMain).SetNextDialogue(common);
            StoryEngine.Tail(alt3).SetNextDialogue(common);
            StoryEngine.Tail(alt2).SetNextDialogue(smallHandoff);
            StoryEngine.AddChoices(pre, L.Get("er_love_prompt_a3_c2"),
                Choice("la3_c2_1", postMain, () => LoveState.A3SmallWedding = false),
                Choice("la3_c2_2", alt2, () => LoveState.A3SmallWedding = true),
                Choice("la3_c2_3", alt3, () => LoveState.A3SmallWedding = false));

            client.mainDialogue = pre;
            StoryEngine.InterceptDismiss(client, () => TradeReaction(9, tracker), () => LoveDirector.Complete(9));
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateAct3Wedding()
        {
            StoreClient client = EleanorStory.NewEleanorClient(
                "love_a3_04", "eleanor1", StoreClient.ClientIntent.DIALOGUE);
            string weddingBlock = LoveState.A3SmallWedding
                ? (RomanceState.Route == 1 ? "la3_s4_small_e1" : "la3_s4_small")
                : (RomanceState.Route == 1 ? "la3_s4_e1" : "la3_s4");
            Dialogue root = LoveText.BuildBackup(L.Get("er_name_sophia"), weddingBlock);
            root.SetStartAction(() => StoryPresentation.ShowBackups(
                PortraitCompat.ResolveMotherHome(), "", "immediate", L.Get("er_name_sophia")));
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                LoveDirector.Complete(10);
                StoryEngine.GiveSelfRefillingPerfume();
                StoryDirector.CompleteCore();
                EndingPageUI.ShowLoveCompletion();
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateClosing()
        {
            StoreClient client = EleanorStory.NewEleanorClient(
                "love_a1_03", "eleanor1", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = LoveText.Build("la1_s3");
            root.SetStartAction(StoryPresentation.Clear);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                LoveDirector.Complete(3);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static Dialogue TradeReaction(int visit, StoryEngine.DealTracker tracker)
        {
            int outcome = !tracker.Any ? 0
                : tracker.Dominant == 'F' ? 1
                : tracker.Dominant == 'D' ? 2
                : 3;
            string prefix;
            if (visit == 1)
            {
                LoveState.T1Traded = tracker.Any;
                LoveState.T1Outcome = outcome;
                prefix = "la1_t1_";
            }
            else if (visit == 2)
            {
                LoveState.T2Traded = tracker.Any;
                LoveState.T2Outcome = outcome;
                prefix = "la1_t2_";
            }
            else if (visit == 4)
            {
                LoveState.A2T1Traded = tracker.Any;
                LoveState.A2T1Outcome = outcome;
                prefix = "la2_t1_";
            }
            else if (visit == 5)
            {
                LoveState.A2T2Traded = tracker.Any;
                LoveState.A2T2Outcome = outcome;
                prefix = "la2_t2_";
            }
            else if (visit == 6)
            {
                LoveState.A2T3Traded = tracker.Any;
                LoveState.A2T3Outcome = outcome;
                prefix = "la2_t3_";
            }
            else if (visit == 7)
            {
                LoveState.A3T1Traded = tracker.Any;
                LoveState.A3T1Outcome = outcome;
                prefix = "la3_t1_";
            }
            else if (visit == 8)
            {
                LoveState.A3T2Traded = tracker.Any;
                LoveState.A3T2Outcome = outcome;
                prefix = "la3_t2_";
            }
            else if (visit == 9)
            {
                LoveState.A3T3Traded = tracker.Any;
                LoveState.A3T3Outcome = outcome;
                prefix = "la3_t3_";
            }
            else
            {
                ModEntry.LogError("Trade reaction for unknown visit: " + visit);
                return LoveText.Build("la1_t1_none");
            }

            string id = prefix
                + (outcome == 0 ? "none" : outcome == 1 ? "fair" : outcome == 2 ? "discount" : "over");
            LoveState.SetInt("cursor." + id, 0);
            return LoveText.Build(id);
        }
    }
}
