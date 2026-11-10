using System;

namespace EleanorRomance
{
    /// <summary>Native shop actions. Only order scheduling and one-shot receipts are mod state.</summary>
    public static class MaidBusiness
    {
        public const int CollectVisit=910, SellVisit=911, BuyVisit=912, SampleVisit=914;
        private const int RetiredAppraiseVisit=913; // Reserved for old pending visits; never schedule again.
        private const string LegacyAppraisalTag="ER_BUSINESS_APPRAISAL";
        public const string SampleTag="ER_BUSINESS_SAMPLE";
        private static string K(string key)=>"biz."+key;
        public static int Number(string key,int fallback=0)=>MaidState.Int(K(key),fallback);
        public static bool Done(string key)=>MaidState.Flag(K(key));
        private static void Set(string key,int value)=>MaidState.SetInt(K(key),value);
        private static void Mark(string key)=>MaidState.Flag(K(key),true);
        public static Dialogue Say(string key,params object[] args)=>StoryEngine.Text(L.Eleanor,L.Get("er_biz_"+key,args));
        private static string Receipt(StoreClient c)=>c.identifier+".stock";

        public static void OnLoaded()
        {
            if(!RomanceState.StoryActive || MaidState.PendingVisit!=RetiredAppraiseVisit)return;
            MaidState.PendingVisit=BuyVisit;
            Set("routine.cycle",1);
        }
        private static void PutStock(GameItem item)=>PlayerStore.Instance.AddDirectSellingItemToTableMiddle(item,isOwend:false);
        private static void Stock(StoreClient c,params string[] ids)
        {
            if(Done(Receipt(c)))return;
            // Each successfully placed item has its own receipt: a failed second
            // placement cannot respawn the first item after loading.
            for(int i=0;i<ids.Length;i++)
            {
                string key=Receipt(c)+"."+i;
                if(Done(key))continue;
                var item=DirectoryMaster.Item(ids[i]);
                StolenHelper.RemoveStolenStatus(item);
                PutStock(item);Mark(key);
            }
            Mark(Receipt(c));
        }
        private static bool SafeItem(GameItem item)=>item!=null && !item.IsTag("IMPORTANT_TAG") &&
            !item.IsTag(LegacyAppraisalTag) && !StolenHelper.IsStolenItem(item);
        private static void Buyer(StoreClient c,int budget,string receipt,Func<GameItem,bool> filter=null)
        {
            string paidTag="ER_BIZ_PAID_"+receipt;
            c.clientIntent=StoreClient.ClientIntent.BUY;c.noAcceptingContraband=true;c.SetBudget(budget);
            // Native IsClientBuyingThisItem accepts listed tags before consulting
            // the predicate. Keep all restrictions in that predicate.
            c.willAcceptItem=item=>SafeItem(item) && !item.IsTag(paidTag) &&
                (item.IsTag(TypeHelper.TOOL) || item.IsTag(TypeHelper.MATERIAL) || item.IsTag("HOUSEHOLD_GOOD")) &&
                (filter==null || filter(item));
            c.OnArrival+=()=>c.OverrideBudget(Math.Max(0,budget-Number(receipt)));
            c.onItemBought+=(item,paid)=>
            {
                if(item==null || item.IsTag(paidTag))return;
                Set(receipt,Number(receipt)+Math.Max(0,paid));item.EnableTag(paidTag);
            };
        }

