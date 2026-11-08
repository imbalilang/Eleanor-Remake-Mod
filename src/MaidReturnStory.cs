using System;
using System.Linq;

namespace EleanorRomance
{
    public static partial class MaidStory
    {
        public static int EventForDay(int day)
        {
            if(MaidState.LegacyResume==1)return day>=MaidState.LegacyDueDay?LegacyVisit:0;
            if(MaidState.PendingVisit>0)return day>=MaidState.NextVisitDay?MaidState.PendingVisit:0;
            bool core=RomanceState.Chapter>=RomanceState.ChapterComplete && RomanceState.EpilogueDone;
            if(core && MaidProjects.ResultPending(1) && day>MaidProjects.PaidDay(1) && !RomanceState.RomanceLocked)return HousingVisit;
            if(MaidState.Lovers && MaidState.BasicComplete && MaidState.Progress<28)
            {
                int progress=MaidState.Progress;
                int due=MaidState.ProgressDay+(progress==23?2:progress==24||progress==26?3:2);
                if(progress==23)due=Math.Max(due,MaidProjects.PaidDay(2)+1);
                if(progress==24)due=Math.Max(due,MaidProjects.PaidDay(3)+1);
                if(progress==26)due=Math.Max(due,MaidProjects.PaidDay(4)+1);
                bool ready=progress==23?MaidProjects.Paid(2):progress==24?MaidProjects.Paid(3):progress==26?MaidProjects.Paid(4):true;
                if(ready && day>=due)return progress+1;
            }
            if(MaidState.BuyerState==1 && day>=MaidState.BuyerDueDay && !RomanceState.RomanceLocked)return BuyerVisit;
            if(!core)return 0;
            if(MaidState.BuyerState==3 && !MaidState.BuyerFeedbackShown && !RomanceState.RomanceLocked)return FeedbackVisit;
            if(MaidWorldEvents.ChatPending)return WorldChatVisit;
            if(!MaidState.Lovers)return 0;
            if(GalaStory.Due(day))return GalaStory.VisitId;
            if(day<Math.Max(MaidState.LastChatDay+7,MaidState.ProgressDay+7))return 0;
            if(MaidState.Progress<28)
                return WaitingProject()>0 && !MaidState.Flag("wait_stage_"+MaidState.Progress)?WaitVisit:0;
            return MaidState.Int("daily_index")<4?DailyVisit:0;
        }

