using System;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Revolution route (design R01, R02, R04-R14). The route starts from the
    /// unprotected-public c14 branch (Route 3) and runs as chapters 101-113.
    /// Supply is counted by the native value of items actually sold to Eleanor,
    /// never by the cash the player received.
    /// </summary>
    public static class RevolutionStory
    {
        public const int G1 = 400;
        public const int G2 = 1000;
        public const int G3 = 1800;

        public const int ChR01 = 101;
        public const int ChR02 = 102;
        public const int ChR04 = 103;
        public const int ChR05 = 104;
        public const int ChR06 = 105;
        public const int ChR07 = 106;
        public const int ChR08 = 107;
        public const int ChR09 = 108;
        public const int ChR10 = 109;
        public const int ChR11 = 110;
        public const int ChR12 = 111;
        public const int ChR13 = 112;
        public const int ChR14 = 113;

        private const string RevPortrait = "eleanor_rev";
        private const string HardshipPortrait = "eleanor3";
        private const string VestPortrait = "er_portrait_eleanor_revolution_undersuit_female_beauty";

        public static bool IsChapter(int chapter)
        {
            return chapter >= ChR01 && chapter <= ChR14;
        }

        /// <summary>R06 and R14 are morning visits that go ahead of ordinary customers.</summary>
        public static bool UseMorningQueue(int chapter)
        {
            return chapter == ChR06 || chapter == ChR14;
        }

        /// <summary>
        /// Returns true while a gated chapter must not play yet. The waiting
        /// visits are supplied by TryQueueDaily; ordinary business continues.
        /// </summary>
        public static bool BlocksChapter(int chapter, int day)
        {
            switch (chapter)
            {
                case ChR07: return RomanceState.RevSupply < G1;
                case ChR10: return RomanceState.RevSupply < G2;
                case ChR12: return RomanceState.RevSupply < G3;
                case ChR13: return day < RomanceState.RevR13EarliestDay;
                default: return false;
            }
        }

        public static void OnLoaded()
        {
            MigrateLegacySave();
        }

        /// <summary>
        /// Saves made inside the old eight-visit route C resume at the nearest
        /// new stage. They keep their real facts and do not receive the new R14
        /// completion or ending page retroactively.
        /// </summary>
        private static void MigrateLegacySave()
        {
            if (RomanceState.Route != 3) return;
            if (RomanceState.RevVersion >= 1) return;
            RomanceState.RevVersion = 1;

            if (RomanceState.Chapter >= RomanceState.ChapterComplete)
            {
                RomanceState.RevLegacyCompleted = true;
                StoryDirector.Log("Old route C save marked legacy-complete; new R14 ending is not granted.");
                return;
            }

            if (RomanceState.Chapter >= ChR01 && RomanceState.Chapter <= ChR09)
            {
                EnsureRevolutionFacts();
                StoryDirector.Log("Old route C save resumed at new revolution chapter "
                    + RomanceState.Chapter + ".");
            }
        }

        private static void EnsureRevolutionFacts()
        {
            RomanceState.Route = 3;
            RomanceState.MotherState = 2;
            if (RomanceState.DriveState != 5 && RomanceState.DriveState != 6)
                RomanceState.DriveState = 5;
            StoryEngine.FreeNutrifruitForLeak();
        }

        public static StoreClient CreateChapter(int chapter)
        {
            switch (chapter)
            {
                case ChR01: return CreateR01();
                case ChR02: return CreateR02();
                case ChR04: return CreateR04();
                case ChR05: return CreateR05();
                case ChR06: return CreateR06();
                case ChR07: return CreateR07();
                case ChR08: return CreateR08();
                case ChR09: return CreateR09();
                case ChR10: return CreateR10();
                case ChR11: return CreateR11();
                case ChR12: return CreateR12();
                case ChR13: return CreateR13();
                case ChR14: return CreateR14();
                default:
                    StoryDirector.LogError("Unknown revolution chapter " + chapter);
                    return null;
            }
        }

        // ------------------------------------------------------------------
        // Chapters
        // ------------------------------------------------------------------

        private static StoreClient CreateR01()
        {
            StoreClient client = Client("rev_r01", HardshipPortrait, StoreClient.ClientIntent.DIALOGUE, upper: true);
            Dialogue root = StoryEngine.Build(RevolutionContent.B_RV01_B001);
            Dialogue common = StoryEngine.Chain(
                RevolutionContent.B_RV01_B002);
            if (MaidState.HasHistory)
                StoryEngine.AppendTo(common, RevolutionContent.B_RV01_B003);
            StoryEngine.AppendTo(common, RevolutionContent.B_RV01_B004);

            Action finish = () =>
            {
                EnsureRevolutionFacts();
                Advance(ChR01, ChR02);
                StoryEngine.Dismiss();
            };
            StoryEngine.Tail(common).SetEndAction(finish);

            StoryEngine.AddChoices(root, L.Get("er_rev_prompt_r01"),
                new StoryEngine.Choice(L.Get("er_rev_choice_r01_1"), L.Get("er_rev_choice_r01_1_desc"), common, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r01_2"), L.Get("er_rev_choice_r01_2_desc"), common, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r01_3"), L.Get("er_rev_choice_r01_3_desc"), common, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR02()
        {
            StoreClient client = Client("rev_r02", HardshipPortrait, StoreClient.ClientIntent.SELL, upper: true);
            client.SetWholesale(20);
            bool sold = false;
            client.onItemSold += (item, paid) => sold = true;

            Dialogue main = StoryEngine.Chain(
                RevolutionContent.B_RV02_B001,
                RevolutionContent.B_RV02_B002,
                RevolutionContent.B_RV02_B003,
                RevolutionContent.B_RV02_B004);
            StoryEngine.Tail(main).SetEndAction(SpawnR02Stock);
            client.mainDialogue = main;

            FinishTrade(client, () =>
            {
                Dialogue wrap = StoryEngine.Build(sold
                    ? RevolutionContent.B_RV02_B005
                    : RevolutionContent.B_RV02_B006);
                StoryEngine.AppendTo(wrap, RevolutionContent.B_RV02_B007);
                return wrap;
            }, () =>
            {
                RomanceState.RevLydiaTold = true;
                Advance(ChR02, ChR04);
            });

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR04()
        {
            StoreClient client = SupplyClient("rev_r04", RevPortrait, 300);
            bool delivered = false;
            client.onItemBought += (item, paid) =>
            {
                delivered = true;
                RecordSupply(item, paid);
            };

            Dialogue root = StoryEngine.Build(RevolutionContent.B_RV04_B001);
            Dialogue offer = StoryEngine.Build(RevolutionContent.B_RV04_B002);
            Dialogue layOut = StoryEngine.Text(L.Player, L.Get("er_rev_bridge_r04_1"));
            Dialogue teach = StoryEngine.Text(L.Player, L.Get("er_rev_bridge_r04_2"));
            StoryEngine.AddChoices(root, L.Get("er_rev_prompt_r04"),
                new StoryEngine.Choice(L.Get("er_rev_choice_r04_1"), L.Get("er_rev_choice_r04_1_desc"), layOut, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r04_2"), L.Get("er_rev_choice_r04_2_desc"), teach, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r04_3"), L.Get("er_rev_choice_r04_3_desc"), offer, null));
            client.mainDialogue = root;

            FinishTrade(client, () =>
            {
                Dialogue wrap = StoryEngine.Build(delivered
                    ? RevolutionContent.B_RV04_B003
                    : RevolutionContent.B_RV04_B004);
                StoryEngine.AppendTo(wrap, RevolutionContent.B_RV04_B005);
                if (!RomanceState.RevLydiaTold)
                    StoryEngine.AppendTo(wrap, RevolutionContent.B_RV04_B006);
                return wrap;
            }, () => Advance(ChR04, ChR05));

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR05()
        {
            StoreClient client = Client("rev_r05", RevPortrait, StoreClient.ClientIntent.SELL, upper: false);
            client.SetWholesale(30);
            bool sold = false;
            client.onItemSold += (item, paid) => sold = true;

            Dialogue main = StoryEngine.Build(RevolutionContent.B_RV05_B001);
            StoryEngine.Tail(main).SetEndAction(SpawnR05Stock);
            client.mainDialogue = main;

            FinishTrade(client, () =>
            {
                Dialogue wrap = StoryEngine.Build(sold
                    ? RevolutionContent.B_RV05_B002
                    : RevolutionContent.B_RV05_B003);
                StoryEngine.AppendTo(wrap, RevolutionContent.B_RV05_B004);
                return wrap;
            }, () => Advance(ChR05, ChR06));

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR06()
        {
            StoreClient client = Client("rev_r06", PrivatePortrait(), StoreClient.ClientIntent.DIALOGUE, upper: false);
            Dialogue root = StoryEngine.Chain(
                RevolutionContent.B_RV06_B001,
                RevolutionContent.B_RV06_B002,
                RevolutionContent.B_RV06_B003);

            Dialogue withoutCoat = StoryEngine.Chain(
                RevolutionContent.B_RV06_B004,
                RevolutionContent.B_RV06_B006,
                RevolutionContent.B_RV06_B007);
            Dialogue withCoat = StoryEngine.Chain(
                RevolutionContent.B_RV06_B004,
                RevolutionContent.B_RV06_B005,
                RevolutionContent.B_RV06_B006,
                RevolutionContent.B_RV06_B007);
            Action finish = () =>
            {
                Advance(ChR06, ChR07);
                StoryEngine.Dismiss();
            };
            StoryEngine.Tail(withoutCoat).SetEndAction(finish);
            StoryEngine.Tail(withCoat).SetEndAction(finish);

            StoryEngine.AddChoices(root, L.Get("er_rev_prompt_r06"),
                new StoryEngine.Choice(L.Get("er_rev_choice_r06_1"), L.Get("er_rev_choice_r06_1_desc"), withoutCoat, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r06_2"), L.Get("er_rev_choice_r06_2_desc"), withoutCoat, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r06_3"), L.Get("er_rev_choice_r06_3_desc"), withCoat, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR07()
        {
            StoreClient client = SupplyClient("rev_r07", RevPortrait, 600);
            TrackSupply(client);
            Dialogue main = StoryEngine.Build(RevolutionContent.B_RV07_B001);
            client.mainDialogue = main;

            FinishTrade(client, () =>
            {
                Dialogue wrap = StoryEngine.Chain(
                    RevolutionContent.B_RV07_B002,
                    RevolutionContent.B_RV07_B003);
                return wrap;
            }, () =>
            {
                RomanceState.RevTier = Math.Max(RomanceState.RevTier, 1);
                Advance(ChR07, ChR08);
            });

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR08()
        {
            StoreClient client = Client("rev_r08", RevPortrait, StoreClient.ClientIntent.SELL, upper: false);
            client.SetWholesale(25);
            bool sold = false;
            client.onItemSold += (item, paid) => sold = true;

            Dialogue main = StoryEngine.Build(RevolutionContent.B_RV08_B001);
            StoryEngine.Tail(main).SetEndAction(SpawnR08Stock);
            client.mainDialogue = main;

            FinishTrade(client, () =>
            {
                Dialogue wrap = StoryEngine.Chain(sold
                    ? RevolutionContent.B_RV08_B002
                    : RevolutionContent.B_RV08_B003);
                StoryEngine.AppendTo(wrap, RevolutionContent.B_RV08_B004);
                return wrap;
            }, () => Advance(ChR08, ChR09));

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR09()
        {
            StoreClient client = SupplyClient("rev_r09", RevPortrait, 400);
            TrackSupply(client);
            Dialogue main = StoryEngine.Build(RevolutionContent.B_RV09_B001);
            client.mainDialogue = main;

            FinishTrade(client, () => StoryEngine.Chain(
                RevolutionContent.B_RV09_B002,
                RevolutionContent.B_RV09_B003,
                RevolutionContent.B_RV09_B004), () => Advance(ChR09, ChR10));

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR10()
        {
            StoreClient client = SupplyClient("rev_r10", RevPortrait, 600);
            TrackSupply(client);
            Dialogue main = StoryEngine.Chain(
                RevolutionContent.B_RV10_B001,
                RevolutionContent.B_RV10_B002,
                RevolutionContent.B_RV10_B003);
            client.mainDialogue = main;

            FinishTrade(client, () => StoryEngine.Build(RevolutionContent.B_RV10_B004), () =>
            {
                RomanceState.RevTier = Math.Max(RomanceState.RevTier, 2);
                Advance(ChR10, ChR11);
            });

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR11()
        {
            StoreClient client = Client("rev_r11", PrivatePortrait(), StoreClient.ClientIntent.DIALOGUE, upper: false);
            Dialogue root = StoryEngine.Chain(
                RevolutionContent.B_RV11_B001,
                RevolutionContent.B_RV11_B002);
            Dialogue common = StoryEngine.Chain(
                RevolutionContent.B_RV11_B003,
                RevolutionContent.B_RV11_B004,
                RevolutionContent.B_RV11_B005);
            StoryEngine.Tail(common).SetEndAction(() =>
            {
                Advance(ChR11, ChR12);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, L.Get("er_rev_prompt_r11"),
                new StoryEngine.Choice(L.Get("er_rev_choice_r11_1"), L.Get("er_rev_choice_r11_1_desc"), common, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r11_2"), L.Get("er_rev_choice_r11_2_desc"), common, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r11_3"), L.Get("er_rev_choice_r11_3_desc"), common, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR12()
        {
            StoreClient client = SupplyClient("rev_r12", RevPortrait, 800);
            TrackSupply(client);
            Dialogue main = StoryEngine.Chain(
                RevolutionContent.B_RV12_B001,
                RevolutionContent.B_RV12_B002);
            client.mainDialogue = main;

            FinishTrade(client, () => StoryEngine.Build(RevolutionContent.B_RV12_B003), () =>
            {
                Advance(ChR12, ChR13);
                int day = Today();
                RomanceState.RevR13EarliestDay = day + 3;
                RomanceState.NextVisitDay = day + 3;
            });

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR13()
        {
            StoreClient client = Client("rev_r13", RevPortrait, StoreClient.ClientIntent.SELL, upper: false);
            client.SetWholesale(35);

            Dialogue main = StoryEngine.Chain(
                RevolutionContent.B_RV13_B001,
                RevolutionContent.B_RV13_B002,
                RevolutionContent.B_RV13_B003);
            StoryEngine.Tail(main).SetEndAction(SpawnR13Stock);
            client.mainDialogue = main;

            FinishTrade(client, () => StoryEngine.Build(RevolutionContent.B_RV13_B004), () =>
            {
                RomanceState.RevTier = 3;
                Advance(ChR13, ChR14);
                RomanceState.NextVisitDay = Today() + 1;
            });

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateR14()
        {
            StoreClient client = Client("rev_r14", PrivatePortrait(), StoreClient.ClientIntent.DIALOGUE, upper: false);
            Dialogue root = StoryEngine.Build(RevolutionContent.B_RV14_B001);

            Dialogue withCup = StoryEngine.Chain(
                RevolutionContent.B_RV14_B002,
                RevolutionContent.B_RV14_B003,
                RevolutionContent.B_RV14_B004);
            Dialogue withoutCup = StoryEngine.Chain(
                RevolutionContent.B_RV14_B002,
                RevolutionContent.B_RV14_B004);

            Action finish = () =>
            {
                RomanceState.RevComplete = true;
                RomanceState.RevEndingShown = false;
                Advance(ChR14, RomanceState.ChapterComplete);
                StoryEngine.Dismiss();
                EndingPageUI.ShowRevolutionCompletion();
            };
            StoryEngine.Tail(withCup).SetEndAction(finish);
            StoryEngine.Tail(withoutCup).SetEndAction(finish);

            StoryEngine.AddChoices(root, L.Get("er_rev_prompt_r14"),
                new StoryEngine.Choice(L.Get("er_rev_choice_r14_1"), L.Get("er_rev_choice_r14_1_desc"), withCup, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r14_2"), L.Get("er_rev_choice_r14_2_desc"), withoutCup, null),
                new StoryEngine.Choice(L.Get("er_rev_choice_r14_3"), L.Get("er_rev_choice_r14_3_desc"), withoutCup, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // Waiting / daily visits
        // ------------------------------------------------------------------

        /// <summary>
        /// Queues a repeatable visit while a supply gate is waiting, or after
        /// R14. Returns false when there is nothing to fill today.
        /// </summary>
        public static bool TryQueueDaily(StoreClientManager manager, int day)
        {
            if (manager == null) return false;
            if (RomanceState.Route != 3 || !RomanceState.StoryActive) return false;
            if (RomanceState.RevLegacyCompleted && RomanceState.Chapter >= RomanceState.ChapterComplete)
                return TryQueuePostDaily(manager, day);

            int pending = RomanceState.PendingChapter > 0
                ? RomanceState.PendingChapter
                : RomanceState.Chapter;
            bool gateWait = pending >= ChR01 && pending <= ChR14 && BlocksChapter(pending, day);
            bool post = RomanceState.Chapter >= RomanceState.ChapterComplete && RomanceState.RevComplete;
            if (!gateWait && !post)
                return TryQueueLootVisit(manager, day, post: false);

            // R13 is the one interval when she is away; do not put her in the
            // shop for a chat visit between departure and return.
            if (pending == ChR13) return false;

            if (post && (RomanceState.RevLastLootDay < 0
                         || day >= RomanceState.RevLastLootDay + 6))
                return TryQueueLootVisit(manager, day, post: true);

            int cooldown = post ? 3 : 2;
            if (RomanceState.RevLastVisitDay >= 0 && day < RomanceState.RevLastVisitDay + cooldown)
                return false;

            int shortIndex = post
                ? 11 + ((day / 3) % 4)
                : (RomanceState.RevLastShort == 1 ? 3 : 1);
            string extraBlock = null;
            if (gateWait && !post)
            {
                bool even = day % 2 == 0;
                if (pending == ChR07)
                    extraBlock = even ? RevolutionContent.B_RV_SHORT_05 : RevolutionContent.B_RV_SHORT_06;
                else if (pending == ChR10)
                    extraBlock = even ? RevolutionContent.B_RV_SHORT_07 : RevolutionContent.B_RV_SHORT_08;
                else if (pending == ChR12)
                    extraBlock = even ? RevolutionContent.B_RV_SHORT_09 : RevolutionContent.B_RV_SHORT_10;
            }

            StoreClient client = CreateDaily(shortIndex, extraBlock);
            if (client == null) return false;

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            RomanceState.RevLastVisitDay = day;
            RomanceState.RevLastShort = shortIndex;
            StoryDirector.Log("Queued revolution daily " + shortIndex + " on day " + day + ".");
            return true;
        }

        private static bool TryQueueLootVisit(StoreClientManager manager, int day, bool post)
        {
            if (RomanceState.RevTier < 1) return false;
            int cooldown = post ? 6 : 4;
            if (RomanceState.RevLastLootDay >= 0 && day < RomanceState.RevLastLootDay + cooldown)
                return false;

            StoreClient client = Client("rev_loot",
                RomanceState.RevComplete ? PrivatePortrait() : RevPortrait,
                StoreClient.ClientIntent.SELL, upper: false);
            client.SetWholesale(30 + RomanceState.RevTier * 5);
            client.dismissable = true;
            Dialogue main = StoryEngine.Build(RevolutionContent.B_RV_SHORT_07);
            StoryEngine.Tail(main).SetEndAction(SpawnTierStock);
            client.mainDialogue = main;
            client.CompleteClientCreation();

            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            RomanceState.RevLastVisitDay = day;
            RomanceState.RevLastLootDay = day;
            StoryDirector.Log("Queued revolution loot visit (tier " + RomanceState.RevTier
                + ") on day " + day + ".");
            return true;
        }

        private static bool TryQueuePostDaily(StoreClientManager manager, int day)
        {
            if (RomanceState.RevLastVisitDay >= 0 && day < RomanceState.RevLastVisitDay + 3) return false;
            int shortIndex = 11 + ((day / 3) % 4);
            StoreClient client = CreateDaily(shortIndex);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            RomanceState.RevLastVisitDay = day;
            RomanceState.RevLastShort = shortIndex;
            StoryDirector.Log("Queued legacy revolution daily " + shortIndex + " on day " + day + ".");
            return true;
        }

        private static StoreClient CreateDaily(int index, string extraBlock = null)
        {
            try
            {
                switch (index)
                {
                    case 1:
                    {
                        StoreClient c = SupplyClient("rev_d01", RevPortrait, 250);
                        c.clientBuyingTagList.Clear();
                        c.clientBuyingTagList.Add("MEDICAL");
                        TrackSupply(c);
                        c.dismissable = true;
                        c.mainDialogue = SupplyDialogue(RevolutionContent.B_RV_SHORT_01, extraBlock);
                        c.CompleteClientCreation();
                        return c;
                    }
                    case 2:
                    {
                        StoreClient c = Client("rev_d02", RevPortrait, StoreClient.ClientIntent.SELL, upper: false);
                        c.SetWholesale(20);
                        c.dismissable = true;
                        c.mainDialogue = StoryEngine.Build(RevolutionContent.B_RV_SHORT_02);
                        StoryEngine.Tail(c.mainDialogue).SetEndAction(SpawnDailyTrio);
                        c.CompleteClientCreation();
                        return c;
                    }
                    case 3:
                    {
                        StoreClient c = SupplyClient("rev_d03", RevPortrait, 250);
                        c.clientBuyingTagList.Clear();
                        c.clientBuyingTagList.Add("TOOL");
                        c.clientBuyingTagList.Add("MEDICAL");
                        TrackSupply(c);
                        c.dismissable = true;
                        c.mainDialogue = SupplyDialogue(RevolutionContent.B_RV_SHORT_03, extraBlock);
                        c.CompleteClientCreation();
                        return c;
                    }
                    case 4:
                    {
                        StoreClient c = Client("rev_d04", RevPortrait, StoreClient.ClientIntent.BUY, upper: false);
                        c.clientBuyingTagList.Add("FOOD");
                        c.SetBudget(150);
                        c.dismissable = true;
                        c.mainDialogue = StoryEngine.Build(RevolutionContent.B_RV_SHORT_04);
                        c.CompleteClientCreation();
                        return c;
                    }
                    case 5:
                        return DialogueDaily("rev_d05", PrivatePortrait(), RevolutionContent.B_RV_SHORT_05);
                    case 6:
                        return DialogueDaily("rev_d06", RevPortrait, RevolutionContent.B_RV_SHORT_06);
                    case 7:
                        return DialogueDaily("rev_d07", RevPortrait, RevolutionContent.B_RV_SHORT_07);
                    case 8:
                        return DialogueDaily("rev_d08", RevPortrait, RevolutionContent.B_RV_SHORT_08);
                    case 9:
                        return DialogueDaily("rev_d09", RevPortrait, RevolutionContent.B_RV_SHORT_09);
                    case 10:
                        return DialogueDaily("rev_d10", PrivatePortrait(), RevolutionContent.B_RV_SHORT_10);
                    case 11:
                        return DialogueDaily("rev_d11", PrivatePortrait(), RevolutionContent.B_RV_SHORT_11);
                    case 12:
                        return DialogueDaily("rev_d12", PrivatePortrait(), RevolutionContent.B_RV_SHORT_12);
                    case 13:
                        return DialogueDaily("rev_d13", PrivatePortrait(), RevolutionContent.B_RV_SHORT_13);
                    case 14:
                        return DialogueDaily("rev_d14", PrivatePortrait(), RevolutionContent.B_RV_SHORT_14);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Revolution daily " + index + " failed: " + ex);
                return null;
            }
        }

        private static Dialogue SupplyDialogue(string primary, string extraBlock)
        {
            return string.IsNullOrEmpty(extraBlock)
                ? StoryEngine.Build(primary)
                : StoryEngine.Chain(primary, extraBlock);
        }

        private static StoreClient DialogueDaily(string id, string portrait, string block)
        {
            StoreClient c = Client(id, portrait, StoreClient.ClientIntent.DIALOGUE, upper: false);
            Dialogue dialogue = StoryEngine.Build(block);
            StoryEngine.Tail(dialogue).SetEndAction(StoryEngine.Dismiss);
            c.mainDialogue = dialogue;
            c.dismissable = true;
            c.CompleteClientCreation();
            return c;
        }

        // ------------------------------------------------------------------
        // Construction helpers
        // ------------------------------------------------------------------

        private static StoreClient Client(string id, string portrait, StoreClient.ClientIntent intent, bool upper)
        {
            return EleanorStory.NewEleanorClient(id, portrait, intent, upper);
        }

        private static StoreClient SupplyClient(string id, string portrait, int budget)
        {
            StoreClient client = Client(id, portrait, StoreClient.ClientIntent.BUY, upper: false);
            client.clientBuyingTagList.Add("MEDICAL");
            client.clientBuyingTagList.Add("WEAPON");
            client.clientBuyingTagList.Add("MELEE_WEAPON");
            client.clientBuyingTagList.Add("TOOL");
            client.SetBudget(budget);
            client.SetVariety();
            return client;
        }

        private static string PrivatePortrait()
        {
            return PortraitCompat.ResolveOutfit(VestPortrait, RevPortrait);
        }

        private static void FinishTrade(StoreClient client, Func<Dialogue> wrap, Action onComplete)
        {
            StoryEngine.InterceptDismiss(client, wrap, onComplete);
        }

        private static void Advance(int expected, int next)
        {
            StoryDirector.AdvanceChapter(expected, next);
        }

        private static int Today()
        {
            return StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
        }

        // ------------------------------------------------------------------
        // Supply accounting
        // ------------------------------------------------------------------

        private static void TrackSupply(StoreClient client)
        {
            client.onItemBought += (item, paid) => RecordSupply(item, paid);
        }

        private static void RecordSupply(GameItem item, int paid)
        {
            if (!CountsAsSupply(item)) return;
            long value = ItemValue(item, paid);
            if (value <= 0) return;
            int before = RomanceState.RevSupply;
            long total = (long)before + value;
            RomanceState.RevSupply = total > int.MaxValue ? int.MaxValue : (int)total;
            NotifySupply(before, RomanceState.RevSupply);
        }

        /// <summary>Only weapons and medicine advance the supply gates; tools and food are normal trade.</summary>
        private static bool CountsAsSupply(GameItem item)
        {
            if (item == null) return false;
            try
            {
                return item.IsTag("MEDICAL") || item.IsTag("WEAPON") || item.IsTag("MELEE_WEAPON");
            }
            catch
            {
                return true; // Unknown item metadata should not silently lose delivery value.
            }
        }

        private static long ItemValue(GameItem item, int paid)
        {
            if (item == null) return 0;
            long value = 0;
            try { value = item.GetBaseValue(); } catch { }
            if (value <= 0) value = item.unitValue;
            if (value <= 0) value = Math.Max(0, paid);
            return value;
        }

        private static int NextTarget(int total)
        {
            if (total < G1) return G1;
            if (total < G2) return G2;
            if (total < G3) return G3;
            return 0;
        }

        private static void NotifySupply(int before, int total)
        {
            int target = NextTarget(total);
            StoryDirector.Notify(target > 0
                ? L.Get("er_rev_supply_tip", total, target)
                : L.Get("er_rev_supply_done", total));
            if (before < G1 && total >= G1) StoryDirector.Notify(L.Get("er_rev_supply_g1"));
            if (before < G2 && total >= G2) StoryDirector.Notify(L.Get("er_rev_supply_g2"));
            if (before < G3 && total >= G3) StoryDirector.Notify(L.Get("er_rev_supply_g3"));
        }

        // ------------------------------------------------------------------
        // Stock
        // ------------------------------------------------------------------

        private static void SpawnR02Stock()
        {
            if (RomanceState.RevStockR02) return;
            RomanceState.RevStockR02 = true;
            SpawnNamed("skincare_cream", "er_rev_item_comb", 1, stolen: false);
            SpawnNamed("box_tampon", "er_rev_item_gloves", 1, stolen: false);
            SpawnNamed("pheromone_perfume", "er_rev_item_clock", 1, stolen: false);
            SpawnNamed("kotton_fabric", "er_rev_item_fabric", 1, stolen: false);
        }

        private static void SpawnR05Stock()
        {
            if (RomanceState.RevStockR05) return;
            RomanceState.RevStockR05 = true;
            SpawnNamed("bandage_item", "er_rev_item_needles", 2, stolen: true);
            SpawnNamed("wire_cutter", "er_rev_item_pliers", 3, stolen: true);
            SpawnNamed("water_filter", "er_rev_item_valves", 1, stolen: true);
        }

        private static void SpawnR08Stock()
        {
            if (RomanceState.RevStockR08) return;
            RomanceState.RevStockR08 = true;
            SpawnNamed("skincare_cream", "er_rev_item_compact", 1, stolen: false);
        }

        private static void SpawnR13Stock()
        {
            if (RomanceState.RevStockR13) return;
            RomanceState.RevStockR13 = true;
            SpawnLoot("glock_receiver");
            SpawnLoot("blue_blood_bag");
            SpawnLoot("surgery_tool");
            SpawnLoot("skincare_cream");
            SpawnLoot("neuroactive_perfume");
        }

        private static void SpawnDailyTrio()
        {
            int day = Today();
            if (RomanceState.RevDailyStockDay == day) return;
            RomanceState.RevDailyStockDay = day;
            SpawnLoot("screwdriver");
            SpawnLoot("screwdriver");
            SpawnLoot("screwdriver");
        }

        private static void SpawnTierStock()
        {
            int day = Today();
            if (RomanceState.RevDailyStockDay == day) return;
            RomanceState.RevDailyStockDay = day;
            switch (RomanceState.RevTier)
            {
                case 1:
                    SpawnLoot("screwdriver");
                    SpawnLoot("wire_cutter");
                    SpawnLoot("toolbox");
                    SpawnLoot("skincare_cream");
                    break;
                case 2:
                    SpawnLoot("revolver");
                    SpawnLoot("hemostatic_bandage_item");
                    SpawnLoot("salve");
                    SpawnLoot("pheromone_perfume");
                    SpawnLoot("skincare_cream");
                    break;
                case 3:
                    SpawnLoot("glock_receiver");
                    SpawnLoot("blue_blood_bag");
                    SpawnLoot("surgery_tool");
                    SpawnLoot("neuroactive_perfume");
                    SpawnLoot("pheromone_perfume");
                    break;
            }
        }

        private static void SpawnLoot(string id)
        {
            SpawnNamed(id, null, 1, stolen: true);
        }

        private static GameItem SpawnNamed(string id, string nameKey, int amount, bool stolen)
        {
            try
            {
                GameItem item = ItemSpawner.Spawn(id);
                if (item == null)
                {
                    StoryDirector.LogError("Revolution stock item missing: " + id);
                    return null;
                }
                if (!string.IsNullOrEmpty(nameKey)) item.SetName(L.Get(nameKey));
                if (amount > 1) item.SetAmount(amount);
                ItemFeatureHelper.SetSecondHand(item);
                PlayerStore.Instance.AddDirectSellingItemToTable(item, isOwend: false, isStolen: stolen);
                return item;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Revolution stock spawn failed for " + id + ": " + ex);
                return null;
            }
        }
    }
}
