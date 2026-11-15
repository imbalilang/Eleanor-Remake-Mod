using System;

namespace EleanorRomance
{
    /// <summary>
    /// Harem-line v2 scene graphs: window (dv01-dv03), protection (dv04-dv07),
    /// double helix (dv08-dv11) and ending (dv12-dv13). Each record is one
    /// client visit; branch and common blocks come from Content/dual_romance_v04.json.
    /// </summary>
    internal static class DualV2Story
    {
        private const string LydiaKey = "Lydia";
        private static string LydiaName => L.Get("er_name_lydia");

        private static Dialogue Build(string companion, params string[] ids)
        {
            if (string.IsNullOrEmpty(companion)) return DualText.Build(ids);
            string name = companion == LydiaKey ? LydiaName : L.Eleanor;
            return DualText.BuildBackup(name, ids);
        }

        private static void PrepareClosingRewrite(string scene)
        {
            string revision = "closing.revision." + scene;
            if (DualState.Int(revision) >= 1) return;
            if (!DualState.Done(scene))
                for (int block = 1; block <= 4; block++)
                    DualState.SetInt("cursor." + scene + "_b" + block.ToString("000"), 0);
            DualState.SetInt(revision, 1);
        }

        public static StoreClient Create(string id)
        {
            switch (id)
            {
                case "dv01": return CreateDv01();
                case "dv02": return CreateDv02();
                case "dv03": return CreateDv03();
                case "dv04": return CreateDv04();
                case "dv05": return CreateDv05();
                case "dv06": return CreateDv06();
                case "dv07": return CreateDv07();
                case "dv08": return CreateDv08();
                case "dv09": return CreateDv09();
                case "dv10": return CreateDv10();
                case "dv11": return CreateDv11();
                case "dv12": PrepareClosingRewrite("dv12"); return CreateDv12();
                case "dv13": PrepareClosingRewrite("dv13"); return CreateDv13();
                default:
                    ModEntry.LogError("Unknown harem v2 scene: " + id);
                    return null;
            }
        }

        private static StoreClient Lydia(string id) => EleanorStory.NewNpcClient(
            id, L.Get("er_name_lydia"), EleanorStory.LydiaSpriteKey(), EleanorStory.FactionKind.Upper);

        private static StoreClient LydiaDate(string id) => EleanorStory.NewNpcClient(
            id, L.Get("er_name_lydia"), PortraitCompat.ResolveLydiaDate(), EleanorStory.FactionKind.Upper);

        private static StoreClient Eleanor(string id) => EleanorStory.NewEleanorClient(
            id, "eleanor1", StoreClient.ClientIntent.DIALOGUE);

        private static StoreClient Thug(string id) => EleanorStory.NewNpcClient(
            id, L.Get("er_name_thug"), "maleBM5", EleanorStory.FactionKind.BlackMarket);

        private static StoreClient Ready(StoreClient client, Dialogue root, string presentationBlock = null)
        {
            var presentation = presentationBlock == null ? null : DualContent.GetPresentation(presentationBlock);
            if (presentation != null) root.SetStartAction(() => StoryPresentation.Apply(presentation));
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static void Finish(StoreClient client, Dialogue root, string scene, Action flags = null)
        {
            DualText.Finish(root, () =>
            {
                flags?.Invoke();
                DualState.SetSceneDay(scene, DualState.Today);
                DualDirector.CompleteScene(scene);
            });
        }

        private static StoreClient Single(StoreClient client, string scene, string companion,
            string block, string presentationBlock = null, Action flags = null)
        {
            Dialogue root = Build(companion, block);
            Finish(client, root, scene, flags);
            return Ready(client, root, presentationBlock);
        }

        /// <summary>
        /// Linear root + one choice point. Non-mainline options map to their branch
        /// blocks in ascending option order; every branch continues through the
        /// common block. The mainline option goes straight to the common block.
        /// </summary>
        private static StoreClient SimpleChoice(StoreClient client, string scene, string companion,
            int choiceNumber, string rootBlock, string commonBlock, int mainline,
            string[] branchBlocks, string presentationBlock = null, Action flags = null)
        {
            Dialogue root = Build(companion, rootBlock);
            Dialogue common = string.IsNullOrEmpty(commonBlock) ? null : Build(companion, commonBlock);
            Dialogue[] branches = new Dialogue[branchBlocks.Length];
            for (int i = 0; i < branchBlocks.Length; i++)
                branches[i] = Build(companion, branchBlocks[i], commonBlock);

            int options = branchBlocks.Length + 1;
            var choices = new StoryEngine.Choice[options];
            string prefix = scene + "_c" + choiceNumber + "_";
            int branchIndex = 0;
            for (int opt = 1; opt <= options; opt++)
            {
                Dialogue target = opt == mainline ? common : branches[branchIndex++];
                choices[opt - 1] = DualText.Choice(prefix + opt, target);
            }
            DualText.Choices(root, "er_dual_prompt", choices);
            Finish(client, root, scene, flags);
            return Ready(client, root, presentationBlock);
        }

        private static StoreClient CreateDv01() => SimpleChoice(Lydia("dual_dv01"),
            "dv01", "Eleanor", 1, "dv01_b001", "dv01_b004", 1,
            new[] { "dv01_b002", "dv01_b003" }, "dv01_b001");

        private static StoreClient CreateDv02()
        {
            StoreClient client = Lydia("dual_dv02");
            Dialogue root = Build("Eleanor", "dv02_b001");
            Dialogue hidden = Build("Eleanor", "dv02_b002");
            Dialogue common = Build("Eleanor", "dv02_b003");
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dv02_c2_1", common),
                DualText.Choice("dv02_c2_2", hidden,
                    () => DualState.Flag("dv02.c2_hidden", true)));
            Finish(client, root, "dv02");
            return Ready(client, root, "dv02_b001");
        }

