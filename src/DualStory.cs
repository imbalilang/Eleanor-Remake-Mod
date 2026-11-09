using System;

namespace EleanorRomance
{
    /// <summary>Scene builders for the Lydia dual-romance extension.</summary>
    public static class DualStory
    {
        public static StoreClient CreateScene(string id)
        {
            switch (id)
            {
                case "entry": return CreateEntry();
                case "dr03": return CreateDr03();
                case "dr04": return CreateDr04();
                case "dr05": return CreateDr05();
                case "dr06": return CreateDr06();
                case "dr07": return CreateDr07();
                case "dr08": return CreateDr08();
                case "dr09": return CreateDr09();
                case "dr10": return CreateDr10();
                case "dr11": return CreateDr11();
                case "dr13": return CreateDr13();
                case "dr14": return CreateDr14();
                case "dr15": return CreateDr15();
                case "dr16": return CreateDr16();
                case "dr17": return CreateDr17();
                case "dr18": return CreateDr18();
                case "dr19": return CreateDr19();
                case "dr20": return CreateDr20();
                case "dual_epilogue": return CreateDualEpilogue();
                case "daily": return CreateDaily();
                case "reputation": return CreateReputation();
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

        private static StoreClient Ready(StoreClient client, Dialogue root)
        {
            client.mainDialogue = root;
            client.CompleteClientCreation();
            return client;
        }

        private static void Finish(StoreClient client, Dialogue root, string scene, Action commit = null)
        {
            DualText.Finish(root, () =>
            {
                commit?.Invoke();
                DualState.SetSceneDay(scene, DualState.Today);
                DualDirector.CompleteScene(scene);
            });
        }

        private static StoreClient CreateEntry()
        {
            StoreClient client = Lydia("dual_entry");
            bool legacy = DualState.Entry == 2;

            // An interrupted first half resumes at the plaque conversation.
            if (!legacy && DualState.Entry == 1 && !DualState.Done("dr01"))
            {
                Dialogue resume = BuildEntrySecondHalf();
                Finish(client, resume, "entry");
                return Ready(client, resume);
            }

            Dialogue open = DualText.Build(legacy ? "legacy_entry_open" : "dr01_open",
                legacy ? "legacy_entry_notice" : "dr01_notice");
            if (open == null)
            {
                open = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_dual_entry_resume"));
            }

            if (legacy)
            {
                // Old lovers do not repeat the original confession. The
                // dedicated invitation leads straight into the extension.
                Dialogue legacyOpen = DualText.Build(
                    RomanceState.WorkState > 0 ? "dr_legacy_has_job" : "dr_legacy_no_job",
                    "dr_legacy_common");
                if (RomanceState.WorkState > 0) DualText.Append(legacyOpen, "dr_legacy_work_fact");
                if (legacyOpen == null) legacyOpen = open;
                Dialogue accepted = DualText.Build("dr_legacy_accept");
                Dialogue declined = DualText.Build("dr_legacy_decline");
                if (accepted == null) accepted = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr_legacy_accept_label"));
                if (declined == null) declined = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr_legacy_decline_label"));
                DualText.Choices(legacyOpen, "er_dual_prompt",
                    DualText.Choice("dr_legacy_accept", accepted),
                    DualText.Choice("dr_legacy_decline", declined, () => { DualState.Entry = 3; DualState.Relation = 3; }));
                Finish(client, legacyOpen, "entry", () => { if (DualState.Entry == 3) DualState.Relation = 3; });
                return Ready(client, legacyOpen);
            }

            Dialogue accept = DualText.Build("dr01_accept");
            Dialogue decline = DualText.Build("dr01_decline");
            Dialogue second = BuildEntrySecondHalf();
            StoryEngine.Tail(accept).SetNextDialogue(second);
            DualText.Choices(open, "er_dual_prompt",
                DualText.Choice("dr01_accept", accept, () => DualState.Accept(1)),
                DualText.Choice("dr01_decline", decline, () => DualState.Entry = 3));
            Finish(client, open, "entry", () =>
            {
                if (DualState.Entry == 3) DualState.Relation = 3;
            });
            return Ready(client, open);
        }

        private static StoreClient Eleanor(string id, string sprite = "eleanor1")
        {
            return EleanorStory.NewEleanorClient(id, sprite, StoreClient.ClientIntent.DIALOGUE);
        }

        private static StoreClient CreateDr03()
        {
            StoreClient client = Eleanor("dual_dr03");
            Dialogue root = DualText.Build("dr03_open",
                DualBusiness.PlaquePlaced() ? "dr03_plaque" : "dr03_no_plaque");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr03");
            return Ready(client, root);
        }

        private static StoreClient CreateDr04()
        {
            StoreClient client = Lydia("dual_dr04");
            string feedback = DualState.FirstTraded ? "dr04_feedback_success"
                : DualState.FirstBusiness == 4 ? "dr04_feedback_failed"
                : "dr04_feedback_none";
            Dialogue root = DualText.Build(feedback, "dr04_consult_open", "dr04_consult");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Action plaque = AppendPlaqueFollowup(root);
            Finish(client, root, "dr04", plaque);
            return Ready(client, root);
        }

        private static StoreClient CreateDr05()
        {
            if (!StoryEngine.CanConfess() && !RomanceState.RomanceConfirmed)
            {
                ModEntry.Log("Dual confession deferred: original confession conditions are not met.");
                return null;
            }
            StoreClient client = Eleanor("dual_dr05");
            Dialogue root = DualText.Build("dr05_open");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));

            Dialogue confess = DualText.Build("dr05_confess");
            Dialogue defer = DualText.Build("dr05_defer");
            Dialogue friend = DualText.Build("dr05_friend");
            if (confess == null) confess = StoryEngine.Text(L.Player, L.Get("er_dual_choice_dr05_confess_label"));
            if (defer == null) defer = StoryEngine.Text(L.Player, L.Get("er_dual_choice_dr05_defer_label"));
            if (friend == null) friend = StoryEngine.Text(L.Player, L.Get("er_dual_choice_dr05_friend_label"));

            DualText.Finish(confess, () =>
            {
                RomanceState.Romance = 1;
                DualState.Complete("dr05");
                DualState.LastImportantDay = DualState.Today;
            });
            DualText.Finish(defer, () => DualDirector.CompleteScene("dr05"));
            DualText.Finish(friend, () =>
            {
                RomanceState.Romance = 4;
                DualState.Relation = 3;
                DualState.CooldownUntil = DualState.Today + 7;
                DualDirector.CompleteScene("dr05");
            });
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr05_confess", confess),
                DualText.Choice("dr05_defer", defer),
                DualText.Choice("dr05_friend", friend));
            return Ready(client, root);
        }

