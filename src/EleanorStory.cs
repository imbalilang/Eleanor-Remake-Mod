using System;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// v0.6 chapter clients.  The dialogue text lives in StoryV06Content and
    /// Localization; this file decides reachability, order and the real game
    /// behaviour that changes the result.
    /// </summary>
    public static class EleanorStory
    {
        private const string Prefix = "er_story_";

        public static StoreClient CreateClient(int chapter)
        {
            try
            {
                StoreClient maidClient = MaidStory.CreateMainChapter(chapter);
                if (maidClient != null) return maidClient;
                if (RomanceState.Route == 3 && RevolutionStory.IsChapter(chapter))
                    return RevolutionStory.CreateChapter(chapter);
                switch (chapter)
                {
                    case 1: return CreateFoodChapter();
                    case 2: return CreateHouseholdChapter();
                    case 3: return CreateBelongingsChapter();
                    case 4: return CreateUncleChapter();
                    // Arrests are now scheduled by the native Eleanor scheduler.
                    case 5: return VanillaEleanorBridge.RescuePending ? null : CreateWarningChapter();
                    case 6: return CreateHardshipChapter();
                    case 7: return CreateScavengerChapter();
                    case 8: return CreateScavengerSecondChapter();
                    case 9: return CreateGiftChapter();
                    case 10: return CreateLydiaChapter();
                    case 11: return CreateLydiaResponseChapter();
                    case 12: return MaidStory.CreateEntry();
                    case 13: return CreateDriveChapter();
                    case 14: return CreateThreatChapter();
                    case 15: return CreateDecisionChapter();
                    case 16: return CreatePreparationChapter();
                    case 17: return CreateReunionChapter();
                    case 18: return CreateMotherChapter();
                    case 19: return CreateAfterReunionChapter();
                    case 20: return CreateConfessionChapter();
                    case 21: return CreateCooperationChapter();
                    case 22: return CreateLydiaSecondChapter();
                    case 23: return CreateFutureChapter();
                    case 24: return CreateConclusionChapter();
                    case 201: return CreateRouteD();
                    default:
                        StoryDirector.LogError("Unknown chapter " + chapter);
                        return null;
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Failed to build chapter " + chapter + ": " + ex);
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Shared client construction
        // ------------------------------------------------------------------

        internal static StoreClient NewEleanorClient(string id, string sprite, StoreClient.ClientIntent intent, bool upper = true)
        {
            StoreClient client = new StoreClient
            {
                identifier = Prefix + id,
                displayName = L.Eleanor,
                spriteName = PortraitCompat.Resolve(string.IsNullOrEmpty(sprite) ? "eleanor1" : sprite),
                clientIntent = intent,
                nonShootable = true,
                arrestable = false,
                reportDegradesFaction = false,
                dismissable = false,
                staysAfterFailedBargain = true
            };

            if (upper) StoreClientFactionSetup.InitUpperLevelCitizen(client);
            else StoreClientFactionSetup.InitRev(client);

            client.acceptDealDialogue = StoryEngine.Text(L.Eleanor, L.Get("er_line_trade_accept"));
            AddTrapWatcher(client);
            return client;
        }

        internal static StoreClient NewNpcClient(string id, string name, string sprite, FactionKind faction)
        {
            StoreClient client = new StoreClient
            {
                identifier = Prefix + id,
                displayName = name,
                spriteName = PortraitCompat.Resolve(sprite),
                clientIntent = StoreClient.ClientIntent.DIALOGUE,
                nonShootable = true,
                arrestable = false,
                reportDegradesFaction = false,
                dismissable = false,
                staysAfterFailedBargain = true
            };

            switch (faction)
            {
                case FactionKind.BlackMarket: StoreClientFactionSetup.InitBlackMarket(client); break;
                case FactionKind.Security: StoreClientFactionSetup.InitSecurity(client); break;
                case FactionKind.Lower: StoreClientFactionSetup.InitLowerLevelCitizen(client); break;
                case FactionKind.Rev: StoreClientFactionSetup.InitRev(client); break;
                default: StoreClientFactionSetup.InitUpperLevelCitizen(client); break;
            }

            client.acceptDealDialogue = StoryEngine.Text(name, L.Get("er_line_trade_accept"));
            return client;
        }

        internal enum FactionKind { Upper, BlackMarket, Security, Lower, Rev }

        internal static void Advance(int expected, int next, Action flags = null)
        {
            flags?.Invoke();
            StoryDirector.AdvanceChapter(expected, next);
        }

        private static void FinishWhenLeaving(StoreClient client, int expected, int next, Func<Dialogue> wrap, Action flags = null)
        {
            StoryEngine.InterceptDismiss(client, wrap, () => Advance(expected, next, flags));
        }

        private static string Prompt(string key)
        {
            return L.Get(key);
        }

        // ------------------------------------------------------------------
        // 1. Food
        // ------------------------------------------------------------------

        private static StoreClient CreateFoodChapter()
        {
            StoreClient client = NewEleanorClient("ch1", "eleanor1", StoreClient.ClientIntent.BUY);
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(300);
            client.SetVariety();

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemBought = (item, paid) => tracker.Record(item, paid, eleanorIsSeller: false);

            Dialogue root = StoryEngine.Build(StoryV06Content.B_C01_N001);
            Dialogue askMother = StoryEngine.Build(StoryV06Content.B_C01_N002);
            Dialogue goods = StoryEngine.Build(StoryV06Content.B_C01_N003);
            StoryEngine.Tail(askMother).SetNextDialogue(goods);
            StoryEngine.AddChoices(root, L.Get("er_op55_c01_prompt"),
                new StoryEngine.Choice(L.Get("er_op55_c01_opt1"), "", askMother, null),
                new StoryEngine.Choice(L.Get("er_op55_c01_opt2"), "", goods, null));
            client.mainDialogue = root;

            FinishWhenLeaving(client, 1, 2, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C01_B003,
                    StoryV06Content.B_C01_B004,
                    StoryV06Content.B_C01_B005,
                    StoryV06Content.B_C01_B005,
                    StoryV06Content.B_C01_B006);
                StoryEngine.Append(dialogue, tracker.Any
                    ? StoryV06Content.B_C01_N004
                    : StoryV06Content.B_C01_N005);
                return Head(dialogue);
            }, () => RecordDeal(tracker, noSaleIsVisit: true));

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 2. Household goods
        // ------------------------------------------------------------------

        private static StoreClient CreateHouseholdChapter()
        {
            StoreClient client = NewEleanorClient("ch2", "eleanor1", StoreClient.ClientIntent.BUY);
            client.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            client.SetBudget(220);
            client.SetVariety();

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemBought = (item, paid) => tracker.Record(item, paid, eleanorIsSeller: false);
            client.mainDialogue = StoryEngine.Build(StoryV06Content.B_C02_N001);

            FinishWhenLeaving(client, 2, 3, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C02_B003,
                    StoryV06Content.B_C02_B004,
                    StoryV06Content.B_C02_B005,
                    StoryV06Content.B_C02_B005,
                    StoryV06Content.B_C02_B006);
                if (tracker.Any) StoryEngine.Append(dialogue, StoryV06Content.B_C02_N002);
                return Head(dialogue);
            }, () => RecordDeal(tracker, noSaleIsVisit: true));

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 3. Selling her belongings (native purchase flow)
        // ------------------------------------------------------------------

        private static StoreClient CreateBelongingsChapter()
        {
            StoreClient client = NewEleanorClient("ch3", "eleanor2", StoreClient.ClientIntent.SELL);
            client.SetWholesale(30);

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) => tracker.Record(item, paid, eleanorIsSeller: true);

            Dialogue main = StoryEngine.Build(StoryV06Content.B_C03_N001);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.BelongingsStockSpawned) return;
                RomanceState.BelongingsStockSpawned = true;
                SpawnSecondHand("bottled_water_premium");
                SpawnSecondHand("box_tampon");
                SpawnSecondHand("neuroactive_perfume");
            });
            client.mainDialogue = main;

            FinishWhenLeaving(client, 3, 4, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C03_B003,
                    StoryV06Content.B_C03_B004,
                    StoryV06Content.B_C03_B005,
                    StoryV06Content.B_C03_B005,
                    StoryV06Content.B_C03_B006);
                StoryEngine.Append(dialogue, StoryV06Content.B_C03_N002);
                Dialogue keepScale = StoryEngine.Build(StoryV06Content.B_C03_N003);
                Dialogue quotePrice = StoryEngine.Build(StoryV06Content.B_C03_N004);
                Dialogue shared = StoryEngine.Build(StoryV06Content.B_C03_N005);
                StoryEngine.Tail(keepScale).SetNextDialogue(shared);
                StoryEngine.Tail(quotePrice).SetNextDialogue(shared);
                StoryEngine.AddChoices(dialogue, L.Get("er_op55_c03_prompt"),
                    new StoryEngine.Choice(L.Get("er_op55_c03_opt1"), "", keepScale, null),
                    new StoryEngine.Choice(L.Get("er_op55_c03_opt2"), "", quotePrice, null));
                return Head(dialogue);
            }, () => RecordDeal(tracker, noSaleIsVisit: true));

            client.CompleteClientCreation();
            return client;
        }

        private static bool CrateInControl()
        {
            try
            {
                if (EmporiumEntry.Instance == null) return false;
                foreach (GameItem item in EmporiumEntry.Instance.GetAllItems())
                {
                    if (item != null
                        && item.identifier == "luxury_supply_crate"
                        && StolenHelper.IsStolenItem(item))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("CrateInControl failed: " + ex);
            }
            return false;
        }

        private static void SpawnSecondHand(string itemId)
        {
            try
            {
                GameItem item = ItemFeatureHelper.SetSecondHand(DirectoryMaster.Item(itemId));
                PlayerStore.Instance.AddDirectSellingItemToTableMiddle(item, isOwend: false);
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("SpawnSecondHand failed for " + itemId + ": " + ex);
            }
        }

        // ------------------------------------------------------------------
        // 4. Fake uncle and the stolen crate
        // ------------------------------------------------------------------

        private static StoreClient CreateUncleChapter()
        {
            if (RomanceState.UncleAccepted || RomanceState.UncleReported || RomanceState.CrateSold)
            {
                return CreateUncleAftermath();
            }

            StoreClient client = NewNpcClient("ch4", L.Get("er_name_uncle"), "maleBM5", FactionKind.BlackMarket);
            client.clientIntent = StoreClient.ClientIntent.SELL;

            GameItem crate = null;
            bool resolved = false;

            Action acceptCrate = () =>
            {
                if (crate != null) return;
                try
                {
                    crate = DirectoryMaster.Item("luxury_supply_crate");
                    crate.AddItemFeature(ItemFeatureList.FreeFeature());
                    StolenHelper.InitStolenItem(crate, 50);
                    PlayerStore.Instance.AddDirectSellingItemToTableMiddle(crate);
                    client.arrestable = true;
                    RomanceState.UncleAccepted = true;
                    RomanceState.CrateKept = true;
                    RomanceState.UncleInspected = true;
                }
                catch (Exception ex)
                {
                    StoryDirector.LogError("Could not create fake-uncle crate: " + ex);
                }
            };

            Action complete = () =>
            {
                if (resolved) return;
                resolved = true;
                Advance(4, 5);
            };

            client.onReportAction = () =>
            {
                try
                {
                    if (crate != null && crate.parentInventory != null && GeneralHelper.IsItemOwned(crate))
                    {
                        if (EmporiumEntry.Instance.frontInvinvElement.childItems.Contains(crate)
                            || EmporiumEntry.Instance.backInvinvElement.childItems.Contains(crate))
                        {
                            GeneralHelper.SetItemOwned(crate, own: false);
                        }
                        else
                        {
                            PlayerStore.Instance.AddDirectSellingItemToTableMiddle(crate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    StoryDirector.LogError("Uncle report handling failed: " + ex);
                }
            };

            client.onCaptureAction = () =>
            {
                RomanceState.UncleReported = true;
                RomanceState.UncleAccepted = false;
                RomanceState.CrateKept = false;
                complete();
            };

            client.customOnArrestDialogue = BuildArrestDialogue();
            client.OnDismiss = complete;

            Dialogue root = StoryEngine.Build(StoryV06Content.B_C04_N001);
            Dialogue keepBox = StoryEngine.Build(StoryV06Content.B_C04_N003);
            Dialogue register = StoryEngine.BuildSkipFirst(StoryV06Content.B_C04_N002);
            Dialogue refuseBox = StoryEngine.Build(StoryV06Content.B_C04_N004);
            StoryEngine.Tail(keepBox).SetEndAction(() => client.dismissable = true);
            StoryEngine.Tail(register).SetEndAction(() => client.dismissable = true);
            StoryEngine.Tail(refuseBox).SetEndAction(() =>
            {
                complete();
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, Prompt("er_prompt_uncle"),
                new StoryEngine.Choice(L.Get("er_op55_c04_opt1"), L.Get("er_choice_accept_crate_desc"),
                    keepBox, () => { acceptCrate(); client.dismissable = true; }),
                new StoryEngine.Choice(L.Get("er_op55_c04_opt2"), L.Get("er_choice_accept_inspect_desc"),
                    register, () => { acceptCrate(); client.dismissable = true; }),
                new StoryEngine.Choice(L.Get("er_op55_c04_opt3"), L.Get("er_choice_refuse_crate_desc"),
                    refuseBox, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateUncleAftermath()
        {
            StoreClient client = NewNpcClient("ch4_after", L.Get("er_name_narrator"),
                "maleLower1", FactionKind.Lower);
            Dialogue root = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_uncle_done"));
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                Advance(4, 5);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static Dialogue BuildArrestDialogue()
        {
            Dialogue dialogue = StoryEngine.Build(StoryV06Content.B_C04_B003);
            StoryEngine.Append(dialogue, StoryV06Content.B_C04_N005);
            StoryEngine.Tail(dialogue).SetEndAction(() =>
            {
                try { StoreUIManager.Instance.OnArrestDialoguePlayed(); }
                catch (Exception ex) { StoryDirector.LogError("OnArrestDialoguePlayed failed: " + ex); }
            });
            return dialogue;
        }

        private static Dialogue BuildCrateArrestDialogue()
        {
            Dialogue dialogue = StoryEngine.Build(StoryV06Content.B_C05_B007);
            StoryEngine.Tail(dialogue).SetEndAction(() =>
            {
                try { StoreUIManager.Instance.OnArrestDialoguePlayed(); }
                catch (Exception ex) { StoryDirector.LogError("OnArrestDialoguePlayed failed: " + ex); }
            });
            return dialogue;
        }

        // ------------------------------------------------------------------
        // 5a. Warning response
        // ------------------------------------------------------------------

        private static StoreClient CreateWarningChapter()
        {
            StoreClient client = NewEleanorClient("ch5w", "eleanor2", StoreClient.ClientIntent.DIALOGUE);
            if (RomanceState.UncleAccepted && !CrateInControl())
            {
                RomanceState.CrateSold = true;
                RomanceState.UncleAccepted = false;
            }
            Dialogue root = StoryEngine.Text(L.Player, L.Get("er_line_warning_prompt"));

            Dialogue quiet = StoryEngine.Text(L.Player, L.Get("er_line_keep_quiet"));
            StoryEngine.Tail(quiet).SetEndAction(() =>
            {
                Advance(5, 6);
                StoryEngine.Dismiss();
            });

            Dialogue tell;
            bool knowsTrap = RomanceState.UncleReported || RomanceState.UncleAccepted || RomanceState.UncleInspected;
            bool giftAvailable = knowsTrap && !RomanceState.WarningGiftGiven;

            if (RomanceState.UncleReported)
            {
                tell = StoryEngine.Chain(StoryV06Content.B_C04_N006,
                    giftAvailable ? StoryV06Content.B_C04_N007 : StoryV06Content.B_C04_N008);
            }
            else if (knowsTrap)
            {
                tell = StoryEngine.Chain(StoryV06Content.B_C04_B004,
                    StoryV06Content.B_C04_B006,
                    StoryV06Content.B_C04_B008);
            }
            else
            {
                tell = StoryEngine.Chain(StoryV06Content.B_C04_B004,
                    StoryV06Content.B_C04_B007,
                    StoryV06Content.B_C04_B009);
            }

            if (giftAvailable)
            {
                string departureBlock = RomanceState.UncleReported
                    ? StoryV06Content.B_C04_N008
                    : StoryV06Content.B_C04_B009;
                Action finishGift = () =>
                {
                    if (RomanceState.WarningGiftGiven) Advance(5, 6);
                    StoryEngine.Dismiss();
                };
                Dialogue purifierGift = StoryEngine.Build(departureBlock);
                StoryEngine.Tail(purifierGift).SetEndAction(finishGift);
                Dialogue perfumeGift = StoryEngine.Build(departureBlock);
                StoryEngine.Tail(perfumeGift).SetEndAction(finishGift);
                Dialogue noGift = StoryEngine.Build(departureBlock);
                StoryEngine.Tail(noGift).SetEndAction(() =>
                {
                    Advance(5, 6);
                    StoryEngine.Dismiss();
                });
                StoryEngine.AddChoices(tell, Prompt("er_prompt_gift"),
                    new StoryEngine.Choice(L.Get("er_choice_gift_purifier"), L.Get("er_choice_gift_purifier_desc"),
                        purifierGift, () => GiveWarningGift(0)),
                    new StoryEngine.Choice(L.Get("er_choice_gift_perfume"), L.Get("er_choice_gift_perfume_desc"),
                        perfumeGift, () => GiveWarningGift(1)),
                    new StoryEngine.Choice(L.Get("er_choice_no_gift"), L.Get("er_choice_no_gift_desc"),
                        noGift, null));
            }
            else
            {
                StoryEngine.Tail(tell).SetEndAction(() =>
                {
                    Advance(5, 6);
                    StoryEngine.Dismiss();
                });
            }

            StoryEngine.AddChoices(root, Prompt("er_prompt_warning"),
                new StoryEngine.Choice(L.Get("er_choice_tell_crate"), L.Get("er_choice_tell_crate_desc"),
                    tell, () =>
                    {
                        RomanceState.UncleWarned = true;
                        try { StoreClientUniqueList.ClearEleanorCrateHeat(); } catch { }
                    }),
                new StoryEngine.Choice(L.Get("er_choice_keep_quiet"), L.Get("er_choice_keep_quiet_desc"),
                    quiet, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static void GiveWarningGift(int option)
        {
            if (RomanceState.WarningGiftGiven) return;
            if (!VanillaEleanorBridge.GiveWarningGift(option))
            {
                StoryDirector.Notify(L.Get("er_notice_gift_pending"));
                return;
            }

            RomanceState.WarningGiftGiven = true;
            StoryDirector.Notify(L.Get("er_notice_gift_received"));
        }

        // ------------------------------------------------------------------
        // 6. Hardship / food aid
        // ------------------------------------------------------------------

        private static StoreClient CreateHardshipChapter()
        {
            StoreClient client = NewEleanorClient("ch6", "eleanor3", StoreClient.ClientIntent.BUY);
            client.clientBuyingTagList.Add("FOOD");
            client.clientBuyingTagList.Add("TREAT");
            client.clientBuyingTagList.Add("BREVAGE");
            client.SetBudget(40);
            client.tradeReputationMultiplier = 2.0; // vanilla Eleanor4 parity

            client.onItemBought = (item, paid) =>
            {
                long market = StoryEngine.MarketValue(item, 1);
                if (paid <= Math.Max(1L, market / 3)) RomanceState.HardshipAidGiven = true;
            };

            Dialogue root = StoryEngine.Build(StoryV06Content.B_C05_N001);
            Dialogue explain = StoryEngine.Build(StoryV06Content.B_C05_N003);
            Dialogue aidNode = StoryEngine.Build(StoryV06Content.B_C05_N005);
            Dialogue noFood = StoryEngine.Build(StoryV06Content.B_C05_N004);
            StoryEngine.Tail(explain).SetNextDialogue(aidNode);
            StoryEngine.Tail(noFood).SetEndAction(() =>
            {
                RomanceState.NoSaleVisits++;
                Advance(6, 7);
                StoryEngine.Dismiss();
            });
            StoryEngine.AddChoices(root, L.Get("er_op55_c05_prompt"),
                new StoryEngine.Choice(L.Get("er_op55_c05_opt1"), "", aidNode, null),
                new StoryEngine.Choice(L.Get("er_op55_c05_opt2"), "", explain, null),
                new StoryEngine.Choice(L.Get("er_op55_c05_opt3"), "", noFood,
                    () => client.CompleteBusiness()));
            client.mainDialogue = root;
            VanillaEleanorBridge.AttachCashAid(client, aidNode);

            FinishWhenLeaving(client, 6, 7, () =>
            {
                Dialogue dialogue;
                if (RomanceState.HardshipCashGiven)
                {
                    dialogue = StoryEngine.Text(L.Player, L.Get("er_line_cash_aid_1"));
                    dialogue.NextDialogue().SetText(L.Eleanor, L.Get("er_line_cash_aid_2"))
                        .NextDialogue().SetText(L.Eleanor, L.Get("er_line_cash_aid_3"))
                        .NextDialogue().SetText(L.Player, L.Get("er_line_cash_aid_4"))
                        .NextDialogue().SetText(L.Eleanor, L.Get("er_line_cash_aid_5"));
                }
                else if (RomanceState.HardshipAidGiven)
                {
                    dialogue = StoryEngine.Build(StoryV06Content.B_C05_N002);
                }
                else
                {
                    dialogue = StoryEngine.Chain(StoryV06Content.B_C05_B004, StoryV06Content.B_C05_B006);
                }
                return Head(dialogue);
            }, () =>
            {
                if (RomanceState.HardshipAidGiven)
                {
                    RomanceState.RecordDeal('D');
                    RomanceState.AddAffection(2);
                }
                else RomanceState.NoSaleVisits++;
            });

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 7-8. Scavenging and the 800-credit investigator fund
        // ------------------------------------------------------------------

        private static StoreClient CreateScavengerChapter()
        {
            StoreClient client = NewEleanorClient("ch7", "eleanor_scav", StoreClient.ClientIntent.SELL);
            client.SetWholesale(30);
            client.staysAfterFailedBargain = true;

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) =>
            {
                tracker.Record(item, paid, eleanorIsSeller: true);
                RomanceState.Savings = RomanceState.Savings + Mathf.Max(0, paid);
                NotifySavingsProgress(paid);
            };

            Dialogue main = StoryEngine.Build(StoryV06Content.B_C06_B101);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.ScavStockSpawned1) return;
                RomanceState.ScavStockSpawned1 = true;
                StoreClientList.AddScavGeneralStock();
            });
            client.mainDialogue = main;

            FinishWhenLeaving(client, 7, 8, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C06_B003,
                    StoryV06Content.B_C06_B004,
                    StoryV06Content.B_C06_B004,
                    StoryV06Content.B_C06_B005,
                    StoryV06Content.B_C06_B006);
                StoryEngine.Append(dialogue, tracker.Any
                    ? StoryV06Content.B_C06_B106
                    : StoryV06Content.B_C06_B107);
                return Head(dialogue);
            }, () =>
            {
                RecordDeal(tracker, noSaleIsVisit: true);
                if (RomanceState.Savings >= 800) RomanceState.SavingsAutoFilled = false;
            });

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateScavengerSecondChapter()
        {
            StoreClient client = NewEleanorClient("ch8", "eleanor_scav", StoreClient.ClientIntent.SELL);
            client.SetWholesale(30);
            client.staysAfterFailedBargain = true;

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) =>
            {
                tracker.Record(item, paid, eleanorIsSeller: true);
                RomanceState.Savings = RomanceState.Savings + Mathf.Max(0, paid);
                NotifySavingsProgress(paid);
            };

            Dialogue main = StoryEngine.Build(StoryV06Content.B_C07_B101);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.ScavStockSpawned2) return;
                RomanceState.ScavStockSpawned2 = true;
                StoreClientList.AddScavGeneralStock();
            });
            client.mainDialogue = main;

            FinishWhenLeaving(client, 8, 9, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C07_B006,
                    StoryV06Content.B_C06_B004,
                    StoryV06Content.B_C06_B004,
                    StoryV06Content.B_C07_B007,
                    StoryV06Content.B_C06_B006);
                StoryEngine.Append(dialogue, StoryV06Content.B_C07_B106);
                return Head(dialogue);
            }, () => RecordDeal(tracker, noSaleIsVisit: true));

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 9. First return gift
        // ------------------------------------------------------------------

        private static StoreClient CreateGiftChapter()
        {
            StoreClient client = NewEleanorClient("ch9", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C08_B101);

            string extraBlock = StoryEngine.PositiveDeals >= 3
                ? StoryV06Content.B_C08_B005
                : StoryV06Content.B_C08_B006;

            Dialogue accept = StoryEngine.BuildSkipFirst(StoryV06Content.B_C08_B102);
            StoryEngine.Append(accept, StoryV06Content.B_C08_B105);
            StoryEngine.Append(accept, extraBlock);

            Dialogue pay = StoryEngine.Build(StoryV06Content.B_C08_B103);
            StoryEngine.Append(pay, StoryV06Content.B_C08_B105);
            StoryEngine.Append(pay, extraBlock);

            Dialogue refuse = StoryEngine.Build(StoryV06Content.B_C08_B104);
            StoryEngine.Append(refuse, StoryV06Content.B_C08_B106);
            StoryEngine.Append(refuse, extraBlock);

            foreach (Dialogue branch in new[] { accept, pay, refuse })
            {
                StoryEngine.Tail(branch).SetEndAction(() =>
                {
                    Advance(9, 10);
                    StoryEngine.Dismiss();
                });
            }

            if (RomanceState.ReturnGiftChoice != 0)
            {
                Dialogue resume;
                if (RomanceState.ReturnGiftChoice == 1)
                {
                    resume = StoryEngine.Build(StoryV06Content.B_C08_B102);
                    StoryEngine.Append(resume, StoryV06Content.B_C08_B105);
                }
                else if (RomanceState.ReturnGiftChoice == 2)
                {
                    resume = StoryEngine.Build(StoryV06Content.B_C08_B103);
                    StoryEngine.Append(resume, StoryV06Content.B_C08_B105);
                }
                else
                {
                    resume = StoryEngine.Build(StoryV06Content.B_C08_B104);
                    StoryEngine.Append(resume, StoryV06Content.B_C08_B106);
                }
                StoryEngine.Append(resume, extraBlock);
                StoryEngine.Tail(resume).SetEndAction(() =>
                {
                    Advance(9, 10);
                    StoryEngine.Dismiss();
                });
                client.mainDialogue = resume;
            }
            else
            {
                Dialogue menu = StoryEngine.Text(L.Player, L.Get("er_op55a2_c08_prompt"));
                StoryEngine.Tail(root).SetNextDialogue(menu);
                Dialogue retry = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_return_gift_retry"));
                retry.SetNextDialogue(menu);
                void AddGiftOption(string labelKey, string descKey, Dialogue success, bool paid)
                {
                    Dialogue response = StoryEngine.Text(L.Player, L.Get(labelKey));
                    response.SetNextDialogue(success);
                    menu.AddChoice(DirectoryMaster.Item("yes"), L.Get(labelKey), L.Get(descKey), response,
                        () => response.SetNextDialogue(TryReceiveReturnGift(paid) ? success : retry),
                        paid ? (Func<bool>)(() => StoryEngine.HasCash(20)) : null);
                }
                AddGiftOption("er_op55a2_c08_opt1", "er_choice_accept_gift_desc", accept, false);
                AddGiftOption("er_op55a2_c08_opt2", "er_choice_pay_gift_desc", pay, true);
                Dialogue refuseReply = StoryEngine.PlayerReply(L.Get("er_op55a2_c08_opt3"), refuse);
                menu.AddChoice(DirectoryMaster.Item("yes"), L.Get("er_op55a2_c08_opt3"),
                    L.Get("er_choice_refuse_gift_desc"), refuseReply, () =>
                    {
                        if (RomanceState.ReturnGiftChoice == 0) RomanceState.ReturnGiftChoice = 3;
                    }, null);
                client.mainDialogue = root;
            }
            client.CompleteClientCreation();
            return client;
        }

        private static bool TryReceiveReturnGift(bool paid)
        {
            int choice = paid ? 2 : 1;
            if (RomanceState.ReturnGiftChoice != 0) return RomanceState.ReturnGiftChoice == choice;
            // Reserve the price first, then refund immediately if placement fails.
            // A failed selection retains the menu and does not advance the chapter.
            if (paid && !StoryEngine.Spend(20)) return false;
            if (!StoryEngine.GiveToPlayer("common_electronic"))
            {
                if (paid) StoryEngine.AddCredit(20);
                return false;
            }
            RomanceState.ReturnGiftChoice = choice;
            return true;
        }

        // ------------------------------------------------------------------
        // 10-11. Lydia and Eleanor's answer
        // ------------------------------------------------------------------

        private static StoreClient CreateLydiaChapter()
        {
            StoreClient client = NewNpcClient("lydia1", L.Get("er_name_lydia"), LydiaSpriteKey(), FactionKind.Upper);
            client.clientIntent = StoreClient.ClientIntent.SELL;
            client.SetWholesale(45);

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) =>
            {
                tracker.Record(item, paid, eleanorIsSeller: true);
                long market = StoryEngine.MarketValue(item, 1);
                if ((long)paid * 10 >= market * 9) RomanceState.LydiaFairBracelet = true;
            };

            Action spawnBracelet = () =>
            {
                if (RomanceState.LydiaBraceletSpawned) return;
                RomanceState.LydiaBraceletSpawned = true;
                try
                {
                    GameItem bracelet = Items.SpawnBracelet();
                    if (bracelet != null)
                        PlayerStore.Instance.AddDirectSellingItemToTableMiddle(bracelet, isOwend: false);
                }
                catch (Exception ex)
                {
                    StoryDirector.LogError("Lydia bracelet spawn failed: " + ex);
                }
            };

            Dialogue main = StoryEngine.Build(StoryV06Content.B_C09_B101);
            DualText.Append(main, "common09_add_added");
            StoryEngine.Append(main, StoryV06Content.B_C09_B102);
            StoryEngine.Tail(main).SetEndAction(spawnBracelet);
            client.mainDialogue = main;

            FinishWhenLeaving(client, 10, 11, () =>
            {
                Dialogue dialogue;
                if (!tracker.Any) dialogue = StoryEngine.Build(StoryV06Content.B_C09_B004);
                else if (RomanceState.LydiaFairBracelet) dialogue = StoryEngine.Build(StoryV06Content.B_C09_B002);
                else dialogue = StoryEngine.Build(StoryV06Content.B_C09_B003);
                if (RomanceState.LydiaFairBracelet) DualText.Append(dialogue, "common09_add_high_price");
                StoryEngine.Append(dialogue, StoryV06Content.B_C09_B106);
                DualText.Append(dialogue, "common09_add_after");
                StoryEngine.Append(dialogue, StoryV06Content.B_C09_B107);
                return Head(dialogue);
            }, () => RomanceState.LydiaVisited = true);

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateLydiaResponseChapter()
        {
            StoreClient client = NewEleanorClient("ch11", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C10_B101);

            Dialogue told = StoryEngine.Build(StoryV06Content.B_C10_B102);
            Dialogue boxFirst = StoryEngine.Build(StoryV06Content.B_C10_B103);
            Dialogue soldOnly = StoryEngine.Build(StoryV06Content.B_C10_B104);
            foreach (Dialogue branch in new[] { told, boxFirst, soldOnly })
            {
                StoryEngine.Append(branch, StoryV06Content.B_C10_B105);
                DualText.Append(branch, "common10_add_family");
                StoryEngine.Append(branch, StoryV06Content.B_C10_B106);
                StoryEngine.Tail(branch).SetEndAction(() =>
                {
                    Advance(11, 12);
                    StoryEngine.Dismiss();
                });
            }

            StoryEngine.AddChoices(root, L.Get("er_op55a2_c10_prompt"),
                new StoryEngine.Choice(L.Get("er_op55a2_c10_opt1"), "", told, null),
                new StoryEngine.Choice(L.Get("er_op55a2_c10_opt2"), "", boxFirst, null),
                new StoryEngine.Choice(L.Get("er_op55a2_c10_opt3"), "", soldOnly, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 12. The 800 credits
        // ------------------------------------------------------------------

        private static StoreClient CreateDriveChapter()
        {
            StoreClient client = NewEleanorClient("ch13", "eleanor2", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Chain(StoryV06Content.B_C12_B001, StoryV06Content.B_C12_B002);

            Dialogue keepByPlayer = StoryEngine.Build(StoryV06Content.B_C12_B003);
            StoryEngine.Tail(keepByPlayer).SetEndAction(() =>
            {
                RomanceState.DriveState = 1;
                Advance(13, 14);
                StoryEngine.Dismiss();
            });

            Dialogue keepByHer = StoryEngine.Build(StoryV06Content.B_C12_B004);
            StoryEngine.Tail(keepByHer).SetEndAction(() =>
            {
                RomanceState.DriveState = 2;
                Advance(13, 14);
                StoryEngine.Dismiss();
            });

            Dialogue exit = StoryEngine.Build(StoryV06Content.B_C12_B005);
            StoryEngine.Tail(exit).SetEndAction(() =>
            {
                RomanceState.DriveState = 7;
                RomanceState.Route = 4;
                RomanceState.BetrayalKind = 4;
                RomanceState.Romance = 4;
                Advance(13, 201);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, Prompt("er_prompt_drive_custody"),
                new StoryEngine.Choice(L.Get("er_choice_custody_player"), L.Get("er_choice_custody_player_desc"),
                    keepByPlayer, null),
                new StoryEngine.Choice(L.Get("er_choice_custody_eleanor"), L.Get("er_choice_custody_eleanor_desc"),
                    keepByHer, null),
                new StoryEngine.Choice(L.Get("er_choice_custody_exit"), L.Get("er_choice_custody_exit_desc"),
                    exit, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 14. Threat
        // ------------------------------------------------------------------

        private static StoreClient CreateThreatChapter()
        {
            StoreClient client = NewNpcClient("threat", L.Get("er_name_thug"), "maleBM5", FactionKind.BlackMarket);
            Dialogue root = RomanceState.UncleReported
                ? StoryEngine.Chain(StoryV06Content.B_C13_B001, StoryV06Content.B_C13_B002, StoryV06Content.B_C13_B004)
                : StoryEngine.Chain(StoryV06Content.B_C13_B003, StoryV06Content.B_C13_B004);

            Dialogue leave = StoryEngine.Build(StoryV06Content.B_C13_B006);
            StoryEngine.Tail(leave).SetEndAction(() =>
            {
                Advance(14, 15);
                StoryEngine.Dismiss();
            });

            Dialogue recorded = StoryEngine.Build(StoryV06Content.B_C13_B005);
            StoryEngine.Tail(recorded).SetEndAction(() =>
            {
                RomanceState.ThreatRecorded = true;
                Advance(14, 15);
                StoryEngine.Dismiss();
            });

            Dialogue betray = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_threat_drive_taken"));
            StoryEngine.Tail(betray).SetEndAction(() =>
            {
                RomanceState.DriveState = 6;
                RomanceState.Route = 4;
                RomanceState.BetrayalKind = 1;
                RomanceState.Romance = 4;
                StoryEngine.AddCredit(300);
                Advance(14, 201);
                StoryEngine.Dismiss();
            });

            Dialogue sell = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_threat_drive_sold"));
            StoryEngine.Tail(sell).SetEndAction(() =>
            {
                RomanceState.DriveState = 6;
                RomanceState.Route = 4;
                RomanceState.BetrayalKind = 2;
                RomanceState.Romance = 4;
                StoryEngine.AddCredit(600);
                Advance(14, 201);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, Prompt("er_prompt_threat"),
                new StoryEngine.Choice(L.Get("er_choice_threat_leave"), L.Get("er_choice_threat_leave_desc"),
                    leave, null),
                new StoryEngine.Choice(L.Get("er_choice_threat_report", 30), L.Get("er_choice_threat_report_desc"),
                    recorded, null,
                    () => (RomanceState.UncleReported || RomanceState.ThreatRecorded)
                        && StoryEngine.SecRep() >= 30),
                new StoryEngine.Choice(L.Get("er_choice_threat_upper", 30), L.Get("er_choice_threat_upper_desc"),
                    recorded, null,
                    () => StoryEngine.UlRep() >= 30 && RomanceState.DriveState != 0),
                new StoryEngine.Choice(L.Get("er_choice_threat_handover", 300), L.Get("er_choice_threat_handover_desc"),
                    betray, null,
                    () => RomanceState.DriveState == 1),
                new StoryEngine.Choice(L.Get("er_choice_threat_sell", 600), L.Get("er_choice_threat_sell_desc"),
                    sell, null,
                    () => RomanceState.DriveState == 1 && StoryEngine.BmRep() >= 20));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 15. Decision: rescue or publish
        // ------------------------------------------------------------------

        private static StoreClient CreateDecisionChapter()
        {
            StoreClient client = NewEleanorClient("ch15", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C14_B001);
            if (StoryEngine.CanPrepareProtection())
                StoryEngine.Append(root, StoryV06Content.B_C14_B002);
            else
                StoryEngine.Append(root, StoryV06Content.B_C14_B003);
            StoryEngine.Append(root, StoryV06Content.B_C14_B004);

            Dialogue routeA = StoryEngine.BuildSkipFirst(StoryV06Content.B_C14_B005);
            StoryEngine.Tail(routeA).SetEndAction(() =>
            {
                RomanceState.Route = 1;
                Advance(15, 16);
                StoryEngine.Dismiss();
            });

            Dialogue routeB = StoryEngine.BuildSkipFirst(StoryV06Content.B_C14_B006);
            StoryEngine.Tail(routeB).SetEndAction(() =>
            {
                RomanceState.Route = 2;
                Advance(15, 16);
                StoryEngine.Dismiss();
            });

            Dialogue routeC = StoryEngine.BuildSkipFirst(StoryV06Content.B_C14_B007);
            StoryEngine.Append(routeC, StoryV06Content.B_C14_B008);
            StoryEngine.Tail(routeC).SetEndAction(() =>
            {
                RomanceState.Route = 3;
                RomanceState.DriveState = 5;
                RomanceState.MotherState = 2;
                RomanceState.RevVersion = 1;
                StoryEngine.FreeNutrifruitForLeak();
                Advance(15, RevolutionStory.ChR01);
                StoryEngine.Dismiss();
            });

            Dialogue d3 = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_secret_leak"));
            StoryEngine.Tail(d3).SetEndAction(() =>
            {
                RomanceState.Route = 4;
                RomanceState.BetrayalKind = 3;
                RomanceState.DriveState = 5;
                RomanceState.Romance = 4;
                Advance(15, 201);
                StoryEngine.Dismiss();
            });

            Dialogue d4 = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_exit_story"));
            StoryEngine.Tail(d4).SetEndAction(() =>
            {
                RomanceState.Route = 4;
                RomanceState.BetrayalKind = 4;
                RomanceState.Romance = 4;
                Advance(15, 201);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, Prompt("er_prompt_direction"),
                new StoryEngine.Choice(L.Get("er_choice_route_a"), L.Get("er_choice_route_a_desc"),
                    routeA, null),
                new StoryEngine.Choice(L.Get("er_choice_route_b"), L.Get("er_choice_route_b_desc"),
                    routeB, null, () => StoryEngine.CanPrepareProtection()),
                new StoryEngine.Choice(L.Get("er_choice_route_c"), L.Get("er_choice_route_c_desc"),
                    routeC, null),
                new StoryEngine.Choice(L.Get("er_choice_secret_leak"), L.Get("er_choice_secret_leak_desc"),
                    d3, null, () => RomanceState.DriveState == 1),
                new StoryEngine.Choice(L.Get("er_choice_route_d"), L.Get("er_choice_route_d_desc"),
                    d4, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 16. Preparation / payment
        // ------------------------------------------------------------------

        private static StoreClient CreatePreparationChapter()
        {
            StoreClient client = NewEleanorClient("ch16", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);

            if (RomanceState.Route == 1)
            {
                Dialogue root = StoryEngine.Build(StoryV06Content.B_C15_B005);
                StoryEngine.Tail(root).SetEndAction(() =>
                {
                    Advance(16, 17);
                    StoryEngine.Dismiss();
                });
                client.mainDialogue = root;
                client.CompleteClientCreation();
                return client;
            }

            Dialogue intro = StoryEngine.Build(StoryV06Content.B_C15_B002);
            if (StoryEngine.PositiveDeals >= 3) StoryEngine.Append(intro, StoryV06Content.B_C15_B003);

            Dialogue approved = StoryEngine.Build(StoryV06Content.B_C15_B004);
            StoryEngine.Tail(approved).SetEndAction(() =>
            {
                RomanceState.ProtectionReady = true;
                Advance(16, 17);
                StoryEngine.Dismiss();
            });

            Dialogue fallback = StoryEngine.Build(StoryV06Content.B_C15_B006);
            StoryEngine.Append(fallback, StoryV06Content.B_C15_B005);
            StoryEngine.Tail(fallback).SetEndAction(() =>
            {
                RomanceState.Route = 1;
                RomanceState.ProtectionFailed = true;
                Advance(16, 17);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(intro, Prompt("er_prompt_protection"),
                new StoryEngine.Choice(L.Get("er_choice_protect_sec", 30, 300), L.Get("er_choice_protect_sec_desc"),
                    approved,
                    () => { if (StoryEngine.Spend(300)) RomanceState.ProtectionChannel = 1; },
                    () => StoryEngine.SecRep() >= 30
                        && (RomanceState.UncleReported || RomanceState.ThreatRecorded)
                        && StoryEngine.HasCash(300)),
                new StoryEngine.Choice(L.Get("er_choice_protect_ul", 30, 400), L.Get("er_choice_protect_ul_desc"),
                    approved,
                    () => { if (StoryEngine.Spend(400)) RomanceState.ProtectionChannel = 2; },
                    () => StoryEngine.UlRep() >= 30
                        && RomanceState.DriveState != 0
                        && StoryEngine.HasCash(400)),
                new StoryEngine.Choice(L.Get("er_choice_protect_case", 200), L.Get("er_choice_protect_case_desc"),
                    approved,
                    () => { if (StoryEngine.Spend(200)) RomanceState.ProtectionChannel = 3; },
                    () => RomanceState.UncleReported && StoryEngine.HasCash(200)),
                new StoryEngine.Choice(L.Get("er_choice_protect_fallback"), L.Get("er_choice_protect_fallback_desc"),
                    fallback, null));

            client.mainDialogue = intro;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 17. Reunion
        // ------------------------------------------------------------------

        private static StoreClient CreateReunionChapter()
        {
            StoreClient client = NewEleanorClient("ch17", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root;
            bool protectionRoute = RomanceState.Route == 2 && RomanceState.ProtectionReady;

            if (protectionRoute)
            {
                root = StoryEngine.Build(StoryV06Content.B_C16_B002);
                StoryEngine.Append(root, StoryV06Content.B_C16_B003);
                StoryEngine.Append(root, StoryV06Content.B_C16_B004);
                StoryEngine.Append(root, StoryV06Content.B_C16_B005);
                StoryEngine.Tail(root).SetEndAction(() =>
                {
                    RomanceState.MotherState = 1;
                    RomanceState.DriveState = 4;
                    StoryEngine.TryFreeNutrifruit();
                    Advance(17, 18);
                    StoryEngine.Dismiss();
                });
            }
            else
            {
                root = StoryEngine.Build(StoryV06Content.B_C16_B001);
                StoryEngine.Append(root, StoryV06Content.B_C16_B003);

                Action completeReturn = () =>
                {
                    RomanceState.Route = 1;
                    RomanceState.MotherState = 1;
                    RomanceState.DriveState = 3;
                    Advance(17, 18);
                    StoryEngine.Dismiss();
                };

                Dialogue acceptCompensation = StoryEngine.Text(
                    L.Get("er_name_narrator"), L.Get("er_line_comp_accept"));
                StoryEngine.Tail(acceptCompensation).SetEndAction(() =>
                {
                    StoryEngine.AddCredit(300);
                    completeReturn();
                });

                Dialogue familyCompensation = StoryEngine.Text(L.Eleanor, L.Get("er_line_comp_family"));
                StoryEngine.Tail(familyCompensation).SetEndAction(() =>
                {
                    RomanceState.AddAffection(1);
                    completeReturn();
                });

                Dialogue refuseCompensation = StoryEngine.Text(L.Eleanor, L.Get("er_line_comp_refuse"));
                StoryEngine.Tail(refuseCompensation).SetEndAction(() =>
                {
                    RomanceState.AddAffection(1);
                    completeReturn();
                });

                StoryEngine.AddChoices(root, Prompt("er_prompt_return_compensation"),
                    new StoryEngine.Choice(L.Get("er_choice_comp_accept"), L.Get("er_choice_comp_accept_desc"),
                        acceptCompensation, null),
                    new StoryEngine.Choice(L.Get("er_choice_comp_family"), L.Get("er_choice_comp_family_desc"),
                        familyCompensation, null),
                    new StoryEngine.Choice(L.Get("er_choice_comp_refuse"), L.Get("er_choice_comp_refuse_desc"),
                        refuseCompensation, null));
            }

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 18. Mother's visit
        // ------------------------------------------------------------------

        private static StoreClient CreateMotherChapter()
        {
            StoreClient client = NewNpcClient("mother", L.Get("er_name_mother"), MotherSpriteKey(), FactionKind.Upper);
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            client.SetBudget(220);
            client.SetVariety();

            Dialogue main = StoryEngine.Chain(StoryV06Content.B_C17_B001, StoryV06Content.B_C17_B002);
            client.mainDialogue = main;

            FinishWhenLeaving(client, 18, 19, () =>
            {
                Dialogue dialogue = StoryEngine.Build(RomanceState.Route == 2
                    ? StoryV06Content.B_C17_B004
                    : StoryV06Content.B_C17_B003);
                StoryEngine.Append(dialogue, StoryV06Content.B_C17_B005);
                return Head(dialogue);
            }, () => RomanceState.MotherVisited = true);

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 19. After the reunion
        // ------------------------------------------------------------------

        internal static StoreClient CreateAfterReunionChapter(Dialogue supplied = null, System.Func<Dialogue> wrap = null, System.Action complete = null)
        {
            StoreClient client = NewEleanorClient(complete==null?"ch19":"dual_dr03", "eleanor_scav", StoreClient.ClientIntent.SELL);
            client.SetWholesale(25);

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) => tracker.Record(item, paid, eleanorIsSeller: true);

            Dialogue main = supplied ?? StoryEngine.Chain(StoryV06Content.B_C18_B001, StoryV06Content.B_C18_B002);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.ScavStockSpawned19) return;
                RomanceState.ScavStockSpawned19 = true;
                StoreClientList.AddScavGeneralStock();
            });
            client.mainDialogue = main;

            Func<Dialogue> fallback = () =>
            {
                bool positive = StoryEngine.PositiveDeals >= 3 || RomanceState.Affection >= 10;
                Dialogue dialogue = StoryEngine.Build(positive
                    ? StoryV06Content.B_C18_B003
                    : StoryV06Content.B_C18_B004);
                if (positive)
                {
                    Dialogue keep = StoryEngine.Build(StoryV06Content.B_C18_B003A);
                    Dialogue always = StoryEngine.Build(StoryV06Content.B_C18_B003B);
                    Dialogue space = StoryEngine.Build(StoryV06Content.B_C18_B003C);
                    StoryEngine.AddChoices(dialogue, L.Get("er_prompt_c18"),
                        new StoryEngine.Choice(L.Get("er_choice_c18_keep"), "", keep, null),
                        new StoryEngine.Choice(L.Get("er_choice_c18_always"), "", always, null),
                        new StoryEngine.Choice(L.Get("er_choice_c18_space"), "", space, null));
                }
                return Head(dialogue);
            };
            StoryEngine.InterceptDismiss(client, wrap ?? fallback, () => { RecordDeal(tracker, noSaleIsVisit: true); StoryCoreFacts.CompleteReplacement(19); if (complete != null) complete(); else { RomanceState.PendingChapter=19; Advance(19,20); } });

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 20. Confession / friendship
        // ------------------------------------------------------------------

        private static StoreClient CreateConfessionChapter()
        {
            StoreClient client = NewEleanorClient("ch20", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = null;

            if (StoryEngine.NeedReconciliation())
            {
                root = StoryEngine.Build(StoryV06Content.B_C19_B001);
            }

            if (StoryEngine.CanConfess())
            {
                if (root == null) root = StoryEngine.Build(StoryV06Content.B_C19_B002);
                else StoryEngine.Append(root, StoryV06Content.B_C19_B002);

                Dialogue accept = StoryEngine.BuildSkipFirst(StoryV06Content.B_C19_B003);
                StoryEngine.Tail(accept).SetEndAction(() =>
                {
                    StoryCoreFacts.Confess(1);
                    Advance(20, 21);
                    StoryEngine.Dismiss();
                });

                Dialogue friends = StoryEngine.BuildSkipFirst(StoryV06Content.B_C19_B005);
                StoryEngine.Tail(friends).SetEndAction(() =>
                {
                    StoryCoreFacts.Confess(3);
                    Advance(20, 21);
                    StoryEngine.Dismiss();
                });

                StoryEngine.AddChoices(root, Prompt("er_prompt_confession"),
                    new StoryEngine.Choice(L.Get("er_choice_confess_yes"), L.Get("er_choice_confess_yes_desc"),
                        accept, null),
                    new StoryEngine.Choice(L.Get("er_choice_confess_friends"), L.Get("er_choice_confess_friends_desc"),
                        friends, null));
            }
            else
            {
                if (root == null) root = StoryEngine.Build(StoryV06Content.B_C19_B006);
                else StoryEngine.Append(root, StoryV06Content.B_C19_B006);
                StoryEngine.Tail(root).SetEndAction(() =>
                {
                    StoryCoreFacts.Confess(3);
                    Advance(20, 21);
                    StoryEngine.Dismiss();
                });
            }

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 21. Cooperation
        // ------------------------------------------------------------------

        internal static StoreClient CreateCooperationChapter(System.Action complete = null, string sprite = null)
        {
            // The common c21 visit happens before Eleanor's Upper Level job.
            // The dual coop scene runs after dr08, so the caller passes eleanor1.
            StoreClient client = NewEleanorClient(complete==null?"ch21":"dual_coop",
                string.IsNullOrEmpty(sprite) ? "eleanor_scav" : sprite,
                StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C20_B001);
            StoryEngine.Append(root, StoryV06Content.B_C20_B004);
            StoryEngine.Append(root, RomanceState.RomanceConfirmed
                ? StoryV06Content.B_C20_B005
                : StoryV06Content.B_C20_B006);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                StoryCoreFacts.ArrangeCooperation();
                if (complete != null) complete(); else Advance(21, 22);
                if (!DualState.Active) QueueCooperationBuyer();
                StoryEngine.Dismiss();
            });

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        internal static StoreClient CreateCooperationBuyer()
        {
            try
            {
                string buyerSprite = PortraitCompat.Resolve(SpriteDict.GetRandomUpperFemaleSpriteName());
                StoreClient buyer = new StoreClient
                {
                    identifier = Prefix + "coop_buyer",
                    displayName = L.Get("er_name_upper_buyer"),
                    spriteName = buyerSprite,
                    clientIntent = StoreClient.ClientIntent.BUY,
                    nonShootable = true,
                    arrestable = false,
                    reportDegradesFaction = false,
                    dismissable = true
                };
                StoreClientFactionSetup.InitUpperLevelCitizen(buyer);
                buyer.clientBuyingTagList.Add(TypeHelper.LUXURY_ITEM);
                buyer.clientBuyingTagList.Add(TypeHelper.ART);
                buyer.clientBuyingTagList.Add(TypeHelper.HIGHEND);
                buyer.SetBudget(600);
                int remaining=ModHelper.GetModDataInt(ModConstants.ModId,"er.core.coop.remaining",-1);
                if(remaining>=0)buyer.OverrideBudget(remaining);
                buyer.onItemBought+=(item,paid)=>ModHelper.SetModDataInt(ModConstants.ModId,"er.core.coop.remaining",buyer.GetBudget());
                buyer.SetVariety();
                buyer.mainDialogue = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_coop_buyer_intro"));
                buyer.AddBasicDialog();
                buyer.acceptDealDialogue = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_coop_buyer"));
                buyer.allDoneDialogue = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_coop_buyer"));
                StoryEngine.Tail(buyer.allDoneDialogue).SetEndAction(StoryEngine.Dismiss);
                StoryEngine.InterceptDismiss(buyer, () => StoryEngine.Text(L.Get("er_name_upper_buyer"),L.Get("er_line_coop_buyer")), StoryCoreFacts.CloseCooperation);
                buyer.CompleteClientCreation();
                ModHelper.SetClientSource(buyer, ModConstants.ModId);
                return buyer;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Cooperation buyer failed: " + ex);
                return null;
            }
        }
        private static void QueueCooperationBuyer()
        { var buyer=CreateCooperationBuyer(); if(buyer!=null)PlayerStore.Instance.storeClientManager.AddNextClient(buyer); }

        // ------------------------------------------------------------------
        // 22. Lydia's second visit
        // ------------------------------------------------------------------

        private static StoreClient CreateLydiaSecondChapter()
        {
            StoreClient client = NewNpcClient("lydia2", L.Get("er_name_lydia"), LydiaSpriteKey(), FactionKind.Upper);
            client.clientIntent = StoreClient.ClientIntent.BUY;
            client.clientBuyingTagList.Add(TypeHelper.LUXURY_ITEM);
            client.clientBuyingTagList.Add(TypeHelper.ART);
            client.clientBuyingTagList.Add(TypeHelper.HIGHEND);
            client.SetBudget(500);
            client.SetVariety();

            client.mainDialogue = StoryEngine.Build(StoryV06Content.B_C21_B001);

            FinishWhenLeaving(client, 22, 23, () =>
            {
                Dialogue dialogue = StoryEngine.Build(StoryV06Content.B_C21_B003);
                if (RomanceState.LydiaFairBracelet) StoryEngine.Append(dialogue, StoryV06Content.B_C21_B002);
                StoryEngine.Append(dialogue, RomanceState.RomanceConfirmed
                    ? StoryV06Content.B_C21_B004
                    : StoryV06Content.B_C21_B005);
                StoryEngine.Append(dialogue, StoryV06Content.B_C21_B006);
                return Head(dialogue);
            });

            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 23. Work and future
        // ------------------------------------------------------------------

        internal static StoreClient CreateFutureChapter()
        {
            StoreClient client = NewEleanorClient("ch23", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(RomanceState.Route == 2
                ? StoryV06Content.B_C22_B002
                : StoryV06Content.B_C22_B001);
            StoryEngine.Append(root, StoryV06Content.B_C22_B003);

            if (RomanceState.RomanceConfirmed)
            {
                StoryEngine.Append(root, StoryV06Content.B_C22_B004);
                StoryEngine.Tail(root).SetEndAction(() => FinishFuture(client, 23));
            }
            else if (RomanceState.Romance == 2)
            {
                Dialogue confirm = StoryEngine.BuildSkipFirst(StoryV06Content.B_C22_B005);
                StoryEngine.Append(confirm, StoryV06Content.B_C22_B004);
                Dialogue friends = StoryEngine.Build(StoryV06Content.B_C22_B006);

                StoryEngine.Tail(confirm).SetEndAction(() =>
                {
                    RomanceState.Romance = 1;
                    FinishFuture(client, 23);
                });
                StoryEngine.Tail(friends).SetEndAction(() =>
                {
                    RomanceState.Romance = 3;
                    FinishFuture(client, 23);
                });

                StoryEngine.AddChoices(root, Prompt("er_prompt_future"),
                    new StoryEngine.Choice(L.Get("er_choice_future_confirm"), L.Get("er_choice_future_confirm_desc"),
                        confirm, null),
                    new StoryEngine.Choice(L.Get("er_choice_future_friends"), L.Get("er_choice_future_friends_desc"),
                        friends, null));
            }
            else
            {
                StoryEngine.Append(root, StoryV06Content.B_C22_B006);
                StoryEngine.Tail(root).SetEndAction(() => FinishFuture(client, 23));
            }

            var orderLead=MaidBusiness.Say("order_lead");
            MaidBusiness.AppendOrderOffer(orderLead,root);
            client.mainDialogue = orderLead;
            client.CompleteClientCreation();
            return client;
        }

        internal static void FinishFuture(StoreClient client, int chapter, System.Action complete = null)
        {
            RomanceState.WorkState = RomanceState.Route == 2 ? 2 : 1;
            if (RomanceState.RomanceConfirmed && RomanceState.LocketWithEleanor && !RomanceState.LocketSold && !RomanceState.LocketGiven && !StoryCoreFacts.LocketRefused)
            {
                Dialogue locket = StoryEngine.Build(StoryV06Content.B_C22_B007);
                Dialogue accept = StoryEngine.Build(StoryV06Content.B_C22_B008);
                StoryEngine.Tail(accept).SetEndAction(() =>
                {
                    StoryCoreFacts.Record(23); if(complete!=null)complete(); else Advance(chapter, 24);
                    StoryEngine.Dismiss();
                });
                Dialogue refuse = StoryEngine.Build(StoryV06Content.B_C22_B009);
                StoryEngine.Tail(refuse).SetEndAction(() =>
                {
                    StoryCoreFacts.RefuseLocket();
                    StoryCoreFacts.Record(23); if(complete!=null)complete(); else Advance(chapter, 24);
                    StoryEngine.Dismiss();
                });
                MaidStory.GateOriginalLocket(locket,accept,refuse);
                DialogUIManager.Instance.StartDialogue(locket, forcedStart: true);
            }
            else if (RomanceState.RomanceConfirmed && RomanceState.LocketSold)
            {
                Dialogue gone = StoryEngine.Build(StoryV06Content.B_C22_B010);
                StoryEngine.Tail(gone).SetEndAction(() =>
                {
                    StoryCoreFacts.Record(23); if(complete!=null)complete(); else Advance(chapter, 24);
                    StoryEngine.Dismiss();
                });
                DialogUIManager.Instance.StartDialogue(gone, forcedStart: true);
            }
            else
            {
                StoryCoreFacts.Record(23); if(complete!=null)complete(); else Advance(chapter, 24);
                StoryEngine.Dismiss();
            }
        }

        // ------------------------------------------------------------------
        // 24. Core conclusion
        // ------------------------------------------------------------------

        internal static StoreClient CreateDeferredResolution()
        {
            var c=NewEleanorClient("resolve_deferred","eleanor_scav",StoreClient.ClientIntent.DIALOGUE);
            var root=DualText.Build("v05_resolve_open");
            var yes=StoryEngine.Text(L.Eleanor,L.Get("er_dual_resolution_ack"));
            var friends=StoryEngine.Text(L.Eleanor,L.Get("er_dual_resolution_ack"));
            DualText.Finish(yes,()=>StoryCoreFacts.Confess(1));
            DualText.Finish(friends,()=>StoryCoreFacts.Confess(3));
            StoryEngine.AddChoices(root,L.Get("er_prompt_future"),
                new StoryEngine.Choice(L.Get("er_choice_future_confirm"),L.Get("er_choice_future_confirm_desc"),yes,null,()=>StoryEngine.CanConfess()),
                new StoryEngine.Choice(L.Get("er_choice_future_friends"),L.Get("er_choice_future_friends_desc"),friends,null));
            c.mainDialogue=root;c.CompleteClientCreation();return c;
        }

        internal static StoreClient CreateConclusionChapter(Dialogue supplied = null, System.Action complete = null)
        {
            if (RomanceState.Romance == 2) return CreateDeferredResolution();
            bool lover = RomanceState.RomanceConfirmed;
            StoreClient client = NewEleanorClient(complete==null?"ch24":"dual_dr11", lover ? "eleanor1" : "eleanor_scav",
                StoreClient.ClientIntent.DIALOGUE);

            Dialogue root = supplied ?? StoryEngine.Chain(StoryV06Content.B_C23_B001, StoryV06Content.B_C23_B002);
            if(supplied==null)StoryEngine.Append(root, lover ? StoryV06Content.B_C23_B003 : StoryV06Content.B_C23_B004);
            Dialogue tail = StoryEngine.Tail(root);
            Dialogue gift = StoryEngine.Text(L.Eleanor, L.Get("er_line_end_perfume"));
            MaidStory.GateOriginalReward(root,gift);
            Dialogue beforeClosing = gift;
            if (lover)
            {
                beforeClosing = gift.NextDialogue()
                    .SetText(L.Eleanor, L.Get("er_line_scav_assist"));
            }
            bool receipt = MaidWorldEvents.AppendReceipt(beforeClosing);
            beforeClosing = StoryEngine.Tail(beforeClosing);
            if(receipt)beforeClosing.SetEndAction(MaidWorldEvents.SubmitReceipt);
            Dialogue closing = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_core_end"));
            beforeClosing.SetNextDialogue(closing);
            DualText.FinishAll(root, () =>
            {
                StoryCoreFacts.FinishCore();
                if(complete!=null)complete();
            });

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // Route D
        // ------------------------------------------------------------------

        private static StoreClient CreateRouteD()
        {
            StoreClient client = NewEleanorClient("rd", "eleanor3", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root;
            switch (RomanceState.BetrayalKind)
            {
                case 1:
                    root = StoryEngine.Build(StoryV06Content.B_RD_B001);
                    StoryEngine.Append(root, StoryV06Content.B_RD_B002);
                    RomanceState.MotherState = 3;
                    break;
                case 2:
                    root = StoryEngine.Build(StoryV06Content.B_RD_B003);
                    RomanceState.MotherState = 2;
                    break;
                case 3:
                    root = StoryEngine.Build(StoryV06Content.B_RD_B004);
                    RomanceState.MotherState = 2;
                    break;
                default:
                    root = StoryEngine.Build(StoryV06Content.B_RD_B005);
                    if (RomanceState.DriveState == 1) RomanceState.DriveState = 2;
                    break;
            }

            RomanceState.Route = 4;
            RomanceState.Romance = 4;
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                StoryDirector.CompleteCore();
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // Epilogue and old-save prompt
        // ------------------------------------------------------------------

        public static StoreClient CreateEpilogueClient(int index)
        {
            try
            {
                string block;
                if (RomanceState.RomanceConfirmed)
                {
                    switch (index)
                    {
                        case 0: block = StoryV06Content.B_EP_B001; break;
                        case 1: block = StoryV06Content.B_EP_B002; break;
                        case 2: block = StoryV06Content.B_EP_B003; break;
                        default: block = StoryV06Content.B_EP_B004; break;
                    }
                }
                else
                {
                    block = StoryV06Content.B_EP_B005;
                }

                bool friend = !RomanceState.RomanceConfirmed;
                StoreClient client = NewEleanorClient("ep" + index,
                    friend ? "eleanor_scav" : "eleanor1",
                    friend ? StoreClient.ClientIntent.SELL : StoreClient.ClientIntent.DIALOGUE);
                client.dismissable = true;
                // The first friend visit is the written scene; later visits use
                // a short trade greeting instead of replaying the same lines.
                Dialogue root = friend && index > 0
                    ? StoryEngine.Text(L.Eleanor, L.Get("er_ep_friend_trade"))
                    : StoryEngine.Build(block);
                if (friend)
                {
                    // The friend epilogue is a real goods visit: her line promises
                    // today's prices, and the friend ending says she occasionally
                    // brings scavenged goods. Stocking here (not only in dialogue)
                    // stops her from leaving without offering anything to sell.
                    client.SetWholesale(10);
                    root.SetStartAction(() =>
                    {
                        if (RomanceState.EpilogueStockIndex == index) return;
                        RomanceState.EpilogueStockIndex = index;
                        StoreClientList.AddScavGeneralStock();
                    });
                }
                else
                {
                    StoryEngine.Tail(root).SetEndAction(StoryEngine.Dismiss);
                }
                client.mainDialogue = root;
                client.CompleteClientCreation();
                return client;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Epilogue failed: " + ex);
                return null;
            }
        }

        public static StoreClient CreateOldSavePromptClient()
        {
            try
            {
                StoreClient client = NewNpcClient("old_save", L.Get("er_name_archivist"),
                    "maleLower1", FactionKind.Lower);

                Dialogue reset = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_old_prompt_reset_line"));
                reset.SetEndAction(() =>
                {
                    RomanceState.ManualResetOldSave();
                    StoryEngine.Dismiss();
                });

                Dialogue keep = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_old_prompt_keep_line"));
                keep.SetEndAction(() =>
                {
                    RomanceState.OldPromptDone = true;
                    StoryDirector.Notify(L.Get("er_notice_keep_vanilla"));
                    StoryEngine.Dismiss();
                });

                Dialogue root = StoryEngine.Text(L.Get("er_name_archivist"), L.Get("er_old_prompt_1"));
                root.SetNextDialogue(StoryEngine.Text(L.Get("er_name_archivist"), L.Get("er_old_prompt_2")));
                StoryEngine.AddChoices(root, L.Get("er_old_prompt_choice_prompt"),
                    new StoryEngine.Choice(L.Get("er_old_prompt_reset"), L.Get("er_old_prompt_reset_desc"), reset, null),
                    new StoryEngine.Choice(L.Get("er_old_prompt_keep"), L.Get("er_old_prompt_keep_desc"), keep, null));

                client.mainDialogue = root;
                client.CompleteClientCreation();
                return client;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Old-save prompt failed: " + ex);
                return null;
            }
        }

        // ------------------------------------------------------------------
        // Trap watcher
        // ------------------------------------------------------------------

        private static void AddTrapWatcher(StoreClient client)
        {
            bool armed = false;
            bool sprung = false;

            client.onItemAddedToCounters = item =>
            {
                if (sprung || armed || item == null) return;
                if (item.identifier != "luxury_supply_crate") return;
                if (!StolenHelper.IsStolenItem(item)) return;
                if (!GeneralHelper.IsItemOwned(item)) return;
                if (RomanceState.Chapter >= 13 || RomanceState.Route != 0) return;
                if (!RomanceState.UncleAccepted
                    || RomanceState.UncleReported
                    || RomanceState.UncleWarned
                    || RomanceState.CrateSold) return;

                armed = true;
                // StartCoroutine/Invoke on a game MonoBehaviour counts as the
                // retiring `threads` capability from 2026-12-10. ModHelper.Delay
                // runs on the loader and is cancelled when the mod is stopped.
                ModHelper.Delay(ModConstants.ModId, 0.3f, () =>
                {
                    bool isCurrent = PlayerStore.Instance.currentClientInstance != null
                        && PlayerStore.Instance.currentClientInstance.GetClientBlueprint() == client;
                    bool onCounter = EmporiumEntry.Instance.backInvinvElement.childItems.Contains(item)
                        || EmporiumEntry.Instance.frontInvinvElement.childItems.Contains(item);
                    if (!isCurrent || !onCounter
                        || !StolenHelper.IsStolenItem(item)
                        || RomanceState.UncleWarned)
                    {
                        armed = false;
                        return;
                    }

                    sprung = true;
                    GeneralHelper.SetItemOwned(item, own: false);
                    client.customOnArrestDialogue = BuildCrateArrestDialogue();

                    if (PlayerStore.Instance.ArrestCurrentClientForStolenGoods())
                    {
                        if (RomanceState.UncleInspected && !RomanceState.UncleWarned)
                        {
                            RomanceState.KnewTrapUnwarned = true;
                        }
                        RomanceState.TrapSprung = true;
                        VanillaEleanorBridge.RecordArrest(
                            RomanceState.PendingChapter > 0 ? RomanceState.PendingChapter : RomanceState.Chapter);
                    }
                    else
                    {
                        GeneralHelper.SetItemOwned(item, own: true);
                        client.customOnArrestDialogue = null;
                        armed = false;
                        sprung = false;
                    }
                });
            };
        }

        // ------------------------------------------------------------------
        // Utility
        // ------------------------------------------------------------------

        internal static string LydiaSpriteKey()
        {
            return PortraitCompat.ResolveLydia();
        }

        internal static string MotherSpriteKey()
        {
            return PortraitCompat.ResolveMother();
        }

        private static Dialogue Head(Dialogue dialogue)
        {
            return dialogue;
        }

        private static Dialogue ReactionBlock(char kind, string fair, string discount, string over, string low, string none)
        {
            switch (kind)
            {
                case 'F': return StoryEngine.Build(fair);
                case 'D': return StoryEngine.Build(discount);
                case 'O': return StoryEngine.Build(over);
                case 'L': return StoryEngine.Build(low);
                default: return StoryEngine.Build(none);
            }
        }

        private static void NotifySavingsProgress(int paid)
        {
            int gained = Mathf.Max(0, paid);
            if (gained <= 0) return;
            int current = RomanceState.Savings;
            if (current >= 800)
            {
                StoryDirector.Notify(L.Get("er_notice_savings_complete"));
            }
            else
            {
                StoryDirector.Notify(L.Get("er_notice_savings_progress", current, 800 - current));
            }
        }

        internal static void RecordDeal(StoryEngine.DealTracker tracker, bool noSaleIsVisit)
        {
            if (!tracker.Any)
            {
                if (noSaleIsVisit) RomanceState.NoSaleVisits++;
                return;
            }

            switch (tracker.Dominant)
            {
                case 'F':
                    RomanceState.RecordDeal('F');
                    RomanceState.AddAffection(1);
                    break;
                case 'D':
                    RomanceState.RecordDeal('D');
                    RomanceState.AddAffection(2);
                    break;
                case 'O':
                    RomanceState.RecordDeal('O');
                    RomanceState.AddAffection(-1);
                    break;
                case 'L':
                    RomanceState.RecordDeal('L');
                    RomanceState.AddAffection(-2);
                    break;
            }
        }
    }
}
