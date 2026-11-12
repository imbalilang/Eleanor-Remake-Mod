using System;

namespace EleanorRomance
{
    /// <summary>Scene builders for the Lydia dual-romance extension.</summary>
    public static class DualStory
    {
        public static StoreClient CreateScene(string id)
        {
            if(id.StartsWith("wait_",StringComparison.Ordinal))return DualDaily.Create(id);
            switch (id)
            {
                case "entry": return CreateEntry();
                case "dr02": return CreatePlaqueScene();
                case "resolve": return CreateDr05();
                case "coop": return EleanorStory.CreateCooperationChapter(()=>DualDirector.CompleteScene("coop"),"eleanor1");
                case "claim": return CreateClaim();
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
                case "dual_epilogue":
                    // The old standalone epilogue visit is retired; old saves
                    // resume at the regular daily visit instead.
                    DualState.Complete("dual_epilogue");
                    return CreateDaily();
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
                if (scene == "dr20") EndingPageUI.ShowDualCompletion();
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
                    DualState.SetSceneDay(scene, DualState.Today);
                    DualDirector.CompleteScene(scene);
                });
        }

        private static StoreClient CreateEntry()
        {
            var client=Lydia("dual_entry");
            if(DualState.Flag("entry.explicit") && DualState.HasHistory)
            {
                var resume=DualText.Build("dr01_accept");Finish(client,resume,"entry");return Ready(client,resume);
            }
            bool legacy=DualState.Flag("migration.consent_pending") && RomanceState.RomanceConfirmed;
            var open=DualText.Build(legacy?"v05_legacy_entry":"dr01_open");
            // Lydia's offer to act as the go-between was written but never
            // appended, so the entry choice arrived before the player heard
            // why she was helping. The legacy consent confirmation already
            // knows the history and skips this pitch.
            if(!legacy)DualText.Append(open,"dr01_notice");
            var accept=DualText.Build("dr01_accept");
            var decline=DualText.Build("dr01_decline");
            DualText.Choices(open,"er_dual_prompt",
                DualText.Choice("dr01_accept",accept,()=>
                {DualState.Accept(legacy?2:1);DualState.Flag("migration.consent_pending",false);}),
                DualText.Choice("dr01_decline",decline,()=>
                {DualState.Entry=3;DualState.Relation=3;DualState.Flag("migration.consent_pending",false);}));
            Finish(client,open,"entry");return Ready(client,open);
        }
        private static StoreClient CreatePlaqueScene()
        {
            var c=Lydia("dual_dr02");var root=DualText.Build("v05_reward50");
            Finish(c,root,"dr02",()=>{if(!DualBusiness.GivePlaque())StoryDirector.Notify(L.Get("er_dual_plaque_full"));});
            return Ready(c,root);
        }
        private static StoreClient CreateClaim()
        {
            var c=StoryCoreFacts.PerfumePending?Eleanor("dual_claim"):Lydia("dual_claim");var root=DualText.Build("v05_claim_open");DualRewards.AppendClaims(root);
            Finish(c,root,"claim",()=>DualState.LastDailyDay=DualState.Today);return Ready(c,root);
        }

        private static StoreClient Eleanor(string id, string sprite = "eleanor1")
        {
            return EleanorStory.NewEleanorClient(id, sprite, StoreClient.ClientIntent.DIALOGUE);
        }

        private static StoreClient CreateDr03()
        {
            return EleanorStory.CreateAfterReunionChapter(null,()=>DualText.Build("dr03_open",
                DualBusiness.PlaquePlaced()?"dr03_plaque":"dr03_no_plaque"),()=>DualDirector.CompleteScene("dr03"));
        }

        private static StoreClient CreateDr04()
        {
            var c=Lydia("dual_dr04");
            var root=DualText.Build("v05_dr04_feedback","dr04_consult_open","dr04_consult","dr04_lesson","dr04_shop");
            DualRewards.AppendClaims(root);
            FinishAfterTrade(c,"dr04");
            return Ready(c,root);
        }

        private static StoreClient CreateDr05()
        {
            var c=Eleanor("dual_dr05");
            var root=DualText.Build(RomanceState.Romance==2?"v05_resolve_open":"dr05_open");
            var friend=DualText.Build("dr05_friend");
            DualText.Finish(friend,()=>
            {StoryCoreFacts.Confess(3);StoryCoreFacts.CompleteReplacement(20);DualState.Relation=3;DualDirector.CompleteScene("dr05");});
            if(!StoryEngine.CanConfess() && !RomanceState.RomanceConfirmed)
            {
                root=StoryEngine.Build(StoryV06Content.B_C19_B006);
                DualText.Finish(root,()=>{StoryCoreFacts.Confess(3);StoryCoreFacts.CompleteReplacement(20);DualState.Relation=3;DualDirector.CompleteScene("dr05");});
            }
            else
            {
                var yes=DualText.Build("dr05_confess");
                DualText.Finish(yes,()=>{StoryCoreFacts.Confess(1);StoryCoreFacts.CompleteReplacement(20);DualDirector.CompleteScene("dr05");});
                DualText.Choices(root,"er_dual_prompt",DualText.Choice("dr05_confess",yes),DualText.Choice("dr05_friend",friend));
            }
            return Ready(c,root);
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
            var c=Lydia("dual_dr07");
            var root=DualText.Build("v05_dr07_open","dr07_confirmed");
            var warm=DualText.Build("dr07_reply_warm","dr07_shop");
            var practical=DualText.Build("dr07_reply_practical","dr07_shop");
            var guarded=DualText.Build("dr07_reply_guarded","dr07_shop");
            DualText.Choices(root,"er_dual_prompt",
                DualText.Choice("dr07_warm",warm),
                DualText.Choice("dr07_practical",practical),
                DualText.Choice("dr07_guarded",guarded));
            FinishAfterTrade(c,"dr07",()=>StoryCoreFacts.CompleteReplacement(22));
            return Ready(c,root);
        }