        private static StoreClient CreateDv03()
        {
            StoreClient client = Lydia("dual_dv03");
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(120);
            client.SetVariety();

            var tracker = new StoryEngine.DealTracker();
            client.onItemBought += (item, paid) =>
            {
                if (item != null) tracker.Record(item, paid, eleanorIsSeller: false);
            };

            Dialogue root = Build("Eleanor", "dv03_b001");
            Dialogue branch = Build("Eleanor", "dv03_b002", "dv03_b003");
            Dialogue common = Build("Eleanor", "dv03_b003");
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dv03_c3_1", common),
                DualText.Choice("dv03_c3_2", branch));
            StoryEngine.InterceptDismiss(client, () => TradeReaction(tracker), () =>
            {
                DualState.SetSceneDay("dv03", DualState.Today);
                DualDirector.CompleteScene("dv03");
            });
            return Ready(client, root, "dv03_b001");
        }

        private static Dialogue TradeReaction(StoryEngine.DealTracker tracker)
        {
            string label = !tracker.Any ? "dv03_b007"
                : tracker.Dominant == 'F' ? "dv03_b004"
                : tracker.Dominant == 'D' ? "dv03_b005"
                : "dv03_b006";
            DualState.SetInt("cursor." + label, 0);
            return Build("Eleanor", label);
        }

        private static StoreClient CreateDv04() => SimpleChoice(Thug("dual_dv04"),
            "dv04", null, 1, "dv04_b001", "dv04_b004", 2,
            new[] { "dv04_b002", "dv04_b003" });

        private static StoreClient CreateDv05() => Single(Lydia("dual_dv05"),
            "dv05", "Eleanor", "dv05_b001", "dv05_b001");

        private static StoreClient CreateDv06() => SimpleChoice(Lydia("dual_dv06"),
            "dv06", "Eleanor", 2, "dv06_b001", "dv06_b003", 1,
            new[] { "dv06_b002" }, "dv06_b001", () =>
            {
                DualState.Flag("dv06.injured", true);
                DualState.Flag("dv06.store_damaged", true);
            });

        private static StoreClient CreateDv07() => Single(Eleanor("dual_dv07"),
            "dv07", "Lydia", "dv07_b001", "dv07_b001");

        private static StoreClient CreateDv08() => SimpleChoice(Eleanor("dual_dv08"),
            "dv08", null, 1, "dv08_b001", "dv08_b004", 1,
            new[] { "dv08_b002", "dv08_b003" }, null, () =>
            {
                DualState.Relation = 1;
                DualState.Flag("d.act3a", true);
            });

        private static StoreClient CreateDv09() => SimpleChoice(LydiaDate("dual_dv09"),
            "dv09", null, 2, "dv09_b001", "dv09_b004", 1,
            new[] { "dv09_b002", "dv09_b003" }, null,
            () => DualState.Flag("dv09.agreed", true));

        private static StoreClient CreateDv10()
        {
            StoreClient client = LydiaDate("dual_dv10");
            Dialogue root = Build(null, "dv10_b001");
            Dialogue branch = Build(null, "dv10_b002", "dv10_b004");
            Dialogue branch3 = Build(null, "dv10_b003", "dv10_b004");
            Dialogue common = Build(null, "dv10_b004");
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dv10_c3_1", common),
                DualText.Choice("dv10_c3_2", branch),
                DualText.Choice("dv10_c3_3", branch3));
            StoryEngine.OnDialogueFinished(root, DualClosingDay.FinishV2);
            return Ready(client, root);
        }

        private static StoreClient CreateDv11() => SimpleChoice(Lydia("dual_dv11"),
            "dv11", null, 4, "dv11_b001", "dv11_b004", 1,
            new[] { "dv11_b002", "dv11_b003" }, null, () =>
            {
                DualState.Flag("dv11.told", true);
                DualState.Flag("d.act3b", true);
            });

        private static StoreClient CreateDv12() => SimpleChoice(Lydia("dual_dv12"),
            "dv12", null, 1, "dv12_b001", "dv12_b004", 1,
            new[] { "dv12_b002", "dv12_b003" }, null,
            () => DualState.Flag("d.act4a", true));

        private static StoreClient CreateDv13()
        {
            StoreClient client = Eleanor("dual_dv13");
            Dialogue root = Build("Lydia", "dv13_b001");
            Dialogue branch = Build("Lydia", "dv13_b002", "dv13_b004");
            Dialogue branch3 = Build("Lydia", "dv13_b003", "dv13_b004");
            Dialogue common = Build("Lydia", "dv13_b004");
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dv13_c2_1", branch),
                DualText.Choice("dv13_c2_2", branch3),
                DualText.Choice("dv13_c2_3", common));
            DualText.Finish(root, () =>
            {
                DualDirector.CompleteScene("dv13", () =>
                {
                    DualState.Relation = 2;
                    DualState.Flag("d.act4", true);
                });
                DualDirector.FinishRewrittenCore();
                EndingPageUI.ShowDualCompletion();
            });
            return Ready(client, root, "dv13_b001");
        }
    }
}
