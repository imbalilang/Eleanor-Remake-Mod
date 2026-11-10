using System;
using System.Linq;

namespace EleanorRomance
{
    /// <summary>Relationship dialogue layered over the existing mother/research routes.</summary>
    public static partial class MaidStory
    {
        public const int LegacyVisit=901, HousingVisit=902, BuyerVisit=903, FeedbackVisit=904,
            WorldChatVisit=905, WaitVisit=906, DailyVisit=907;

        private static StoreClient Eleanor(string scene,bool maid=false,string normal="eleanor_scav",StoreClient.ClientIntent intent=StoreClient.ClientIntent.DIALOGUE,bool upper=true)
        {
            var client=EleanorStory.NewEleanorClient("maid_"+scene,normal,intent,upper);
            if(maid)client.spriteName=PortraitCompat.ResolveMaid(client.identifier,normal);
            return client;
        }
        private static StoreClient Ready(StoreClient client,Dialogue root)
        { client.mainDialogue=root;client.CompleteClientCreation();return client; }
        private static StoryEngine.Choice Option(string key,Dialogue next,Action action=null,Func<bool> enabled=null,string description=null)
        {
            string label=ChoiceLabel(key);
            if(next!=null && next.title==L.Player && next.plainText==label && next.nextDialogue!=null)
                next=next.nextDialogue;
            return new StoryEngine.Choice(label,L.Get(description??"er_maid_choice_no_cost"),next,action,enabled);
        }
        private static string ChoiceLabel(string key)
        {
            switch(key)
            {
                case "er_choice_protect_sec":return L.Get(key,30,300);
                case "er_choice_protect_ul":return L.Get(key,30,400);
                case "er_choice_protect_case":return L.Get(key,200);
                case "er_choice_threat_report":case "er_choice_threat_upper":return L.Get(key,30);
                case "er_maid_choice_threat_report":case "er_maid_choice_threat_upper":return L.Get(key,30);
                case "er_choice_threat_handover":return L.Get(key,300);
                case "er_choice_threat_sell":return L.Get(key,600);
                default:return L.Get(key);
            }
        }
        private static void Choices(Dialogue root,params StoryEngine.Choice[] options)
            =>StoryEngine.AddChoices(root,L.Get("er_maid_prompt"),options);

