using System;

namespace EleanorRomance
{
    /// <summary>Scene builders for the Lydia dual-romance extension.</summary>
    public static class DualStory
    {
        public static StoreClient CreateScene(string id)
        {
            if(id.StartsWith("dv",StringComparison.Ordinal))return DualV2Story.Create(id);
            switch (id)
            {
                case "entry": return CreateD01();
                case "reward": return CreateReward();
                case "daily": return CreateDaily();
                default:
                    ModEntry.LogError("Dual scene body not connected yet: " + id);
                    return null;
            }
        }

        private static StoreClient Lydia(string id)
        {
            return EleanorStory.NewNpcClient(id, L.Get("er_name_lydia"),
                EleanorStory.LydiaSpriteKey(), EleanorStory.FactionKind.Upper);
        }

        private static StoreClient Ready(StoreClient client, Dialogue root, string presentationBlock = null)
        {
            var presentation = presentationBlock == null ? null : DualContent.GetPresentation(presentationBlock);
            if (presentation != null)
                root.SetStartAction(() => StoryPresentation.Apply(presentation));
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static void Finish(Dialogue root, string scene, Action commit)
        {
            DualText.Finish(root, () =>
            {
                commit?.Invoke();
                DualDirector.CompleteScene(scene);
            });
        }

        /// <summary>
        /// Lydia is an Upper Level patron, never a seller. Scenes using this
        /// helper continue the written conversation, then let her buy luxury,
        /// art or high-end stock through the native haggling flow. The scene
        /// commits only when she leaves, so a mid-visit save cannot skip the
        /// trade half.
        /// </summary>
        private static void FinishAfterTrade(StoreClient client, string scene, Action commit = null, int budget = 500)
        {
            bool bought = false;
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add(TypeHelper.LUXURY_ITEM);
            client.clientBuyingTagList.Add(TypeHelper.ART);
            client.clientBuyingTagList.Add(TypeHelper.HIGHEND);
            client.SetBudget(budget);
            client.SetVariety();
            client.onItemBought += (item, paid) => { if (item != null) bought = true; };
            StoryEngine.InterceptDismiss(client,
                () => StoryEngine.Text(L.Get("er_name_lydia"),
                    L.Get(bought ? "er_dual_trade_close_bought" : "er_dual_trade_close_none")),
                () =>
                {
                    commit?.Invoke();
                    DualDirector.CompleteScene(scene);
                });
        }

        private static StoreClient CreateD01()
        {
            StoreClient client = Lydia("dual_d01");

            Dialogue root = DualText.Build("d01_b001", "d01_b002");
            Dialogue push = BuildD01Rest("d01_b003");
            Dialogue refuse = BuildD01Rest("d01_b004");
            Dialogue putBack = BuildD01Rest("d01_b005");
            DualText.Choices(root, "er_d01_c1_prompt",
                DualText.Choice("d01_c1_1", push),
                DualText.Choice("d01_c1_2", refuse),
                DualText.Choice("d01_c1_3", putBack));

            DualText.Finish(root, () =>
            {
                DualState.Accept();
                DualState.Flag("d.act1", true);
                DualDirector.CompleteScene("entry");
            });
            return Ready(client, root, "d01_b001");
        }

        private static Dialogue BuildD01Rest(string firstBlock)
        {
            Dialogue head = DualText.Build(firstBlock);
            DualText.Append(head, "d01_b006");
            Dialogue list = DualText.Build("d01_b008", "d01_b010");
            Dialogue noSend = DualText.Build("d01_b007", "d01_b010");
            Dialogue take = DualText.Build("d01_b009", "d01_b010");
            DualText.Choices(head, "er_d01_c2_prompt",
                DualText.Choice("d01_c2_1", list),
                DualText.Choice("d01_c2_2", noSend),
                DualText.Choice("d01_c2_3", take));
            return head;
        }

        private static StoreClient CreateReward()
        {
            int stage = DualRewards.DueRewrittenReward(DualState.Today);
            if (stage == 0) return null;
            var client = Lydia("dual_reward");
            var root = StoryEngine.Text(client.displayName,
                L.Get(DualRewards.Received(stage) ? "er_dual_claim_line" : "er_dual_reputation_gift",
                    DualRewards.Name(stage)));
            Finish(root, "reward", () =>
            {
                if (!DualRewards.Give(stage)) StoryDirector.Notify(L.Get("er_dual_plaque_full"));
                DualState.SetInt("reward.visit_day", DualState.Today);
            });
            return Ready(client, root);
        }

        private static StoreClient Eleanor(string id, string sprite = "eleanor1")
        {
            return EleanorStory.NewEleanorClient(id, sprite, StoreClient.ClientIntent.DIALOGUE);
        }

        private static StoreClient CreateDaily()
        {
            int rotation = DualState.DailyIndex % 2;
            int index = rotation + 2;
            bool lydia = index == 2;
            StoreClient client = lydia ? Lydia("dual_daily_" + index)
                : Eleanor("dual_daily_" + index, "eleanor1");
            Dialogue root = lydia
                ? DualText.Build("dr_daily_" + index + "_open", "dr_daily_2_shop")
                : DualText.Build("dr_daily_" + index + "_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_dual_entry_resume"));
            Action advance = () =>
            {
                DualState.DailyIndex = (rotation + 1) % 2;
                DualState.LastDailyDay = DualState.Today;
            };
            if (lydia) FinishAfterTrade(client, "daily", advance, 300);
            else DualText.Finish(root, () =>
            {
                advance();
                DualDirector.CompleteScene("daily");
            });
            return Ready(client, root);
        }

    }
}