        private static StoreClient CreateDr06()
        {
            StoreClient client = Eleanor("dual_dr06");
            Dialogue root = DualText.Build("dr06_open", "dr06_meal", "dr06_after");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr06");
            return Ready(client, root);
        }

        private static StoreClient CreateDr07()
        {
            StoreClient client = Lydia("dual_dr07");
            string result = DualState.FirstTraded ? "dr07_fair" : "dr07_none";
            Dialogue root = DualText.Build(result,
                RomanceState.RomanceConfirmed ? "dr07_confirmed" : "dr07_unconfirmed",
                "dr07_business");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));

            Dialogue book = DualText.Build("dr07_booked");
            Dialogue decline = DualText.Build("dr07_declined");
            if (book == null) book = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr07_book_label"));
            if (decline == null) decline = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr07_decline_label"));
            DualText.Finish(book, () =>
            {
                DualBusiness.ArrangeSecondBusiness(DualState.Today + 1);
                DualDirector.CompleteScene("dr07");
            });
            DualText.Finish(decline, () => DualDirector.CompleteScene("dr07"));
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr07_book", book),
                DualText.Choice("dr07_decline", decline));
            return Ready(client, root);
        }

        private static StoreClient CreateDr08()
        {
            StoreClient client = Eleanor("dual_dr08");
            string job = RomanceState.Route == 2 ? "dr08_job_b" : "dr08_job_a";
            string mother = MaidWorldEvents.Acquitted ? "dr08_mother_cleared" : "dr08_mother_pending";
            Dialogue root = DualText.Build(job, mother, "dr08_common");
            if (RomanceState.RomanceConfirmed) DualText.Append(root, "dr08_lover");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr08", () =>
            {
                RomanceState.WorkState = RomanceState.Route == 2 ? 2 : 1;
            });
            return Ready(client, root);
        }

        private static StoreClient CreateDr09()
        {
            StoreClient client = Lydia("dual_dr09");
            Dialogue root = DualText.Build("dr09_open",
                DualState.SecondTraded ? "dr09_business_success" : "dr09_business_none");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Action plaque = AppendPlaqueFollowup(root);
            Finish(client, root, "dr09", plaque);
            return Ready(client, root);
        }

        private static StoreClient CreateDr10()
        {
            StoreClient client = Lydia("dual_dr10");
            Dialogue root = DualText.Build("dr10_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Dialogue suit = DualText.Build("dr10_option1", "dr10_common");
            Dialogue eleanor = DualText.Build("dr10_option2", "dr10_common");
            if (suit == null) suit = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr10_suit_label"));
            if (eleanor == null) eleanor = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr10_eleanor_label"));
            DualText.Finish(suit, () => DualDirector.CompleteScene("dr10"));
            DualText.Finish(eleanor, () => DualDirector.CompleteScene("dr10"));
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr10_suit", suit),
                DualText.Choice("dr10_eleanor", eleanor));
            return Ready(client, root);
        }

        private static StoreClient CreateDr11()
        {
            StoreClient client = Eleanor("dual_dr11", "eleanor1");
            Dialogue root = DualText.Build("dr11_open");
            if (DualState.SecondTraded) DualText.Append(root, "dr11_add");
            DualText.Append(root, "dr12_open");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            DualText.Finish(root, () =>
            {
                StoryEngine.GiveSelfRefillingPerfume();
                DualState.CoreTakenOver = true;
                DualState.CoreCommitted = true;
                if (RomanceState.Chapter < RomanceState.ChapterComplete)
                    StoryDirector.CompleteCore();
                DualDirector.CompleteScene("dr11");
            });
            return Ready(client, root);
        }

        private static StoreClient CreateDr13()
        {
            StoreClient client = Lydia("dual_dr13");
            Dialogue root = DualText.Build("dr13_open", "dr13_door");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr13");
            return Ready(client, root);
        }

        private static StoreClient CreateDr14()
        {
            StoreClient client = Lydia("dual_dr14");
            Dialogue root = DualText.Build("dr14_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Dialogue accept = DualText.Build("dr14_accept");
            if (accept == null) accept = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr14_accept_label"));
            DualText.Finish(accept, () =>
            {
                DualState.Relation = 1;
                DualDirector.CompleteScene("dr14");
            });
            // Once the player has confirmed entry, both responses continue the
            // discussion; there is no repeated "leave the route" option here.
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr14_accept", accept),
                DualText.Choice("dr14_together", accept));
            return Ready(client, root);
        }

        private static StoreClient CreateDr15()
        {
            StoreClient client = Eleanor("dual_dr15", "eleanor1");
            Dialogue root = DualText.Build("dr15_open");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Dialogue wait = DualText.Build("dr15_wait");
            if (wait == null) wait = StoryEngine.Text(L.Eleanor, L.Get("er_dual_choice_dr15_wait_label"));
            DualText.Finish(wait, () =>
            {
                DualState.Relation = 1;
                DualDirector.CompleteScene("dr15");
            });
            // Both replies move forward; the only exit is the initial entry
            // choice and the ordinary-romance choice in dr05.
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr15_wait", wait),
                DualText.Choice("dr15_steady", wait));
            return Ready(client, root);
        }

        private static StoreClient CreateDr16()
        {
            StoreClient client = Lydia("dual_dr16");
            Dialogue root = DualText.Build("dr16_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr16", () => DualState.Relation = 1);
            return Ready(client, root);
        }

        private static StoreClient CreateDr17()
        {
            StoreClient client = Eleanor("dual_dr17", "eleanor1");
            Dialogue root = DualText.Build("dr17_open");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Dialogue confirm = DualText.Build("dr17_accept");
            if (confirm == null) confirm = StoryEngine.Text(L.Eleanor, L.Get("er_dual_choice_dr17_confirm_label"));
            DualText.Finish(confirm, () =>
            {
                // Only after both independent agreements: the choice click
                // alone must not create the relationship.
                DualState.Relation = 2;
                DualDirector.CompleteScene("dr17");
            });
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr17_confirm", confirm));
            return Ready(client, root);
        }

        private static StoreClient CreateDr18()
        {
            string sprite = PortraitCompat.ResolveLydiaDate();
            bool datePortrait = sprite == PortraitCompat.LydiaDateBeautyKey
                || sprite == PortraitCompat.LydiaDateVanillaKey;
            StoreClient client = EleanorStory.NewNpcClient("dual_dr18",
                L.Get("er_name_lydia"), sprite, EleanorStory.FactionKind.Upper);
            Dialogue root = DualText.Build("dr18_open", datePortrait ? "dr18_outfit" : "dr18_fallback");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Dialogue embrace = DualText.Build("dr18_embrace", "dr18_common");
            Dialogue talk = DualText.Build("dr18_talk", "dr18_common");
            if (embrace == null) embrace = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr18_embrace_label"));
            if (talk == null) talk = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr18_talk_label"));
            DualText.Finish(embrace, () => DualDirector.CompleteScene("dr18"));
            DualText.Finish(talk, () => DualDirector.CompleteScene("dr18"));
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr18_embrace", embrace),
                DualText.Choice("dr18_talk", talk));
            return Ready(client, root);
        }

        private static StoreClient CreateDr19()
        {
            StoreClient client = Eleanor("dual_dr19", "eleanor1");
            Dialogue root = DualText.Build("dr19_open");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr19");
            return Ready(client, root);
        }

        private static StoreClient CreateDr20()
        {
            StoreClient client = Eleanor("dual_dr20", "eleanor1");
            Dialogue root = DualText.Build("dr20_arrival", "dr20_gathering",
                DualBusiness.PlaquePlaced() ? "dr20_plaque" : "dr20_no_plaque",
                "dr20_closing");
            if (DualState.FirstTraded || DualState.SecondTraded) DualText.Append(root, "dr20_business_extra");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr20", () =>
            {
                DualState.LastDailyDay = DualState.Today;
                StoryDirector.Notify(L.Get("er_dual_notice_ending"));
            });
            return Ready(client, root);
        }

        private static StoreClient CreateDualEpilogue()
        {
            StoreClient client = Eleanor("dual_epilogue", "eleanor1");
            Dialogue root = DualText.Build("dr_epilogue_safe",
                RomanceState.Route == 2 ? "dr_epilogue_route_b" : "dr_epilogue_route_a",
                DualState.FirstTraded || DualState.SecondTraded ? "dr_epilogue_plaque_listed"
                    : DualState.PlaqueReceived ? "dr_epilogue_plaque_held"
                    : "dr_epilogue_plaque_none");
            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_notice_ending"));
            Finish(client, root, "dual_epilogue", () => DualState.LastDailyDay = DualState.Today);
            return Ready(client, root);
        }

        private static StoreClient CreateDaily()
        {
            int index = DualState.DailyIndex % 7 + 1;
            StoreClient client = index == 2 ? Lydia("dual_daily_" + index)
                : Eleanor("dual_daily_" + index, "eleanor1");
            Dialogue root = DualText.Build("dr_daily_" + index + "_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_dual_entry_resume"));
            DualText.Finish(root, () =>
            {
                DualState.DailyIndex = index % 7;
                DualState.LastDailyDay = DualState.Today;
                DualDirector.CompleteScene("daily");
            });
            return Ready(client, root);
        }

        private static Action AppendPlaqueFollowup(Dialogue root)
        {
            if (root == null) return null;
            if (DualState.PlaqueReceived && !DualBusiness.PlaqueExists())
                DualBusiness.MarkPlaqueLost();
            if (DualBusiness.CanReissuePlaque()
                || (!DualState.PlaqueReceived && DualState.Done("dr02") && DualState.PlaqueRetryDay >= 0))
            {
                DualText.Append(root, "dr_plaque_pickup");
                return () => DualBusiness.GivePlaque();
            }
            if (DualState.PlaqueReceived && !DualBusiness.PlaquePlaced())
            {
                DualText.Append(root, "dr_plaque_removed");
                return null;
            }
            if (DualState.PlaqueReceived && DualBusiness.PlaquePlaced()
                && !DualBusiness.HasFirstItem() && DualState.FirstBusiness == 0)
            {
                DualText.Append(root, "dr_plaque_no_stock");
            }
            return null;
        }

        private static StoreClient CreateReputation()
        {
            StoreClient client = Lydia("dual_reputation");
            Dialogue root = DualText.Build("dr_biz_reputation_open");
            if (!DualBusiness.PlaquePlaced()) DualText.Append(root, "dr_biz_reputation_removed");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_biz_feedback_notice"));
            DualText.Finish(root, () =>
            {
                DualState.FeedbackPending = false;
                DualState.FeedbackShown = true;
                DualState.CooldownUntil = DualState.Today + 2;
                DualDirector.CompleteScene("reputation");
            });
            return Ready(client, root);
        }

        private static Dialogue BuildEntrySecondHalf()
        {
            bool hasStock = DualBusiness.HasFirstItem();
            Dialogue plaque = DualText.Build("dr02_open", hasStock ? "dr02_stock" : "dr02_no_stock", "dr02_plaque");
            Dialogue take = DualText.Build("dr02_accept", "dr02_leave");
            Dialogue hold = DualText.Build("dr02_hold", "dr02_leave");
            if (take == null) take = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr02_accept_plaque_label"));
            if (hold == null) hold = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr02_hold_plaque_label"));
            DualText.Choices(plaque, "er_dual_prompt",
                DualText.Choice("dr02_accept_plaque", take, () =>
                {
                    if (!DualBusiness.GivePlaque())
                    {
                        StoryDirector.Notify(L.Get("er_dual_plaque_full"));
                        DualState.PlaqueRetryDay = DualState.Today + 1;
                    }
                }),
                DualText.Choice("dr02_hold_plaque", hold, () =>
                {
                    DualState.PlaqueRetryDay = DualState.Today + 1;
                }));
            return plaque;
        }
    }
}