        public static bool AcceptOrder()
        {
            if(Number("order.state")!=0)return Number("order.state")==1;
            Set("order.state",1);Set("order.next",MaidState.Today+2);
            Set("order.budget",600);return true;
        }
        public static void DeclineOrder(){if(Number("order.state")==0)Set("order.state",3);}
        public static void AppendOrderOffer(Dialogue head,Dialogue continuation)
        {
            if(Number("order.state")!=0){StoryEngine.Tail(head).SetNextDialogue(continuation);return;}
            StoryEngine.Tail(head).SetNextDialogue(Say("order_offer"));
            StoryEngine.AddChoices(head,L.Get("er_biz_order_prompt"),
                new StoryEngine.Choice(L.Get("er_biz_order_accept"),L.Get("er_biz_order_terms"),continuation,()=>AcceptOrder()),
                new StoryEngine.Choice(L.Get("er_biz_order_decline"),L.Get("er_maid_choice_no_cost"),continuation,DeclineOrder));
        }
        public static bool AcceptsOrderItem(GameItem item)
        {
            if(Number("order.state")!=1 || !SafeItem(item) || item.IsTag("ER_ORDER_DELIVERED"))return false;
            return item.IsTag(TypeHelper.TOOL) && Number("order.tools")<2 ||
                item.IsTag(TypeHelper.MATERIAL) && Number("order.materials")<1;
        }
        private static void RecordOrderItem(GameItem item,int paid)
        {
            if(!AcceptsOrderItem(item))return;
            if(item.IsTag(TypeHelper.TOOL) && Number("order.tools")<2)Set("order.tools",Number("order.tools")+1);
            else Set("order.materials",Number("order.materials")+1);
            item.EnableTag("ER_ORDER_DELIVERED");
            if(Number("order.tools")==2 && Number("order.materials")==1)Set("order.state",2);
        }
        private static StoreClient Collect()
        {
            var c=EleanorStory.NewEleanorClient("biz_collect","eleanor_scav",StoreClient.ClientIntent.BUY);
            Buyer(c,Number("order.budget",600),"order.spent",AcceptsOrderItem);
            c.onItemBought+=RecordOrderItem;
            c.onDealAccepted+=()=>
            {
                if(Number("order.state")!=2)return;
                // Native BUY completion checks remaining budget, not just the
                // CompleteBusiness flag. Stop buying once this order is filled.
                c.OverrideBudget(0);c.CompleteBusiness();
            };
            c.OnArrival+=()=>{if(Number("order.first")==0)Set("order.first",MaidState.Today);};
            c.mainDialogue=Say("order_collect",Number("order.tools"),Number("order.materials"),
                Math.Max(0,Number("order.budget",600)-Number("order.spent")));
            StoryEngine.InterceptDismiss(c,()=>MaidInteractions.WithLight(MaidDividends.WithReceipt(Say(Number("order.state")==2?"order_done":
                Number("order.spent")>=Number("order.budget",600)?"order_budget":"order_partial"))),()=>
            {
                if(Number("order.spent")>=Number("order.budget",600) && Number("order.state")==1)Set("order.state",4);
                Set("order.next",MaidState.Today+2);Complete(CollectVisit);
            });
            c.CompleteClientCreation();return c;
        }

        public static void ConfigureSupply(StoreClient c,Dialogue opening,Func<Dialogue> ending,Action complete)
        {
            c.clientIntent=StoreClient.ClientIntent.SELL;c.SetWholesale(10);
            Action finish=()=>{if(!Done("supply.finished")){Mark("supply.finished");complete();}};
            StoryEngine.InterceptDismiss(c,ending,finish);
            if(Number("supply.choice")!=0)
            {
                StoryEngine.Tail(opening).SetNextDialogue(Say("supply_resume").SetEndAction(()=>SpawnSupply(c)));
                return;
            }
            StoryEngine.Tail(opening).SetNextDialogue(Say("supply_offer"));
            var trade=Say("supply_trade");trade.SetEndAction(()=>SpawnSupply(c));
            var skip=MaidText.Finish(ending(),finish);
            StoryEngine.AddChoices(opening,L.Get("er_biz_supply_prompt"),
                new StoryEngine.Choice(L.Get("er_biz_supply_tools"),L.Get("er_biz_normal_prices"),trade,()=>ChooseSupply(1)),
                new StoryEngine.Choice(L.Get("er_biz_supply_materials"),L.Get("er_biz_normal_prices"),trade,()=>ChooseSupply(2)),
                new StoryEngine.Choice(L.Get("er_biz_supply_no"),L.Get("er_maid_choice_no_cost"),skip,()=>ChooseSupply(3)));
        }
        private static void ChooseSupply(int choice){if(Number("supply.choice")==0)Set("supply.choice",choice);}
        private static void SpawnSupply(StoreClient c)
        {
            if(Number("supply.choice")==1)Stock(c,"screwdriver","wire_cutter");
            else if(Number("supply.choice")==2)Stock(c,"metal_ingot","scrap_metal");
        }
        public static void AttachSample(StoreClient c,Dialogue opening)
        {
            c.clientIntent=StoreClient.ClientIntent.SELL;c.SetWholesale(10);
            if(MaidProjects.Paid(3) && !MaidProjects.ResultShown(3))Mark("sample.entitled");
            c.onItemSold+=(item,paid)=>
            {
                if(item!=null && item.IsTag(SampleTag))
                {Mark("sample.claimed");item.RemoveItemFeatureByID("free");}
            };
            opening.SetStartAction(()=>
            {
                SpawnSample(c);
                if(Number("supply.choice")==2)Stock(c,"metal_ingot","scrap_metal");
                else Stock(c,"screwdriver","wire_cutter");
            });
        }
        private static void SpawnSample(StoreClient c)
        {
            if(!Done("sample.entitled") || Done("sample.claimed") || Done(c.identifier+".sample"))return;
            var item=DirectoryMaster.Item("screwdriver");
            item.EnableTag(SampleTag);item.AddItemFeature(ItemFeatureList.FreeFeature());
            item.shortDescription=L.Get("er_biz_sample_desc");
            PutStock(item);Mark(c.identifier+".sample");
        }
        public static void AttachRestock(StoreClient c)
        {
            Buyer(c,900,"restock.spent");
            c.mainDialogue=Say("restock_intro");
        }

