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
                switch (chapter)
                {
                    case 1: return CreateFoodChapter();
                    case 2: return CreateHouseholdChapter();
                    case 3: return CreateBelongingsChapter();
                    case 4: return CreateUncleChapter();
                    case 5: return RomanceState.Arrested && !RomanceState.Rescued
                        ? CreateRescueChapter()
                        : CreateWarningChapter();
                    case 6: return CreateHardshipChapter();
                    case 7: return CreateScavengerChapter();
                    case 8: return CreateScavengerSecondChapter();
                    case 9: return CreateGiftChapter();
                    case 10: return CreateLydiaChapter();
                    case 11: return CreateLydiaResponseChapter();
                    case 12: return CreateSavingsChapter();
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
                    case 101: return CreateRouteC1();
                    case 102: return CreateRouteC2();
                    case 103: return CreateRouteC3();
                    case 104: return CreateRouteC4();
                    case 105: return CreateRouteC5();
                    case 106: return CreateRouteC6();
                    case 107: return CreateRouteC7();
                    case 108: return CreateRouteC8();
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

        private static StoreClient NewEleanorClient(string id, string sprite, StoreClient.ClientIntent intent, bool upper = true)
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
                dismissable = false
            };

            if (upper) StoreClientFactionSetup.InitUpperLevelCitizen(client);
            else StoreClientFactionSetup.InitRev(client);

            client.acceptDealDialogue = StoryEngine.Text(L.Eleanor, L.Get("er_line_ellipsis"));
            client.allDoneDialogue = StoryEngine.Text(L.Eleanor, L.Get("er_line_ellipsis"));
            AddTrapWatcher(client);
            return client;
        }

        private static StoreClient NewNpcClient(string id, string name, string sprite, FactionKind faction)
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
                dismissable = false
            };

            switch (faction)
            {
                case FactionKind.BlackMarket: StoreClientFactionSetup.InitBlackMarket(client); break;
                case FactionKind.Security: StoreClientFactionSetup.InitSecurity(client); break;
                case FactionKind.Lower: StoreClientFactionSetup.InitLowerLevelCitizen(client); break;
                case FactionKind.Rev: StoreClientFactionSetup.InitRev(client); break;
                default: StoreClientFactionSetup.InitUpperLevelCitizen(client); break;
            }

            client.acceptDealDialogue = StoryEngine.Text(name, L.Get("er_line_ellipsis"));
            client.allDoneDialogue = StoryEngine.Text(name, L.Get("er_line_ellipsis"));
            return client;
        }

        private enum FactionKind { Upper, BlackMarket, Security, Lower, Rev }

        private static void Advance(int expected, int next, Action flags = null)
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
            client.mainDialogue = StoryEngine.Chain(StoryV06Content.B_C01_B001, StoryV06Content.B_C01_B002);

            FinishWhenLeaving(client, 1, 2, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C01_B003,
                    StoryV06Content.B_C01_B004,
                    StoryV06Content.B_C01_B005,
                    StoryV06Content.B_C01_B005,
                    StoryV06Content.B_C01_B006);
                StoryEngine.Append(dialogue, StoryV06Content.B_C01_B007);
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
            client.mainDialogue = StoryEngine.Chain(StoryV06Content.B_C02_B001, StoryV06Content.B_C02_B002);

            FinishWhenLeaving(client, 2, 3, () =>
            {
                Dialogue dialogue = ReactionBlock(tracker.Dominant,
                    StoryV06Content.B_C02_B003,
                    StoryV06Content.B_C02_B004,
                    StoryV06Content.B_C02_B005,
                    StoryV06Content.B_C02_B005,
                    StoryV06Content.B_C02_B006);
                StoryEngine.Append(dialogue, StoryV06Content.B_C02_B007);
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
            client.isBargainAvailable = false;

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) => tracker.Record(item, paid, eleanorIsSeller: true);

            Dialogue main = StoryEngine.Chain(StoryV06Content.B_C03_B001, StoryV06Content.B_C03_B002);
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
                StoryEngine.Append(dialogue, StoryV06Content.B_C03_B007);
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
            client.isBargainAvailable = false;

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

            Dialogue root = StoryEngine.Build(StoryV06Content.B_C04_B001);
            Dialogue accept = StoryEngine.Build(StoryV06Content.B_C04_B002);
            accept.SetEndAction(() => client.dismissable = true);

            StoryEngine.AddChoices(root, Prompt("er_prompt_uncle"),
                new StoryEngine.Choice(L.Get("er_choice_accept_crate"), L.Get("er_choice_accept_crate_desc"),
                    accept, () => { acceptCrate(); client.dismissable = true; }),
                new StoryEngine.Choice(L.Get("er_choice_accept_inspect"), L.Get("er_choice_accept_inspect_desc"),
                    accept, () => { acceptCrate(); client.dismissable = true; }),
                new StoryEngine.Choice(L.Get("er_choice_refuse_crate"), L.Get("er_choice_refuse_crate_desc"),
                    null, () => { complete(); StoryEngine.Dismiss(); }));

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
            Dialogue root = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_warning_prompt"));

            Dialogue quiet = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_keep_quiet"));
            StoryEngine.Tail(quiet).SetEndAction(() =>
            {
                Advance(5, 6);
                StoryEngine.Dismiss();
            });

            Dialogue tell;
            bool knowsTrap = RomanceState.UncleReported || RomanceState.UncleAccepted || RomanceState.UncleInspected;

            if (RomanceState.UncleReported)
            {
                tell = StoryEngine.Chain(StoryV06Content.B_C04_B004,
                    StoryV06Content.B_C04_B005,
                    StoryV06Content.B_C04_B008);
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
                StoryEngine.Tail(tell).SetEndAction(() =>
                {
                    Advance(5, 6);
                    StoryEngine.Dismiss();
                });
            }

            if (knowsTrap)
            {
                // She offers a small existing item as thanks.  Each branch ends
                // with her leaving line and only then commits the chapter.
                Dialogue takeGift = StoryEngine.Build(StoryV06Content.B_C04_B009);
                StoryEngine.Tail(takeGift).SetEndAction(() =>
                {
                    StoryEngine.GiveToPlayer("portable_water_purifier");
                    Advance(5, 6);
                    StoryEngine.Dismiss();
                });
                Dialogue noGift = StoryEngine.Build(StoryV06Content.B_C04_B009);
                StoryEngine.Tail(noGift).SetEndAction(() =>
                {
                    Advance(5, 6);
                    StoryEngine.Dismiss();
                });
                StoryEngine.AddChoices(tell, Prompt("er_prompt_gift"),
                    new StoryEngine.Choice(L.Get("er_choice_take_gift"), L.Get("er_choice_take_gift_desc"),
                        takeGift, null),
                    new StoryEngine.Choice(L.Get("er_choice_no_gift"), L.Get("er_choice_no_gift_desc"),
                        noGift, null));
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

        // ------------------------------------------------------------------
        // 5b. Arrest rescue
        // ------------------------------------------------------------------

        private static StoreClient CreateRescueChapter()
        {
            StoreClient client = NewNpcClient("rescue", L.Get("er_name_officer"), "maleSec1", FactionKind.Security);
            Dialogue root = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_rescue_intro"));

            Dialogue paid = StoryEngine.Build(StoryV06Content.B_C05_B008);
            if (RomanceState.UncleInspected && !RomanceState.UncleWarned)
            {
                StoryEngine.Append(paid, StoryV06Content.B_C05_B010);
            }
            else
            {
                StoryEngine.Append(paid, StoryV06Content.B_C05_B009);
            }
            StoryEngine.Append(paid, StoryV06Content.B_C05_B011);
            StoryEngine.Tail(paid).SetEndAction(() =>
            {
                RomanceState.Rescued = true;
                RomanceState.Arrested = false;
                int next = Mathf.Max(6, RomanceState.TrapResumeChapter);
                Advance(5, next);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, Prompt("er_prompt_rescue"),
                new StoryEngine.Choice(L.Get("er_choice_rescue_report", 300), L.Get("er_choice_rescue_report_desc"),
                    paid,
                    () =>
                    {
                        if (StoryEngine.Spend(300))
                        {
                            RomanceState.RescueMethod = 1;
                            RomanceState.ThreatRecorded = true;
                        }
                    },
                    () => RomanceState.UncleReported && StoryEngine.HasCash(300)),
                new StoryEngine.Choice(L.Get("er_choice_rescue_sec", 30, 300), L.Get("er_choice_rescue_sec_desc"),
                    paid,
                    () =>
                    {
                        if (StoryEngine.Spend(300))
                        {
                            RomanceState.RescueMethod = 1;
                            RomanceState.ThreatRecorded = true;
                        }
                    },
                    () => StoryEngine.SecRep() >= 30 && StoryEngine.HasCash(300)),
                new StoryEngine.Choice(L.Get("er_choice_rescue_bm", 30, 400), L.Get("er_choice_rescue_bm_desc"),
                    paid,
                    () =>
                    {
                        if (StoryEngine.Spend(400)) RomanceState.RescueMethod = 2;
                    },
                    () => StoryEngine.BmRep() >= 30 && StoryEngine.HasCash(400)),
                new StoryEngine.Choice(L.Get("er_choice_rescue_money", 1200), L.Get("er_choice_rescue_money_desc"),
                    paid,
                    () => { if (StoryEngine.Spend(1200)) RomanceState.RescueMethod = 3; },
                    () => StoryEngine.HasCash(1200)),
                new StoryEngine.Choice(L.Get("er_choice_rescue_ordinary", 120), L.Get("er_choice_rescue_ordinary_desc"),
                    paid,
                    () => { if (StoryEngine.Spend(120)) RomanceState.RescueMethod = 4; },
                    () => StoryEngine.HasCash(120)),
                new StoryEngine.Choice(L.Get("er_choice_rescue_later"), L.Get("er_choice_rescue_later_desc"),
                    null, () => StoryEngine.Dismiss()),
                new StoryEngine.Choice(L.Get("er_choice_rescue_stop"), L.Get("er_choice_rescue_stop_desc"),
                    StopRescue(), null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static Dialogue StopRescue()
        {
            Dialogue dialogue = StoryEngine.Text(L.Eleanor, L.Get("er_line_rescue_abandon"));
            StoryEngine.Tail(dialogue).SetEndAction(() =>
            {
                StoryDirector.AbandonStory();
                StoryEngine.Dismiss();
            });
            return dialogue;
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
            client.isBargainAvailable = false;

            bool foodGiven = false;
            client.onItemBought = (item, paid) =>
            {
                if (paid <= Mathf.Max(1, item.GetBaseValue()) / 3) foodGiven = true;
            };

            client.mainDialogue = StoryEngine.Build(StoryV06Content.B_C05_B001);

            FinishWhenLeaving(client, 6, 7, () =>
            {
                Dialogue dialogue;
                if (foodGiven)
                {
                    dialogue = StoryEngine.Chain(StoryV06Content.B_C05_B002, StoryV06Content.B_C05_B005);
                    RomanceState.RecordDeal('D');
                    RomanceState.AddAffection(2);
                }
                else
                {
                    dialogue = StoryEngine.Chain(StoryV06Content.B_C05_B004, StoryV06Content.B_C05_B006);
                    RomanceState.NoSaleVisits++;
                }
                return Head(dialogue);
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
            client.isBargainAvailable = false;

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) =>
            {
                tracker.Record(item, paid, eleanorIsSeller: true);
                RomanceState.Savings = RomanceState.Savings + Mathf.Max(0, paid);
            };

            Dialogue main = StoryEngine.Chain(StoryV06Content.B_C06_B001, StoryV06Content.B_C06_B002);
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
                    StoryV06Content.B_C06_B004, // player paid above value
                    StoryV06Content.B_C06_B004,
                    StoryV06Content.B_C06_B005, // player lowballed her
                    StoryV06Content.B_C06_B006);
                StoryEngine.Append(dialogue, StoryV06Content.B_C06_B007);
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
            client.isBargainAvailable = false;

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) =>
            {
                tracker.Record(item, paid, eleanorIsSeller: true);
                RomanceState.Savings = RomanceState.Savings + Mathf.Max(0, paid);
            };

            Dialogue main = StoryEngine.Chain(StoryV06Content.B_C07_B001, StoryV06Content.B_C07_B002);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.ScavStockSpawned2) return;
                RomanceState.ScavStockSpawned2 = true;
                StoreClientList.AddScavGeneralStock();
            });
            client.mainDialogue = main;

            FinishWhenLeaving(client, 8, 9, () =>
            {
                Dialogue dialogue = StoryEngine.Build(StoryEngine.LlRep() >= 20
                    ? StoryV06Content.B_C07_B003
                    : StoryV06Content.B_C07_B004);
                if (tracker.Any) StoryEngine.Append(dialogue, StoryV06Content.B_C07_B005);
                StoryEngine.Append(dialogue, RomanceState.Lowballs > 0
                    ? StoryV06Content.B_C07_B007
                    : StoryV06Content.B_C07_B006);
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
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C08_B001);

            Dialogue accept = StoryEngine.Build(StoryV06Content.B_C08_B002);
            Dialogue pay = StoryEngine.Build(StoryV06Content.B_C08_B003);
            Dialogue refuse = StoryEngine.Build(StoryV06Content.B_C08_B004);

            Action gift = () => StoryEngine.GiveToPlayer("scrap_metal");
            string extraBlock = StoryEngine.PositiveDeals >= 3
                ? StoryV06Content.B_C08_B005
                : StoryV06Content.B_C08_B006;

            foreach (Dialogue branch in new[] { accept, pay, refuse })
            {
                StoryEngine.Append(branch, extraBlock);
                StoryEngine.Tail(branch).SetEndAction(() =>
                {
                    Advance(9, 10);
                    StoryEngine.Dismiss();
                });
            }

            StoryEngine.AddChoices(root, Prompt("er_prompt_gift_return"),
                new StoryEngine.Choice(L.Get("er_choice_accept_gift"), L.Get("er_choice_accept_gift_desc"),
                    accept, gift),
                new StoryEngine.Choice(L.Get("er_choice_pay_gift"), L.Get("er_choice_pay_gift_desc"),
                    pay, () => { gift(); StoryEngine.Spend(20); }),
                new StoryEngine.Choice(L.Get("er_choice_refuse_gift"), L.Get("er_choice_refuse_gift_desc"),
                    refuse, null));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 10-11. Lydia and Eleanor's answer
        // ------------------------------------------------------------------

        private static StoreClient CreateLydiaChapter()
        {
            StoreClient client = NewNpcClient("lydia1", L.Get("er_name_lydia"), ModConstants.LydiaSprite, FactionKind.Upper);
            client.clientIntent = StoreClient.ClientIntent.SELL;
            client.SetWholesale(45);
            client.isBargainAvailable = false;

            StoryEngine.DealTracker tracker = new StoryEngine.DealTracker();
            client.onItemSold = (item, paid) =>
            {
                tracker.Record(item, paid, eleanorIsSeller: true);
                long baseValue = Math.Max(1, item.GetBaseValue());
                if (paid >= baseValue * 0.9) RomanceState.LydiaFairBracelet = true;
            };

            Dialogue main = StoryEngine.Build(StoryV06Content.B_C09_B001);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.LydiaBraceletSpawned) return;
                RomanceState.LydiaBraceletSpawned = true;
                try
                {
                    GameItem bracelet = Items.SpawnBracelet();
                    if (bracelet != null)
                    {
                        PlayerStore.Instance.AddDirectSellingItemToTableMiddle(bracelet, isOwend: false);
                    }
                }
                catch (Exception ex)
                {
                    StoryDirector.LogError("Lydia bracelet spawn failed: " + ex);
                }
            });
            client.mainDialogue = main;

            FinishWhenLeaving(client, 10, 11, () =>
            {
                Dialogue dialogue;
                if (!tracker.Any) dialogue = StoryEngine.Build(StoryV06Content.B_C09_B004);
                else if (RomanceState.LydiaFairBracelet) dialogue = StoryEngine.Build(StoryV06Content.B_C09_B002);
                else dialogue = StoryEngine.Build(StoryV06Content.B_C09_B003);
                StoryEngine.Append(dialogue, StoryV06Content.B_C09_B005);
                StoryEngine.Append(dialogue, StoryV06Content.B_C09_B006);
                return Head(dialogue);
            }, () => RomanceState.LydiaVisited = true);

            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateLydiaResponseChapter()
        {
            StoreClient client = NewEleanorClient("ch11", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C10_B001);

            string branch;
            bool hasMistake = RomanceState.Lowballs > 0 || RomanceState.Overcharges > 0;
            if (hasMistake && StoryEngine.PositiveDeals < 3)
                branch = StoryV06Content.B_C10_B004;
            else if ((RomanceState.KnewTrapUnwarned && RomanceState.Rescued) || hasMistake)
                branch = StoryV06Content.B_C10_B003;
            else
                branch = StoryV06Content.B_C10_B002;

            StoryEngine.Append(root, branch);
            StoryEngine.Append(root, StoryV06Content.B_C10_B005);
            if (StoryEngine.PositiveDeals >= 3 || RomanceState.Affection >= 8)
            {
                StoryEngine.Append(root, StoryV06Content.B_C10_B006);
            }

            StoryEngine.Tail(root).SetEndAction(() =>
            {
                Advance(11, 12);
                StoryEngine.Dismiss();
            });

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 12. The 800 credits
        // ------------------------------------------------------------------

        private static StoreClient CreateSavingsChapter()
        {
            StoreClient client = NewEleanorClient("ch12", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root;

            if (RomanceState.Savings < 800)
            {
                RomanceState.Savings = 800;
                RomanceState.SavingsAutoFilled = true;
                if (RomanceState.Discounts + RomanceState.FairDeals < 2)
                {
                    RomanceState.LocketSold = true;
                    RomanceState.LocketWithEleanor = false;
                }
                root = StoryEngine.Build(StoryV06Content.B_C11_B004);
                if (RomanceState.LocketSold) StoryEngine.Append(root, StoryV06Content.B_C11_B005);
            }
            else
            {
                root = StoryEngine.Build(StoryV06Content.B_C11_B001);
            }

            Dialogue completion = StoryEngine.Build(StoryV06Content.B_C11_B002);
            StoryEngine.Tail(completion).SetEndAction(() =>
            {
                Advance(12, 13);
                StoryEngine.Dismiss();
            });

            Dialogue exploitative = StoryEngine.Build(StoryV06Content.B_C11_B003);
            StoryEngine.Tail(exploitative).SetEndAction(() =>
            {
                RomanceState.Romance = 4;
                Advance(12, 13);
                StoryEngine.Dismiss();
            });

            StoryEngine.AddChoices(root, Prompt("er_prompt_savings"),
                new StoryEngine.Choice(L.Get("er_choice_savings_continue"), L.Get("er_choice_savings_continue_desc"),
                    completion, null),
                new StoryEngine.Choice(L.Get("er_choice_savings_control", 800), L.Get("er_choice_savings_control_desc"),
                    exploitative, null,
                    () => StoryEngine.HasCash(800)));

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 13. The drive and its custodian
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
                Advance(15, 101);
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
            }
            else
            {
                root = StoryEngine.Build(StoryV06Content.B_C16_B001);
                StoryEngine.Append(root, StoryV06Content.B_C16_B003);
            }

            StoryEngine.Tail(root).SetEndAction(() =>
            {
                if (protectionRoute)
                {
                    RomanceState.MotherState = 1;
                    RomanceState.DriveState = 4;
                    StoryEngine.TryFreeNutrifruit();
                }
                else
                {
                    RomanceState.Route = 1;
                    RomanceState.MotherState = 1;
                    RomanceState.DriveState = 3;
                }
                Advance(17, 18);
                StoryEngine.Dismiss();
            });

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // 18. Mother's visit
        // ------------------------------------------------------------------

        private static StoreClient CreateMotherChapter()
        {
            StoreClient client = NewNpcClient("mother", L.Get("er_name_mother"), ModConstants.MotherSprite, FactionKind.Upper);
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

        private static StoreClient CreateAfterReunionChapter()
        {
            StoreClient client = NewEleanorClient("ch19", "eleanor_scav", StoreClient.ClientIntent.SELL);
            client.SetWholesale(25);
            client.isBargainAvailable = false;

            Dialogue main = StoryEngine.Chain(StoryV06Content.B_C18_B001, StoryV06Content.B_C18_B002);
            StoryEngine.Tail(main).SetEndAction(() =>
            {
                if (RomanceState.ScavStockSpawned19) return;
                RomanceState.ScavStockSpawned19 = true;
                StoreClientList.AddScavGeneralStock();
            });
            client.mainDialogue = main;

            FinishWhenLeaving(client, 19, 20, () =>
            {
                Dialogue dialogue = StoryEngine.Build(
                    StoryEngine.PositiveDeals >= 3 || RomanceState.Affection >= 10
                        ? StoryV06Content.B_C18_B003
                        : StoryV06Content.B_C18_B004);
                return Head(dialogue);
            });

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
                    RomanceState.Romance = 1;
                    Advance(20, 21);
                    StoryEngine.Dismiss();
                });

                Dialogue slow = StoryEngine.BuildSkipFirst(StoryV06Content.B_C19_B004);
                StoryEngine.Tail(slow).SetEndAction(() =>
                {
                    RomanceState.Romance = 2;
                    Advance(20, 21);
                    StoryEngine.Dismiss();
                });

                Dialogue friends = StoryEngine.BuildSkipFirst(StoryV06Content.B_C19_B005);
                StoryEngine.Tail(friends).SetEndAction(() =>
                {
                    RomanceState.Romance = 3;
                    Advance(20, 21);
                    StoryEngine.Dismiss();
                });

                StoryEngine.AddChoices(root, Prompt("er_prompt_confession"),
                    new StoryEngine.Choice(L.Get("er_choice_confess_yes"), L.Get("er_choice_confess_yes_desc"),
                        accept, null),
                    new StoryEngine.Choice(L.Get("er_choice_confess_slow"), L.Get("er_choice_confess_slow_desc"),
                        slow, null),
                    new StoryEngine.Choice(L.Get("er_choice_confess_friends"), L.Get("er_choice_confess_friends_desc"),
                        friends, null));
            }
            else
            {
                if (root == null) root = StoryEngine.Build(StoryV06Content.B_C19_B006);
                else StoryEngine.Append(root, StoryV06Content.B_C19_B006);
                RomanceState.Romance = 3;
                StoryEngine.Tail(root).SetEndAction(() =>
                {
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

        private static StoreClient CreateCooperationChapter()
        {
            StoreClient client = NewEleanorClient("ch21", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_C20_B001);
            StoryEngine.Append(root, StoryV06Content.B_C20_B004);
            StoryEngine.Append(root, RomanceState.RomanceConfirmed
                ? StoryV06Content.B_C20_B005
                : StoryV06Content.B_C20_B006);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                Advance(21, 22);
                QueueCooperationBuyer();
                StoryEngine.Dismiss();
            });

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static void QueueCooperationBuyer()
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
                buyer.SetVariety();
                buyer.mainDialogue = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_coop_buyer_intro"));
                buyer.AddBasicDialog();
                buyer.acceptDealDialogue = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_coop_buyer"));
                buyer.allDoneDialogue = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_coop_buyer"));
                StoryEngine.Tail(buyer.allDoneDialogue).SetEndAction(StoryEngine.Dismiss);
                buyer.CompleteClientCreation();
                ModHelper.SetClientSource(buyer, ModConstants.ModId);
                PlayerStore.Instance.storeClientManager.AddNextClient(buyer);
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Cooperation buyer failed: " + ex);
            }
        }

        // ------------------------------------------------------------------
        // 22. Lydia's second visit
        // ------------------------------------------------------------------

        private static StoreClient CreateLydiaSecondChapter()
        {
            StoreClient client = NewNpcClient("lydia2", L.Get("er_name_lydia"), ModConstants.LydiaSprite, FactionKind.Upper);
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

        private static StoreClient CreateFutureChapter()
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

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static void FinishFuture(StoreClient client, int chapter)
        {
            RomanceState.WorkState = RomanceState.Route == 2 ? 2 : 1;
            if (RomanceState.RomanceConfirmed && RomanceState.LocketWithEleanor && !RomanceState.LocketSold)
            {
                Dialogue locket = StoryEngine.Build(StoryV06Content.B_C22_B007);
                Dialogue accept = StoryEngine.Build(StoryV06Content.B_C22_B008);
                StoryEngine.Tail(accept).SetEndAction(() =>
                {
                    Items.GiveLocket();
                    Advance(chapter, 24);
                    StoryEngine.Dismiss();
                });
                Dialogue refuse = StoryEngine.Build(StoryV06Content.B_C22_B009);
                StoryEngine.Tail(refuse).SetEndAction(() =>
                {
                    Advance(chapter, 24);
                    StoryEngine.Dismiss();
                });
                StoryEngine.AddChoices(locket, Prompt("er_prompt_locket"),
                    new StoryEngine.Choice(L.Get("er_choice_take_locket"), L.Get("er_choice_take_locket_desc"),
                        accept, null),
                    new StoryEngine.Choice(L.Get("er_choice_refuse_locket"), L.Get("er_choice_refuse_locket_desc"),
                        refuse, null));
                DialogUIManager.Instance.StartDialogue(locket, forcedStart: true);
            }
            else if (RomanceState.RomanceConfirmed && RomanceState.LocketSold)
            {
                Dialogue gone = StoryEngine.Build(StoryV06Content.B_C22_B010);
                StoryEngine.Tail(gone).SetEndAction(() =>
                {
                    Advance(chapter, 24);
                    StoryEngine.Dismiss();
                });
                DialogUIManager.Instance.StartDialogue(gone, forcedStart: true);
            }
            else
            {
                Advance(chapter, 24);
                StoryEngine.Dismiss();
            }
        }

        // ------------------------------------------------------------------
        // 24. Core conclusion
        // ------------------------------------------------------------------

        private static StoreClient CreateConclusionChapter()
        {
            if (RomanceState.Romance == 2) RomanceState.Romance = 3;
            bool lover = RomanceState.RomanceConfirmed;
            StoreClient client = NewEleanorClient("ch24", lover ? "eleanor1" : "eleanor_scav",
                StoreClient.ClientIntent.DIALOGUE);

            Dialogue root = StoryEngine.Chain(StoryV06Content.B_C23_B001, StoryV06Content.B_C23_B002);
            StoryEngine.Append(root, lover ? StoryV06Content.B_C23_B003 : StoryV06Content.B_C23_B004);
            Dialogue tail = StoryEngine.Tail(root);
            Dialogue closing = tail.NextDialogue().SetText(L.Get("er_name_narrator"), L.Get("er_line_core_end"));
            closing.SetEndAction(() =>
            {
                StoryDirector.CompleteCore();
                StoryEngine.Dismiss();
            });

            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        // ------------------------------------------------------------------
        // Route C
        // ------------------------------------------------------------------

        private static StoreClient CreateRouteC1()
        {
            StoreClient client = NewEleanorClient("rc1", "eleanor_scav", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_RC_B001);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                RomanceState.DriveState = 5;
                Advance(101, 102);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC2()
        {
            StoreClient client = NewEleanorClient("rc2", "eleanor3", StoreClient.ClientIntent.DIALOGUE);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_RC_B002);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                RomanceState.MotherState = 2;
                Advance(102, 103);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC3()
        {
            StoreClient client = NewEleanorClient("rc3", "eleanor3", StoreClient.ClientIntent.BUY);
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(40);
            client.isBargainAvailable = false;
            bool dealt = false;
            client.onItemBought = (item, paid) => { if (paid >= 0) dealt = true; };
            Dialogue main = StoryEngine.Build(StoryV06Content.B_RC_B003);
            client.mainDialogue = main;
            FinishWhenLeaving(client, 103, 104, () =>
            {
                Dialogue dialogue = dealt
                    ? StoryEngine.Build(StoryV06Content.B_RC_B004)
                    : StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_line_no_food"));
                return Head(dialogue);
            });
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC4()
        {
            StoreClient client = NewEleanorClient("rc4", "eleanor3", StoreClient.ClientIntent.DIALOGUE, upper: false);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_RC_B005);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                RomanceState.WorkState = 3;
                Advance(104, 105);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC5()
        {
            StoreClient client = NewEleanorClient("rc5", "eleanor3", StoreClient.ClientIntent.BUY, upper: false);
            client.clientBuyingTagList.Add("WEAPON");
            client.SetBudget(350);
            client.isBargainAvailable = false;
            bool dealt = false;
            client.onItemBought = (item, paid) => { if (paid >= 0) dealt = true; };
            Dialogue main = StoryEngine.Build(StoryV06Content.B_RC_B006);
            client.mainDialogue = main;
            FinishWhenLeaving(client, 105, 106, () =>
            {
                Dialogue dialogue = dealt
                    ? StoryEngine.Chain(StoryV06Content.B_RC_B007, StoryV06Content.B_RC_B009)
                    : StoryEngine.Chain(StoryV06Content.B_RC_B008, StoryV06Content.B_RC_B009);
                return Head(dialogue);
            });
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC6()
        {
            StoreClient client = NewNpcClient("rc6", L.Get("er_name_lydia"), ModConstants.LydiaSprite, FactionKind.Upper);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_RC_B010);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                Advance(106, 107);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC7()
        {
            StoreClient client = NewEleanorClient("rc7", "eleanor_rev", StoreClient.ClientIntent.DIALOGUE, upper: false);
            Dialogue root = StoryEngine.Build(StoryV06Content.B_RC_B011);
            StoryEngine.Append(root, RomanceState.RomanceConfirmed || RomanceState.Romance == 2
                ? StoryV06Content.B_RC_B012
                : StoryV06Content.B_RC_B013);
            StoryEngine.Tail(root).SetEndAction(() =>
            {
                Advance(107, 108);
                StoryEngine.Dismiss();
            });
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static StoreClient CreateRouteC8()
        {
            StoreClient client = NewEleanorClient("rc8", "eleanor_rev", StoreClient.ClientIntent.BUY, upper: false);
            client.clientBuyingTagList.Add("FOOD");
            client.SetBudget(120);
            client.isBargainAvailable = false;
            Dialogue main = StoryEngine.Build(StoryV06Content.B_RC_B014);
            client.mainDialogue = main;
            FinishWhenLeaving(client, 108, 300, () =>
            {
                Dialogue dialogue = StoryEngine.Build(StoryV06Content.B_RC_B015);
                return Head(dialogue);
            }, () => RomanceState.WorkState = 3);
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
                    RomanceState.MotherState = 3;
                    RomanceState.DriveState = 3;
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

                StoreClient client = NewEleanorClient("ep" + index,
                    RomanceState.RomanceConfirmed ? "eleanor1" : "eleanor_scav",
                    StoreClient.ClientIntent.DIALOGUE);
                client.dismissable = true;
                Dialogue root = StoryEngine.Build(block);
                StoryEngine.Tail(root).SetEndAction(StoryEngine.Dismiss);
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
                root.NextDialogue().SetText(L.Get("er_name_archivist"), L.Get("er_old_prompt_2"));
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
                DialogUIManager.Instance.StartCoroutine(MetaHelper.WaitAndDo(0.3f, () =>
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
                        RomanceState.Arrested = true;
                        RomanceState.TrapResumeChapter = Mathf.Max(6,
                            RomanceState.PendingChapter > 0 ? RomanceState.PendingChapter : RomanceState.Chapter);
                        RomanceState.PendingChapter = 0;
                        RomanceState.Chapter = 5;
                        RomanceState.NextVisitDay = StoreStation.Instance.dayCounter + 1;
                    }
                    else
                    {
                        GeneralHelper.SetItemOwned(item, own: true);
                        client.customOnArrestDialogue = null;
                        armed = false;
                        sprung = false;
                    }
                }));
            };
        }

        // ------------------------------------------------------------------
        // Utility
        // ------------------------------------------------------------------

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

        private static void RecordDeal(StoryEngine.DealTracker tracker, bool noSaleIsVisit)
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