        /// <summary>Recheck at selection, before selecting a successful response graph.</summary>
        private static void GuardedOption(Dialogue menu,string label,Dialogue success,Func<bool> pay,string description,Func<bool> enabled)
        {
            var response=StoryEngine.Text(L.Player,ChoiceLabel(label));
            response.SetNextDialogue(success);
            var retry=StoryEngine.Text(L.Get("er_name_narrator"),L.Get("er_maid_payment_retry"));
            retry.SetNextDialogue(menu);
            menu.AddChoice(DirectoryMaster.Item("yes"),ChoiceLabel(label),L.Get(description),response,
                ()=>response.SetNextDialogue(pay()?success:retry),enabled);
        }
        private static void FinishMain(Dialogue root,int code,int next,Action flags=null,bool extras=false)
        {
            Action extraCommit=extras?AppendSafeExtras(root):null;
            MaidText.Finish(root,()=>
            {
                if(RomanceState.PendingChapter!=code && RomanceState.Chapter!=code)return;
                extraCommit?.Invoke();flags?.Invoke();
                // Directly resumed chapters also use the established ordered completion gate.
                RomanceState.PendingChapter=code;
                EleanorStory.Advance(code,next);
                MaidState.LastImportantDay=MaidState.Today;
                MaidProjects.RefreshStates(true);
            });
        }
        private static Action AppendSafeExtras(Dialogue head)
        {
            bool housing=MaidProjects.ResultPending(1) && MaidState.Today>MaidProjects.PaidDay(1);
            bool buyer=MaidState.BuyerState==3 && !MaidState.BuyerFeedbackShown;
            bool procurement=MaidState.Flag("procurement_pending");
            bool world=MaidWorldEvents.ChatPending;
            if(housing)MaidText.Append(head,"m18_b009");
            if(buyer)MaidText.Append(head,MaidState.BuyerTraded?"m20_b003":"m20_b004");
            if(procurement)
                StoryEngine.Tail(head).SetNextDialogue(StoryEngine.Text(L.Eleanor,L.Get("er_maid_procurement_feedback")));
            if(world)MaidWorldEvents.AppendChat(head);
            return ()=>
            {
                if(housing)MaidProjects.ShowResult(1);
                if(buyer)MaidState.BuyerFeedbackShown=true;
                if(procurement)MaidState.Flag("procurement_pending",false);
                if(world)MaidWorldEvents.CompleteChat();
            };
        }
        public static StoreClient CreateEntry()
        {
            var client=Eleanor("entry");
            // A successful payment interrupted before the final line resumes its response;
            // it must never offer another payment or a fictitious refund.
            if(MaidState.EntryKind==1 && MaidState.Contract==1)
            {
                var paid=MaidText.Build("m11_b011");FinishMain(paid,12,13);return Ready(client,paid);
            }
            bool autofill=RomanceState.Savings<800;
            if(autofill)
            {
                RomanceState.Savings=800;RomanceState.SavingsAutoFilled=true;
                if(StoryEngine.PositiveDeals<2){RomanceState.LocketSold=true;RomanceState.LocketWithEleanor=false;}
            }
            var root=MaidText.Build(autofill?"m11_b002":"m11_b001");
            if(RomanceState.LocketSold)MaidText.Append(root,"m11_b003");
            MaidText.Append(root,"m11_b004");
            var ordinary=StoryEngine.Build(StoryV06Content.B_C11_B002);FinishMain(ordinary,12,13);
            var withdraw=MaidText.Build("m11_b012");FinishMain(withdraw,12,13);
            var offer=MaidText.Build("m11_b006","m11_b007","m11_b008");
            var menu=StoryEngine.Text(L.Player,L.Get("er_maid_entry_terms"));
            StoryEngine.Tail(offer).SetNextDialogue(menu);
            var success=MaidText.Build("m11_b011");
            FinishMain(success,12,13,()=>StoryDirector.Notify(L.Get("er_maid_notice_entry")));
            GuardedOption(menu,"er_maid_entry_confirm",success,MaidProjects.TryEnter,"er_maid_entry_terms",()=>StoryEngine.HasCash(800));
            menu.AddChoice(DirectoryMaster.Item("yes"),L.Get("er_maid_entry_back"),L.Get("er_maid_choice_no_cost"),withdraw,null,null);
            Choices(root,Option("er_maid_entry_normal",ordinary),Option("er_maid_entry_offer",offer,description:"er_maid_entry_terms"));
            return Ready(client,root);
        }
        public static StoreClient CreateLegacy()
        {
            var client=Eleanor("legacy",normal:"eleanor_scav",intent:StoreClient.ClientIntent.SELL);
            client.SetWholesale(25);
            var tracker=new StoryEngine.DealTracker();
            client.onItemSold=(item,paid)=>tracker.Record(item,paid,true);
            var root=MaidText.Build("legacy_b002");
            StoryEngine.InterceptDismiss(client,()=>
            {
                var wrap=MaidText.Build(tracker.Any?"legacy_b004":"legacy_b005","legacy_b006");
                var help=MaidText.Build("legacy_b008","m11_b007");
                var confirm=MaidText.Build("legacy_b011");
                MaidText.Finish(confirm,()=>MaidRouteMigration.Complete(true));
                var back=MaidText.Build("legacy_b012");
                MaidText.Finish(back,()=>MaidRouteMigration.Complete(false));
                var finalMenu=StoryEngine.Text(L.Player,L.Get("er_maid_prompt"));
                StoryEngine.Tail(help).SetNextDialogue(finalMenu);
                Choices(finalMenu,Option("er_maid_legacy_confirm",confirm,description:"er_maid_legacy_confirm_desc"),
                    Option("er_maid_legacy_back",back,description:"er_maid_legacy_back_desc"));
                var retract=MaidText.Build("legacy_b013");
                MaidText.Finish(retract,()=>MaidRouteMigration.Complete(false));
                Choices(wrap,Option("er_maid_legacy_help",help,description:"er_maid_legacy_help_desc"),
                    Option("er_maid_legacy_retract",retract,description:"er_maid_legacy_retract_desc"));
                return wrap;
            },null);
            return Ready(client,root);
        }
        public static StoreClient CreateMainChapter(int code)
        {
            if(!MaidState.HasHistory)return null;
            MaidRouteMigration.PreserveEstablishedAddress();
            if(code>=101 && code<=108)return CreateTragic(code);
            if(code==201)return CreateExit();
            if(!MaidState.Active)
            {
                if(code==23)return AttachOriginalSafe(EleanorStory.CreateFutureChapter());
                if(code==24)return AttachOriginalSafe(EleanorStory.CreateConclusionChapter());
                return null;
            }
            switch(code)
            {
                case 13:return CreateDrive(); case 14:return CreateThreat();case 15:return CreateDecision();
                case 16:return CreatePreparation();case 17:return CreateReunion();case 18:return CreateMother();
                case 19:return CreateWorkVisit();case 20:return CreateGrowingCloser();case 21:return CreateConfession();
                case 22:return CreateLydia();case 23:return CreateFuture();case 24:return CreateConclusion();
                default:return null;
            }
        }
        private static StoreClient CreateDrive()
        {
            var c=Eleanor("m12",normal:"eleanor2");
            var root=MaidText.Build("m12_b001");
            var study=MaidText.Build("m12_b002","m12_b003");
            var player=MaidText.Build("m12_b006","m12_b008","m12_b009");FinishMain(player,13,14,()=>RomanceState.DriveState=1);
            var her=MaidText.Build("m12_b007","m12_b008","m12_b009");FinishMain(her,13,14,()=>RomanceState.DriveState=2);
            var handback=MaidText.Build("choicefix_b001","m12_b002","m12_b003","m12_b007","m12_b008","m12_b009");
            FinishMain(handback,13,14,()=>RomanceState.DriveState=2);
            StoryEngine.AddChoices(study,L.Get("er_prompt_drive_custody"),
                Option("er_maid_choice_custody_keep",player,description:"er_maid_choice_custody_keep_desc"),
                Option("er_maid_choice_custody_carry",her,description:"er_maid_choice_custody_carry_desc"));
            if(RomanceState.KnewTrapUnwarned && !MaidState.Flag("reconciled") && !MaidState.Flag("honesty.promised"))
            {
                MaidText.Append(root,"m12_b004");
                Choices(root,Option("er_maid_reconcile_yes",study,()=>MaidState.Flag("honesty.promised",true),description:"er_maid_repair_promise_desc"),
                    Option("er_maid_repair_handback",handback,()=>
                    {
                        MaidState.Flag("honesty.promised",true);MaidState.Flag("honesty.handback",true);
                        RomanceState.DriveState=2;
                    },description:"er_maid_repair_handback_desc"));
            }
            else StoryEngine.Tail(root).SetNextDialogue(MaidState.Flag("honesty.handback")?handback:study);
            return Ready(c,root);
        }
        private static StoreClient CreateThreat()
        {
            var c=EleanorStory.NewNpcClient("maid_m13",L.Get("er_name_thug"),"maleBM5",EleanorStory.FactionKind.BlackMarket);
            var root=MaidText.Build("m13_b001");if(RomanceState.UncleReported)MaidText.Append(root,"m13_b002");MaidText.Append(root,"m13_b003");
            if(RomanceState.DriveState==2)MaidText.Append(root,"m13_b006");
            var leave=MaidText.Build("m13_b008");FinishMain(leave,14,15);
            var record=MaidText.Build("m13_b007");FinishMain(record,14,15,()=>RomanceState.ThreatRecorded=true);
            var betray=StoryEngine.Text(L.Get("er_name_narrator"),L.Get("er_line_threat_drive_taken"));
            FinishMain(betray,14,201,()=>{StoryEngine.AddCredit(300);Terminate(1,6);});
            var sell=StoryEngine.Text(L.Get("er_name_narrator"),L.Get("er_line_threat_drive_sold"));
            FinishMain(sell,14,201,()=>{StoryEngine.AddCredit(600);Terminate(2,6);});
            StoryEngine.AddChoices(root,L.Get("er_prompt_threat"),
                Option("er_maid_choice_threat_leave",leave,description:"er_maid_choice_threat_leave_desc"),
                Option("er_maid_choice_threat_report",record,description:"er_maid_choice_threat_report_desc",
                    enabled:()=>StoryEngine.SecRep()>=30 && (RomanceState.UncleReported||RomanceState.ThreatRecorded)),
                Option("er_maid_choice_threat_upper",record,description:"er_maid_choice_threat_upper_desc",
                    enabled:()=>StoryEngine.UlRep()>=30 && RomanceState.DriveState!=0),
                Option("er_choice_threat_handover",ConfirmRisk("er_maid_handover_warning","er_maid_handover_confirm","er_choice_threat_handover_desc",betray,root),
                    enabled:()=>RomanceState.HasDriveWithPlayer,description:"er_choice_threat_handover_desc"),
                Option("er_choice_threat_sell",ConfirmRisk("er_maid_sell_warning","er_maid_sell_confirm","er_maid_sell_risk_desc",sell,root),
                    enabled:()=>RomanceState.HasDriveWithPlayer && StoryEngine.BmRep()>=20,description:"er_maid_sell_risk_desc"));
            return Ready(c,root);
        }
        private static Dialogue ConfirmRisk(string warning,string confirm,string description,Dialogue proceed,Dialogue back)
        {
            var root=StoryEngine.Text(L.Get("er_name_narrator"),L.Get(warning));
            Choices(root,Option(confirm,proceed,description:description),Option("er_maid_reconsider",back,description:"er_maid_reconsider_desc"));
            return root;
        }
        private static StoreClient CreateDecision()
        {
            var c=Eleanor("m14");
            var root=MaidText.Build("m14_b001","m14_b002",StoryEngine.CanPrepareProtection()?"m14_b003":"m14_b004","m14_b005");
            var a=MaidText.Build("m14_b007","m14_b013");FinishMain(a,15,16,()=>RomanceState.Route=1);
            var b=MaidText.Build("m14_b008","m14_b013");FinishMain(b,15,16,()=>RomanceState.Route=2);
            var debate=MaidText.Build("m14_b009");
            var leak=MaidText.Build("m14_b010","m14_b013");FinishMain(leak,15,101,()=>RomanceState.Route=3);
            Choices(debate,Option("er_maid_confirm_leak",leak,description:"er_maid_public_risk_desc"),
                Option("er_maid_reconsider",root,description:"er_maid_reconsider_desc"));
            var secret=StoryEngine.Text(L.Get("er_name_narrator"),L.Get("er_line_secret_leak"));FinishMain(secret,15,201,()=>Terminate(3,5));
            var exit=MaidText.Build("m14_b011");FinishMain(exit,15,201,()=>Terminate(4,RomanceState.DriveState==1?2:RomanceState.DriveState));
            var difficulty=MaidText.Build("choicefix_b003");
            Choices(difficulty,Option("er_choice_route_a",a,description:"er_maid_rescue_exchange_desc"),
                Option("er_maid_reconsider",root,description:"er_maid_reconsider_desc"),
                Option("er_maid_choice_route_exit",exit,description:"er_maid_choice_route_exit_desc"));
            Choices(root,Option("er_choice_route_a",a,description:"er_maid_rescue_exchange_desc"),
                Option("er_choice_route_b",b,enabled:StoryEngine.CanPrepareProtection,description:"er_maid_rescue_protection_desc"),
                Option("er_choice_route_c",debate,description:"er_maid_public_risk_desc"),
                Option("er_choice_secret_leak",ConfirmRisk("er_maid_secret_warning","er_maid_secret_confirm","er_maid_secret_risk_desc",secret,root),
                    enabled:()=>RomanceState.HasDriveWithPlayer,description:"er_maid_secret_risk_desc"),
                Option("er_maid_route_difficulty",difficulty,description:"er_maid_route_difficulty_desc"));
            return Ready(c,root);
        }
        private static StoreClient CreatePreparation()
        {
            // Reuse the existing protection channels and their exact fees, not the projects.
            var c=Eleanor("m15");
            var root=MaidText.Build("m15_b001");
            if(RomanceState.Route==1)
            {
                MaidText.Append(root,"m15_b002","m15_b005","m15_b006");FinishMain(root,16,17);return Ready(c,root);
            }
            MaidText.Append(root,"m15_b003");
            var approved=MaidText.Build("m15_b005","m15_b006");FinishMain(approved,16,17,()=>RomanceState.ProtectionReady=true);
            if(RomanceState.ProtectionChannel!=0)
            {
                StoryEngine.Tail(root).SetNextDialogue(approved);
                return Ready(c,root); // An interrupted, already-paid channel resumes without charging or re-gating.
            }
            var fallback=MaidText.Build("m15_b004","m15_b002","m15_b005","m15_b006");
            FinishMain(fallback,16,17,()=>{RomanceState.Route=1;RomanceState.ProtectionFailed=true;});
            var menu=StoryEngine.Text(L.Player,L.Get("er_prompt_protection"));StoryEngine.Tail(root).SetNextDialogue(menu);
            GuardedOption(menu,"er_choice_protect_sec",approved,()=>PayProtection(300,1),"er_choice_protect_sec_desc",
                ()=>StoryEngine.SecRep()>=30 && (RomanceState.UncleReported||RomanceState.ThreatRecorded) && StoryEngine.HasCash(300));
            GuardedOption(menu,"er_choice_protect_ul",approved,()=>PayProtection(400,2),"er_choice_protect_ul_desc",
                ()=>StoryEngine.UlRep()>=30 && RomanceState.DriveState!=0 && StoryEngine.HasCash(400));
            GuardedOption(menu,"er_choice_protect_case",approved,()=>PayProtection(200,3),"er_choice_protect_case_desc",
                ()=>RomanceState.UncleReported && StoryEngine.HasCash(200));
            menu.AddChoice(DirectoryMaster.Item("yes"),L.Get("er_choice_protect_fallback"),L.Get("er_choice_protect_fallback_desc"),fallback,null,null);
            return Ready(c,root);
        }
        private static bool PayProtection(int amount,int channel)
        {
            bool valid=channel==1 ? StoryEngine.SecRep()>=30 && (RomanceState.UncleReported||RomanceState.ThreatRecorded)
                : channel==2 ? StoryEngine.UlRep()>=30 && RomanceState.DriveState!=0 : RomanceState.UncleReported;
            if(!valid)return false;
            if(RomanceState.ProtectionChannel!=0)return true;
            if(!StoryEngine.Spend(amount))return false;
            RomanceState.ProtectionChannel=channel;return true;
        }
        private static StoreClient CreateReunion()
        {
            var c=Eleanor("m16");bool b=RomanceState.Route==2 && RomanceState.ProtectionReady;
            Dialogue root;
            if(MaidInteractions.MorningEntry)
            {
                root=MaidText.Build(b?"interaction_b008":"interaction_b007");
                var detail=MaidText.Build(b?"m16_b003":"m16_b001");
                detail=detail.nextDialogue;
                if(!b)detail=detail.nextDialogue; // Skip the already answered "brought her out?"
                StoryEngine.Tail(root).SetNextDialogue(detail);
                if(!b)MaidText.Append(root,"m16_b002");
                MaidText.Append(root,"m16_b004","m16_b005","m16_b006");
            }
            else root=b?MaidText.Build("m16_b003","m16_b004","m16_b005","m16_b006")
                :MaidText.Build("m16_b001","m16_b002","m16_b004","m16_b005","m16_b006");
            bool repair=MaidState.Flag("honesty.promised") && !MaidState.Flag("reconciled");
            if(repair)MaidText.Append(root,"m16_b007");
            Action complete=()=>
            {
                RomanceState.MotherState=1;RomanceState.DriveState=b?4:3;
                MaidInteractions.Flag("morning.entry",false);
                if(b)StoryEngine.TryFreeNutrifruit();else RomanceState.Route=1;
                if(repair)MaidState.Flag("reconciled",true);
            };
            // Route A keeps the original "returned it" affection path; the fixed
            // dialogue replaces the removed compensation menu and never adds cash.
            FinishMain(root,17,18,()=>{if(!b)RomanceState.AddAffection(1);complete();});
            return Ready(c,root);
        }
        private static StoreClient CreateMother()
        {
            var c=EleanorStory.NewNpcClient("maid_m17",L.Get("er_name_mother"),EleanorStory.MotherSpriteKey(),EleanorStory.FactionKind.Upper);
            c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add("HOUSEHOLD_GOOD");c.SetBudget(220);c.SetVariety();
            var root=MaidText.Build("m17_b001",MaidState.EntryKind==2?"m17_b004":"m17_b003","m17_b005",RomanceState.Route==2?"m17_b007":"m17_b006");
            StoryEngine.InterceptDismiss(c,()=>MaidText.Build("m17_b008"),()=>
            {
                RomanceState.MotherVisited=true;RomanceState.PendingChapter=18;EleanorStory.Advance(18,19);MaidState.LastImportantDay=MaidState.Today;
            });
            return Ready(c,root);
        }
        private static StoreClient CreateWorkVisit()
        {
            var c=Eleanor("m18",intent:StoreClient.ClientIntent.SELL);c.SetWholesale(25);
            var tracker=new StoryEngine.DealTracker();c.onItemSold=(item,paid)=>tracker.Record(item,paid,true);
            var root=MaidText.Build("m18_b002","m18_b003","m18_b004");
            StoryEngine.Tail(root).SetEndAction(()=>
            { if(RomanceState.ScavStockSpawned19)return;RomanceState.ScavStockSpawned19=true;StoreClientList.AddScavGeneralStock(); });
            Action extraCommit=null;
            StoryEngine.InterceptDismiss(c,()=>
            {
                var wrap=MaidText.Build(tracker.Any?"m18_b005":"m18_b006","m18_b007","m18_b008");
                extraCommit=AppendSafeExtras(wrap);MaidText.Append(wrap,"m18_b010");return wrap;
            },()=>
            {
                EleanorStory.RecordDeal(tracker,true);extraCommit?.Invoke();
                RomanceState.PendingChapter=19;EleanorStory.Advance(19,20);MaidState.LastImportantDay=MaidState.Today;MaidProjects.RefreshStates(true);
            });
            return Ready(c,root);
        }
        private static StoreClient CreateGrowingCloser()
        {
            var c=Eleanor("m19",true);var root=MaidText.Build("m19_b001");
            var after=MaidText.Build("m19_b005");
            if(MaidState.AddressAccepted)MaidText.Append(after,"m19_b007");
            else
            {
                var first=MaidText.Build("m19_b006");
                StoryEngine.Tail(first).SetEndAction(()=>MaidState.AddressAccepted=true);
                StoryEngine.Tail(after).SetNextDialogue(first);
                MaidText.Append(after,"m19_b008");
            }
            FinishMain(after,20,21,()=>MaidState.DebutShown=true);
            if(RomanceState.KnewTrapUnwarned && !MaidState.Flag("reconciled"))
            {
                MaidText.Append(root,"m19_b002");
                var accept=StoryEngine.Text(L.Eleanor,L.Get("er_maid_repair_ack"));
                accept.SetEndAction(()=>MaidState.Flag("reconciled",true));accept.SetNextDialogue(after);
                var records=MaidText.Build("choicefix_b002");
                StoryEngine.Tail(records).SetEndAction(()=>
                {MaidState.Flag("reconciled",true);MaidState.Flag("honesty.open_records",true);});
                StoryEngine.Tail(records).SetNextDialogue(after);
                Choices(root,Option("er_maid_reconcile_yes",accept,description:"er_maid_repair_promise_desc"),
                    Option("er_maid_repair_records",records,description:"er_maid_repair_records_desc"));
            }
            else StoryEngine.Tail(root).SetNextDialogue(after);
            return Ready(c,root);
        }
        private static StoreClient CreateConfession()
        {
            var c=Eleanor("m20",true);var root=MaidText.Build("m20_b001","m20_b005","m20_b006");
            var yes=MaidText.Build("m20_b008","m20_b009");FinishMain(yes,21,22,Confess);
            var close=MaidText.Build("choicefix_b004","m20_b009");FinishMain(close,21,22,Confess);
            var tease=MaidText.Build("choicefix_b005","m20_b009");FinishMain(tease,21,22,Confess);
            Choices(root,Option("er_maid_confess_yes",yes,description:"er_maid_confess_continue_desc"),
                Option("er_maid_confess_close",close,description:"er_maid_confess_continue_desc"),
                Option("er_maid_confess_tease",tease,description:"er_maid_confess_continue_desc"));
            return Ready(c,root);
        }
        private static void Confess()
        {
            if(!MaidState.Active)return;
            MaidState.Confession=1;RomanceState.Romance=1;
            InviteBuyer();
        }
        private static StoreClient CreateLydia()
        {
            var c=EleanorStory.NewNpcClient("maid_m21",L.Get("er_name_lydia"),EleanorStory.LydiaSpriteKey(),EleanorStory.FactionKind.Upper);
            c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add(TypeHelper.LUXURY_ITEM);c.clientBuyingTagList.Add(TypeHelper.ART);c.clientBuyingTagList.Add(TypeHelper.HIGHEND);c.SetBudget(500);c.SetVariety();
            var root=MaidText.Build("m21_b001");
            StoryEngine.InterceptDismiss(c,()=>
            {
                var wrap=MaidText.Build("m21_b002",MaidState.EntryKind==2?"m21_b004":"m21_b003");
                if(RomanceState.LydiaFairBracelet)MaidText.Append(wrap,"m21_b005");
                if(MaidState.Confession==1)MaidText.Append(wrap,"m21_b006");
                MaidText.Append(wrap,"m21_b007");return wrap;
            },()=>{RomanceState.PendingChapter=22;EleanorStory.Advance(22,23);MaidState.LastImportantDay=MaidState.Today;});
            return Ready(c,root);
        }
        private static StoreClient CreateFuture()
        {
            var c=Eleanor("m22",true);
            var root=MaidText.Build(RomanceState.Route==2?"m22_b002":"m22_b001","m22_b003");
            // A save migrated from the retired deferred confession already heard
            // Lydia's "she didn't let me ask" variant in reading chapter 21.
            bool lydiaDeferred=MaidState.Flag("migrated.deferred") && MaidState.Flag("migrated.lydia_deferred");
            if(!lydiaDeferred)MaidText.Append(root,"m22_b004");
            var continuation=MaidBusiness.Say("order_after");
            AttachLocket(continuation,null);
            MaidBusiness.AppendOrderOffer(root,continuation);
            return Ready(c,root);
        }
        private static void AttachLocket(Dialogue head,Action relationship)
        {
            // The locket only exists on the attendant-lovers path; a migrated
            // deferred save uses the same delivery/retry rules.
            if(RomanceState.LocketWithEleanor && !RomanceState.LocketSold && !RomanceState.LocketGiven)
            {
                MaidText.Append(head,"m22_b005");
                var accepted=MaidText.Build("m22_b006");FinishMain(accepted,23,24,relationship,true);
                var refused=MaidText.Build("m22_b007");FinishMain(refused,23,24,relationship,true);
                var menu=StoryEngine.Text(L.Player,L.Get("er_prompt_locket"));StoryEngine.Tail(head).SetNextDialogue(menu);
                GuardedOption(menu,"er_choice_take_locket",accepted,TryLocket,"er_choice_take_locket_desc",()=>true);
                menu.AddChoice(DirectoryMaster.Item("yes"),L.Get("er_choice_refuse_locket"),L.Get("er_choice_refuse_locket_desc"),refused,null,null);
            }
            else
            {
                if(RomanceState.LocketSold)MaidText.Append(head,"m22_b008");
                FinishMain(head,23,24,relationship,true);
            }
        }
        private static bool TryLocket()
        { if(RomanceState.LocketGiven)return true;Items.GiveLocket();return RomanceState.LocketGiven; }
        private static StoreClient CreateConclusion()
        {
            var c=Eleanor("m23",true,normal:"eleanor1");
            var root=MaidText.Build("m23_b001","m23_b002");
            var reward=MaidText.Build("m23_b003","m23_b005");
            bool receipt=MaidWorldEvents.AppendReceipt(reward);
            MaidText.Append(reward,"m23_b007","m23_b008","m23_b009","m23_b010");
            Action extras=AppendSafeExtras(reward);
            MaidText.Finish(reward,()=>
            {
                if(MaidState.BasicComplete)return;
                extras?.Invoke();if(receipt)MaidWorldEvents.SubmitReceipt();
                MaidState.BasicComplete=true;MaidState.BasicDay=MaidState.Today;MaidState.Progress=23;MaidState.ProgressDay=MaidState.Today;
                StoryDirector.CompleteCore();MaidState.LastImportantDay=MaidState.Today;MaidProjects.RefreshStates(true);
                StoryDirector.Notify(L.Get("er_maid_notice_basic"));
            });
            // Delivery succeeds before the gift speech; full storage leaves a visible retry.
            var menu=StoryEngine.Text(L.Player,L.Get("er_maid_reward_prompt"));StoryEngine.Tail(root).SetNextDialogue(menu);
            GuardedOption(menu,"er_maid_reward_claim",reward,()=>
            { StoryEngine.GiveSelfRefillingPerfume();return RomanceState.PerfumeGiven; },"er_maid_reward_desc",()=>true);
            menu.AddChoice(DirectoryMaster.Item("yes"),L.Get("er_maid_reward_later"),L.Get("er_maid_reward_desc"),
                MaidText.Finish(StoryEngine.Text(L.Eleanor,L.Get("er_maid_reward_keep")),null),null,null);
            return Ready(c,root);
        }
        internal static void GateOriginalReward(Dialogue head,Dialogue reward)
        {
            var menu=StoryEngine.Text(L.Player,L.Get("er_maid_reward_prompt"));StoryEngine.Tail(head).SetNextDialogue(menu);
            GuardedOption(menu,"er_maid_reward_claim",reward,()=>
            {StoryEngine.GiveSelfRefillingPerfume();return RomanceState.PerfumeGiven;},"er_maid_reward_desc",()=>true);
            menu.AddChoice(DirectoryMaster.Item("yes"),L.Get("er_maid_reward_later"),L.Get("er_maid_reward_desc"),
                MaidText.Finish(StoryEngine.Text(L.Eleanor,L.Get("er_maid_reward_keep")),null),null,null);
        }
        internal static void GateOriginalLocket(Dialogue head,Dialogue accept,Dialogue refuse)
        {
            var menu=StoryEngine.Text(L.Player,L.Get("er_prompt_locket"));StoryEngine.Tail(head).SetNextDialogue(menu);
            GuardedOption(menu,"er_choice_take_locket",accept,TryLocket,"er_choice_take_locket_desc",()=>true);
            menu.AddChoice(DirectoryMaster.Item("yes"),L.Get("er_choice_refuse_locket"),L.Get("er_choice_refuse_locket_desc"),refuse,null,null);
        }
        private static StoreClient AttachOriginalSafe(StoreClient client)
        {
            var visited=new System.Collections.Generic.HashSet<Dialogue>();
            Action<Dialogue> decorate=null;
            decorate=node=>
            {
                if(node==null || !visited.Add(node))return;
                if(node.nextDialogue!=null)decorate(node.nextDialogue);
                foreach(var choice in node.choices)decorate(choice.Item2);
                if(node.nextDialogue!=null || node.choices.Count>0 || node.endAction==null)return;
                Action end=node.endAction;
                node.SetEndAction(null);
                Action extras=AppendSafeExtras(node);
                StoryEngine.Tail(node).SetEndAction(()=>{extras?.Invoke();end();});
            };
            decorate(client.mainDialogue);
            return client;
        }
        private static void Terminate(int kind,int drive)
        {
            RomanceState.Route=4;RomanceState.BetrayalKind=kind;RomanceState.DriveState=drive;
            MaidState.End(kind==4?"investigation-exit":"betrayal",4);
        }
    }
}