        private static bool AvailableBusiness=>VersionGate.IsSupported && RomanceState.StoryActive &&
            !RomanceState.OldBlocked && !RomanceState.RomanceLocked && MaidState.SafeMother;
        public static bool NeedsFunding()
        {
            int p=MaidStory.WaitingProject();if(p==0)return false;
            int need=int.MaxValue;
            foreach(var plan in MaidProjects.Plans)
            {
                if(plan.Project!=p)continue;
                var q=MaidProjects.GetQuote(plan);
                need=Math.Min(need,q.Cash);
            }
            return need!=int.MaxValue && PlayerStore.Instance.GetCash()<need;
        }
        public static bool TryQueue(StoreClientManager manager,int day)
        {
            if(!AvailableBusiness)return false;
            int id=0;
            if(Number("order.state")==1 && Number("order.first")>0 && day>Number("order.first")+4)
            {Set("order.state",4);StoryDirector.Notify(L.Get("er_biz_order_expired"));}
            if(Number("order.state")==1 && day>=Number("order.next"))id=CollectVisit;
            else if(Done("sample.entitled") && !Done("sample.claimed") && MaidProjects.ResultShown(3) && day>=Number("sample.next"))id=SampleVisit;
            else if(MaidDividends.Due(day))id=MaidDividends.VisitId;
            else if(MaidState.Active && NeedsFunding() && day>=Number("routine.next") && day>=MaidState.LastImportantDay+2)
            {
                id=SellVisit+Number("routine.cycle")%2;
                if(id==SellVisit && PlayerStore.Instance.GetCash()<=0)id=BuyVisit;
                Set("routine.cycle",id-SellVisit);
                Set("routine.serial",Number("routine.serial")+1);
            }
            if(id==0)return false;
            var c=id==MaidDividends.VisitId?MaidDividends.CreateVisit():CreateEvent(id);if(c==null)return false;
            ModHelper.SetClientSource(c,ModConstants.ModId);manager.AddClient(c);
            if(manager.clientStack==null || !manager.clientStack.Contains(c))return false;
            MaidState.PendingVisit=id;MaidState.NextVisitDay=day;MaidState.LastImportantDay=day;RomanceState.LastQueuedDay=day;
            return true;
        }
        public static StoreClient CreateEvent(int id)
        {
            if(id==RetiredAppraiseVisit){OnLoaded();id=BuyVisit;}
            if(id==CollectVisit)return Collect();
            if(id==SampleVisit)
            {
                var c=EleanorStory.NewEleanorClient("biz_sample_"+MaidState.Today,"eleanor_scav",StoreClient.ClientIntent.SELL);
                c.mainDialogue=Say("sample_pickup");AttachSample(c,c.mainDialogue);
                // Pickup is the already-paid sample only, not another shop stock.
                c.mainDialogue.SetStartAction(()=>SpawnSample(c));
                StoryEngine.InterceptDismiss(c,()=>MaidInteractions.WithLight(MaidDividends.WithReceipt(Say(Done("sample.claimed")?"sample_close":"sample_later"))),
                    ()=>{Set("sample.next",MaidState.Today+7);Complete(id);});
                c.CompleteClientCreation();return c;
            }
            if(id<SellVisit || id>BuyVisit)return null;
            int serial=Number("routine.serial");
            var client=EleanorStory.NewEleanorClient("biz_routine_"+serial,"eleanor_scav",StoreClient.ClientIntent.SELL);
            Action done=()=>
            {Set("routine.next",MaidState.Today+4);Set("routine.cycle",Number("routine.cycle")+1);Complete(id);};
            client.mainDialogue=Say(id==SellVisit?"routine_sell":"routine_buy");
            if(id==SellVisit)client.mainDialogue.SetEndAction(()=>Stock(client,"scrap_metal","screwdriver"));
            else Buyer(client,350,"routine.spent."+serial);
            StoryEngine.InterceptDismiss(client,()=>MaidInteractions.WithLight(MaidDividends.WithReceipt(Say("routine_end"))),done);
            client.CompleteClientCreation();return client;
        }
        private static void Complete(int id)
        {
            if(MaidState.PendingVisit!=id)return;
            MaidState.PendingVisit=0;MaidState.NextVisitDay=-1;MaidState.LastImportantDay=MaidState.Today;
            MaidProjects.RefreshStates(true);
        }
    }
}