        private static StoreClient CreateDr08()
        {
            var c=Eleanor("dual_dr08");
            string job=RomanceState.WorkState>0?"v05_dr08_existing":RomanceState.Route==2?"dr08_job_b":"dr08_job_a";
            var root=RomanceState.Route==1
                ?DualText.Build(job,"dr08_common","dr08_lover")
                :DualText.Build(job,MaidWorldEvents.Acquitted?"dr08_mother_cleared":"dr08_mother_pending","dr08_common","dr08_lover");
            StoryEngine.Tail(root).SetEndAction(()=>EleanorStory.FinishFuture(c,23,()=>
            {StoryCoreFacts.CompleteReplacement(23);DualDirector.CompleteScene("dr08");}));
            return Ready(c,root);
        }

        private static StoreClient CreateDr09()
        {
            var c=Lydia("dual_dr09");
            var root=DualText.Build("v05_reward100","dr09_open");
            var compliment=DualText.Build("dr09_reply_compliment","dr09_shop");
            var work=DualText.Build("dr09_reply_work","dr09_shop");
            var practical=DualText.Build("dr09_reply_practical","dr09_shop");
            DualRewards.AppendClaims(compliment);
            DualRewards.AppendClaims(work);
            DualRewards.AppendClaims(practical);
            DualText.Choices(root,"er_dual_prompt",
                DualText.Choice("dr09_compliment",compliment),
                DualText.Choice("dr09_work",work),
                DualText.Choice("dr09_practical",practical));
            FinishAfterTrade(c,"dr09",()=>{if(DualProgression.Earned(2))DualRewards.Give(2);});
            return Ready(c,root);
        }

        private static StoreClient CreateDr10()
        {
            StoreClient client = Lydia("dual_dr10");
            Dialogue root = DualText.Build("dr10_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Dialogue suit = DualText.Build("dr10_option1", "dr10_common", "dr10_shop");
            Dialogue eleanor = DualText.Build("dr10_option2", "dr10_common", "dr10_shop");
            if (suit == null) suit = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr10_suit_label"));
            if (eleanor == null) eleanor = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_choice_dr10_eleanor_label"));
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr10_suit", suit),
                DualText.Choice("dr10_eleanor", eleanor));
            FinishAfterTrade(client, "dr10");
            return Ready(client, root);
        }

        private static StoreClient CreateDr11()
        {
            var root=DualText.Build("dr11_open","dr12_open");
            return EleanorStory.CreateConclusionChapter(root,()=>
            {DualState.CoreTakenOver=true;DualState.CoreCommitted=true;DualDirector.CompleteScene("dr11");});
        }

        private static StoreClient CreateDr13()
        {
            StoreClient client = Lydia("dual_dr13");
            Dialogue root = DualText.Build("v05_reward150", "dr13_open", "dr13_door");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_lydia"), L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr13",()=>{if(DualProgression.Earned(3))DualRewards.Give(3);});
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
            Dialogue yes = DualText.Build("dr14_reply_yes", "dr14_reason");
            Dialogue time = DualText.Build("dr14_reply_time", "dr14_reason");
            Dialogue tease = DualText.Build("dr14_reply_tease", "dr14_reason");
            Action<Dialogue> addTellChoices = branch => DualText.Choices(branch, "er_dual_prompt",
                DualText.Choice("dr14_accept", accept),
                DualText.Choice("dr14_together", accept));
            addTellChoices(yes);
            addTellChoices(time);
            addTellChoices(tease);
            // The player answers her confession before the "tell Eleanor"
            // decision; all three reactions continue through her explanation.
            DualText.Choices(root, "er_dual_prompt",
                DualText.Choice("dr14_yes", yes),
                DualText.Choice("dr14_time", time),
                DualText.Choice("dr14_tease", tease));
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
                DualState.ScavSource = RomanceState.ScavAssist ? 1 : 0;
                DualState.Flag("scav.transferred", true);
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
                DualBusiness.PlaquePlaced() ? "dr20_plaque" : "dr20_no_plaque");

            if (root == null) root = StoryEngine.Text(L.Eleanor, L.Get("er_dual_entry_resume"));
            Finish(client, root, "dr20", () =>
            {
                DualState.LastDailyDay = DualState.Today;
                StoryDirector.Notify(L.Get("er_dual_notice_ending"));
            });
            return Ready(client, root);
        }

        private static StoreClient CreateDaily()
        {
            int index = DualState.DailyIndex % 7 + 1;
            bool lydia = index == 2;
            StoreClient client = lydia ? Lydia("dual_daily_" + index)
                : Eleanor("dual_daily_" + index, "eleanor1");
            Dialogue root = lydia
                ? DualText.Build("dr_daily_" + index + "_open", "dr_daily_2_shop")
                : DualText.Build("dr_daily_" + index + "_open");
            if (root == null) root = StoryEngine.Text(L.Get("er_name_narrator"), L.Get("er_dual_entry_resume"));
            DualRewards.AppendClaims(root);
            Action complete = () =>
            {
                DualState.DailyIndex = index % 7;
                DualState.LastDailyDay = DualState.Today;
                DualDirector.CompleteScene("daily");
            };
            if (lydia) FinishAfterTrade(client, "daily", complete, 300);
            else DualText.Finish(root, complete);
            return Ready(client, root);
        }

    }
}