        public static bool TryQueue(StoreClientManager manager,int day)
        {
            int id=EventForDay(day);if(id==0)return false;
            StoreClient client=CreateEvent(id);if(client==null)return false;
            ModHelper.SetClientSource(client,ModConstants.ModId);manager.AddClient(client);
            if(manager.clientStack==null || !manager.clientStack.Contains(client))return false;
            MaidState.PendingVisit=id;MaidState.NextVisitDay=day;MaidState.LastImportantDay=day;RomanceState.LastQueuedDay=day;
            return true;
        }
        public static StoreClient CreateEvent(int id)
        {
            switch(id)
            {
                case LegacyVisit:return CreateLegacy();case HousingVisit:return CreateStandalone("housing");
                case BuyerVisit:return CreateBuyer();case FeedbackVisit:return CreateStandalone("feedback");
                case WorldChatVisit:return CreateStandalone("world");
                case WaitVisit:return CreateWait();case DailyVisit:return CreateDaily();
                case 24:return CreateReturn24();case 25:return CreateReturn25();case 26:return CreateProcurement();
                case 27:return CreateReturn27();case 28:return CreateReturn28();
                case MaidDividends.VisitId:return MaidDividends.CreateVisit();case GalaStory.VisitId:return GalaStory.CreateVisit();
                default:return MaidBusiness.CreateEvent(id);
            }
        }
        private static void CompleteEvent(int id,Action flags=null)
        {
            if(MaidState.PendingVisit!=id)return;
            flags?.Invoke();MaidState.PendingVisit=0;MaidState.NextVisitDay=-1;MaidState.LastImportantDay=MaidState.Today;
            MaidProjects.RefreshStates(true);
        }
        private static Dialogue FinishEvent(Dialogue head,int id,Action flags=null,bool extras=true)
        {
            Action extra=extras?AppendSafeExtras(head):null;
            if(id!=28)MaidDividends.WithReceipt(head);
            if(id<24 || id>28)head=MaidInteractions.WithLight(head);
            MaidText.Finish(head,()=>CompleteEvent(id,()=>{extra?.Invoke();flags?.Invoke();}));
            return head;
        }
        private static StoreClient CreateStandalone(string scene)
        {
            var c=Eleanor(scene,MaidState.Lovers && MaidState.DebutShown);
            int id=scene=="housing"?HousingVisit:scene=="feedback"?FeedbackVisit:WorldChatVisit;
            // AppendSafeExtras is the only producer of result flags, so combined
            // housing/buyer/world visits never duplicate their text or commits.
            var root=StoryEngine.Text(L.Eleanor,L.Get("er_maid_safe_visit"));
            root=FinishEvent(root,id);return Ready(c,root);
        }
        private static void AdvanceReturn(int chapter)
        {
            MaidDividends.Accrue();
            MaidState.Progress=chapter;MaidState.ProgressDay=MaidState.Today;
            if(chapter==24)MaidProjects.ShowResult(2);
            if(chapter==25)MaidProjects.ShowResult(3);
            if(chapter==27)MaidProjects.ShowResult(4);
            MaidInteractions.OnProgress(chapter);
            MaidDividends.Accrue();
        }
        private static StoreClient CreateReturn24()
        {
            var c=Eleanor("m24",normal:"eleanor1");
            var opening=MaidText.Build("m24_b001");
            Action extra=null;
            MaidBusiness.ConfigureSupply(c,opening,()=>
            {
                var ending=MaidText.Build("m24_b002",MaidProjects.FavorPlan(2)?"m24_b003":"m24_b004","m24_b005");
                MaidText.Append(ending,RomanceState.Route==1?"m24_b006":MaidWorldEvents.Acquitted?"m24_b008":"m24_b007");
                if(RomanceState.Route==2 && !MaidWorldEvents.Acquitted)
                    StoryEngine.Tail(ending).SetNextDialogue(StoryEngine.Text(L.Player,L.Get("er_maid_one_step")));
                MaidText.Append(ending,"m24_b009","m24_b010");extra=AppendSafeExtras(ending);return ending;
            },()=>CompleteEvent(24,()=>{extra?.Invoke();AdvanceReturn(24);}));
            return Ready(c,opening);
        }
        private static StoreClient CreateReturn25()
        {
            var c=Eleanor("m25",normal:"eleanor1");
            var root=MaidText.Build("m25_b001","m25_b002");
            MaidBusiness.AttachSample(c,root);
            Action extra=null;
            StoryEngine.InterceptDismiss(c,()=>
            {
                var wrap=MaidBusiness.Done("sample.claimed")?MaidText.Build("m25_b003"):MaidBusiness.Say("sample_later");
                MaidInteractions.AppendLessonEcho(wrap);
                MaidText.Append(wrap,"m25_b004",MaidProjects.FavorPlan(3)?"m25_b005":"m25_b006",
                    RomanceState.Route==1?"m25_b007":"m25_b008","m25_b009");
                if(MaidProjects.Paid(3) && !MaidProjects.ResultShown(3))
                    StoryEngine.Tail(wrap).SetNextDialogue(MaidDividends.Say("first"));
                extra=AppendSafeExtras(wrap);return wrap;
            },()=>CompleteEvent(25,()=>{extra?.Invoke();AdvanceReturn(25);MaidState.SetInt("biz.sample.next",MaidState.Today+3);}));
            return Ready(c,root);
        }
        private static StoreClient CreateProcurement()
        {
            var c=EleanorStory.NewNpcClient("maid_m26",L.Get("er_name_upper_buyer"),
                PortraitCompat.Resolve(SpriteDict.GetRandomUpperMaleSpriteName()),EleanorStory.FactionKind.Upper);
            c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add(TypeHelper.HIGHEND);c.clientBuyingTagList.Add("HOUSEHOLD_GOOD");c.SetBudget(600);c.SetVariety();
            var root=MaidText.Build("m26_b001");
            var first=MaidText.Build("m26_b003","m26_b005");var second=MaidText.Build("m26_b004","m26_b005");
            Choices(root,Option("er_maid_procure_no1",first),Option("er_maid_procure_no2",second));
            StoryEngine.InterceptDismiss(c,()=>
            {
                var wrap=MaidText.Build("m26_b007");return wrap;
            },()=>CompleteEvent(26,()=>{AdvanceReturn(26);MaidState.Flag("procurement_pending",true);}));
            return Ready(c,root);
        }
        private static StoreClient CreateReturn27()
        {
            var c=Eleanor("m27",normal:"eleanor1");
            bool attended=RomanceState.Route==2 && MaidWorldEvents.Acquitted;
            var root=MaidText.Build("m27_b002",RomanceState.Route==1?"m27_b005":attended?"m27_b003":"m27_b004","m27_b006");
            if(MaidProjects.FavorPlan(4))MaidText.Append(root,"m27_b007","m27_b008");else MaidText.Append(root,"m27_b009");
            if(MaidProjects.Paid(4) && !MaidProjects.ResultShown(4))
                StoryEngine.Tail(root).SetNextDialogue(MaidDividends.Say("established"));
            MaidDividends.WithReceipt(root);
            MaidText.Append(root,"m27_b010","m27_b011");
            Action extra=AppendSafeExtras(root);
            MaidBusiness.AttachRestock(c);
            StoryEngine.InterceptDismiss(c,()=>root,()=>CompleteEvent(27,()=>{extra?.Invoke();MaidState.Flag("mother_at_27",attended);AdvanceReturn(27);}));
            return Ready(c,c.mainDialogue);
        }
        private static StoreClient CreateReturn28()
        {
            var c=Eleanor("m28",true);
            var root=MaidText.Build("m28_b002","m28_b003");
            string[] blocks={"m28_b005","m28_b006","m28_b007"};
            var options=new StoryEngine.Choice[3];
            for(int i=0;i<3;i++)
            {
                var branch=MaidText.Build(blocks[i]);if(i==2)MaidText.Append(branch,"m28_b008");
                MaidText.Append(branch,"m28_b010","m28_b011");
                branch=FinishEvent(branch,28,()=>{AdvanceReturn(28);MaidState.Contract=3;MaidState.LastChatDay=MaidState.Today;});
                options[i]=Option("er_maid_final_"+(i+1),branch,description:"er_maid_final_continue_desc");
            }
            Choices(root,options);return Ready(c,root);
        }
        private static StoreClient CreateWait()
        {
            int project=WaitingProject();
            var cash=MaidProjects.Find("er_maid_p"+project+"_cash");
            bool moneyShort=cash!=null && MaidProjects.GetQuote(cash).Cash>PlayerStore.Instance.GetCash();
            bool connectionsShort=cash!=null && cash.DiscountCost>0 && !MaidProjects.DiscountEligible(cash);
            int index=-1;
            int cursor=MaidState.Int("wait_index")%3;
            for(int pass=0;pass<2 && index<0;pass++)for(int n=0;n<3;n++)
            {
                int candidate=(cursor+n)%3;
                bool appropriate=candidate==2 || candidate==0 && moneyShort || candidate==1 && moneyShort && connectionsShort;
                if(appropriate && (pass==1 || !MaidState.Flag("wait_seen_"+candidate))){index=candidate;break;}
            }
            var c=Eleanor("wait",true);
            var root=MaidText.Build("wait_b00"+(index+1));int stage=MaidState.Progress;
            if(project>0)
            {
                var reminder=StoryEngine.Text(L.Eleanor,L.Get("er_maid_wait_project_"+project));
                reminder.SetNextDialogue(root);root=reminder;
            }
            root=FinishEvent(root,WaitVisit,()=>{MaidState.LastChatDay=MaidState.Today;MaidState.SetInt("wait_index",index+1);MaidState.Flag("wait_seen_"+index,true);MaidState.Flag("wait_stage_"+stage,true);});
            return Ready(c,root);
        }
        internal static int WaitingProject()
        {
            int p=!MaidProjects.ResultShown(1)?1:MaidState.Progress==23?2:MaidState.Progress==24?3:MaidState.Progress==26?4:0;
            return p>0 && !MaidProjects.Paid(p) && MaidProjects.Available(p)?p:0;
        }
        private static StoreClient CreateDaily()
        {
            int index=MaidState.Int("daily_index")%4;var c=Eleanor("daily",true);
            var root=MaidText.Build("daily_b00"+(index+2));if(index==3)MaidText.Append(root,"daily_b006");
            root=FinishEvent(root,DailyVisit,()=>{MaidState.LastChatDay=MaidState.Today;MaidState.SetInt("daily_index",index+1);});return Ready(c,root);
        }
        private static void InviteBuyer()
        {
            if(MaidState.BuyerState!=0)return;
            MaidState.BuyerState=1;MaidState.BuyerDueDay=MaidState.Today+1;
        }
        private static StoreClient CreateBuyer()
        {
            var c=EleanorStory.NewNpcClient("maid_buyer",L.Get("er_name_upper_buyer"),
                PortraitCompat.Resolve(SpriteDict.GetRandomUpperMaleSpriteName()),EleanorStory.FactionKind.Upper);
            c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add(TypeHelper.HIGHEND);c.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            c.clientBuyingTagList.Add(TypeHelper.MATERIAL);c.clientBuyingTagList.Add(TypeHelper.TOOL);c.SetBudget(600);c.SetVariety();
            c.onItemBought=(item,paid)=>MaidState.BuyerTraded=true;
            var root=StoryEngine.Text(c.displayName,L.Get("er_maid_buyer_intro"));
            StoryEngine.InterceptDismiss(c,()=>StoryEngine.Text(c.displayName,L.Get("er_maid_buyer_close")),
                ()=>CompleteEvent(BuyerVisit,()=>{MaidState.BuyerState=3;MaidState.SetInt("buyer_left_day",MaidState.Today);}));
            return Ready(c,root);
        }
        public static bool HasUrgentClient(StoreClientManager manager)
        {
            return manager.clientStack!=null && manager.clientStack.Any(c=>c!=null && !VanillaEleanorBridge.IsOurClient(c)
                && (c.isSentenceOfficer || !c.dismissable && c.clientIntent==StoreClient.ClientIntent.DIALOGUE
                    || c.isSecurity && !string.IsNullOrEmpty(c.eventSourceId)));
        }
    }
}
