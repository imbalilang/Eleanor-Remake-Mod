using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
namespace EleanorRomance
{
    public static class DualShowcaseEffects
    {
        public static bool Enabled;
        public sealed class Visit
        {
            public StoreClient Client;
            public int Id,BaseBudget,Remaining,Sales,Goods,TipPaid;
            public bool Plaque,Budget,Tips;
        }
        public sealed class Sale
        {
            public Visit Visit;
            public GameItem Item;
            public int Paid;
            public bool Captured,Done;
        }
        private static readonly Dictionary<StoreClient,Visit> visits=new Dictionary<StoreClient,Visit>();
        private static readonly Stack<Sale> sales=new Stack<Sale>();
        private static int busy;
        private static bool deferredSave;
        public static void ClearMemory(){visits.Clear();sales.Clear();busy=0;paying=false;deferredSave=false;}
        public static bool Eligible(StoreClient c)
        {
            if(c==null || c.clientIntent!=StoreClient.ClientIntent.BUY || !c.useClientBudget)return false;
            if(c.sourceModId==ModConstants.ModId)return (c.identifier??"").StartsWith("er_story_dual_referral_",StringComparison.Ordinal);
            if(!string.IsNullOrEmpty(c.sourceModId))return false;
            return c.identifier=="upperLowerVisitor" || c.identifier=="upperLowerVisitorLuxury" || c.identifier=="ulDiscerningShopper";
        }
        public static bool Placed(int stage)
        {
            if(!RomanceState.StoryActive || !DualState.Writable || !DualProgression.Earned(stage))return false;
            return DualBusiness.ShowcaseItems().Any(i=>i.identifier==DualRewards.ItemIds[stage-1] && GeneralHelper.IsItemOwned(i));
        }
        public static void Arrive(StoreClient c)
        {
            if(!Enabled || !DualState.Writable || !RomanceState.StoryActive || !Eligible(c) || visits.ContainsKey(c))return;
            var v=new Visit{Client=c,Id=DualState.Int("visit.sequence")+1,BaseBudget=c.GetBudget(),Plaque=Placed(1),Budget=Placed(2),Tips=Placed(3)};
            if(v.Id<=0)v.Id=1;
            v.Remaining=v.Budget ? (int)Math.Min(int.MaxValue,(long)Math.Max(0,v.BaseBudget)*13/10) : v.BaseBudget;
            visits.Add(c,v);DualState.SetInt("visit.sequence",v.Id);
            c.OverrideBudget(v.Remaining);Save(v);FlushPending();
        }
        private static void Save(Visit v)
        {
            DualState.SetInt("visit.active",v.Id);DualState.Text("visit.kind",v.Client.identifier);
            DualState.SetInt("visit.base",v.BaseBudget);DualState.SetInt("visit.remaining",v.Client.GetBudget());
            DualState.Flag("visit.plaque",v.Plaque);
            DualState.Flag("visit.budget",v.Budget);DualState.Flag("visit.tips",v.Tips);
            DualState.SetInt("visit.sales",v.Sales);DualState.SetInt("visit.goods",v.Goods);DualState.SetInt("visit.tip_paid",v.TipPaid);
        }
        public static void OnLoaded()
        {
            if(!RomanceState.StoryActive || !DualState.Writable){ClearMemory();return;}
            var c=PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            Visit v;
            if(c!=null && visits.TryGetValue(c,out v) && v.Id==DualState.Int("visit.active"))
            {
                v.BaseBudget=DualState.Int("visit.base");v.Remaining=DualState.Int("visit.remaining");
                v.Sales=DualState.Int("visit.sales");v.Goods=DualState.Int("visit.goods");v.TipPaid=DualState.Int("visit.tip_paid");
                v.Plaque=DualState.Flag("visit.plaque");v.Budget=DualState.Flag("visit.budget");v.Tips=DualState.Flag("visit.tips");
                c.OverrideBudget(v.Remaining);return;
            }
            ClearMemory();
            if(DualState.Writable)DualState.SetInt("visit.active",0);
            // Vanilla does not serialize the current ordinary customer. Do not resurrect it.
        }
        public static Sale BeginSale(GameItem item)
        {
            if(!Enabled || !RomanceState.StoryActive || !DualState.Writable)return null;
            var c=PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            Visit v;
            if(c==null || !visits.TryGetValue(c,out v) || item==null || !GeneralHelper.IsItemOwned(item))return null;
            var sale=new Sale{Visit=v,Item=item};sales.Push(sale);busy++;return sale;
        }
        public static void Capture(StoreClient c,GameItem item,int amount)
        {
            if(sales.Count==0)return;
            var s=sales.Peek();
            if(!s.Captured && s.Visit.Client==c && s.Item==item)
            {s.Paid=Math.Max(0,amount);s.Captured=true;}
        }
        public static void EndSale(Sale s,bool success)
        {
            if(s==null || s.Done)return;
            s.Done=true;
            try
            {
                if(success && s.Captured && !GeneralHelper.IsItemOwned(s.Item))
                {
                    var v=s.Visit;v.Sales++;
                    v.Goods=(int)Math.Min(int.MaxValue,(long)v.Goods+s.Paid);
                    int due=v.Tips ? Math.Min(150,v.Goods/10) : 0;
                    int delta=Math.Max(0,due-v.TipPaid);
                    if(delta>0)
                    {
                        long pending=Math.Min(long.MaxValue-delta,Pending())+delta;
                        DualState.Text("tip.pending",pending.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        v.TipPaid=due;
                    }
                    v.Remaining=v.Client.GetBudget();Save(v);FlushPending();DualProgression.Check();
                }
            }
            finally
            {
                if(sales.Count>0 && sales.Peek()==s)sales.Pop();
                busy=Math.Max(0,busy-1);
                if(busy==0 && deferredSave)SaveDeferred();
            }
        }
        private static long Pending(){long n;return long.TryParse(DualState.Text("tip.pending"),out n)?Math.Max(0,n):0;}
        private static bool paying;
        public static void FlushPending()
        {
            if(paying || !RomanceState.StoryActive || !DualState.Writable || PlayerStore.Instance==null)return;
            long pending=Pending();int before=PlayerStore.Instance.GetCash();
            int pay=(int)Math.Min(int.MaxValue,Math.Min(pending,Math.Max(0,(long)int.MaxValue-before)));
            if(pay<=0)return;
            paying=true;busy++;
            try
            {
                try { PlayerStore.Instance.ModCash(pay,false); }
                catch(Exception ex){ModEntry.LogError("Tip cash observer failed; preserving actual payment: "+ex.Message);}
                int actual=(int)Math.Min(pay,Math.Max(0,(long)PlayerStore.Instance.GetCash()-before));
                DualState.Text("tip.pending",(pending-actual).ToString(System.Globalization.CultureInfo.InvariantCulture));
                if(actual>0)StoryDirector.Notify(L.Get("er_dual_tip_paid",actual));
            }
            finally
            {
                paying=false;busy=Math.Max(0,busy-1);
                if(busy==0 && deferredSave)SaveDeferred();
            }
        }
        public static void Close(StoreClient c)
        {
            if(c==null)return;Visit v;
            if(visits.TryGetValue(c,out v))
            {visits.Remove(c);if(DualState.Int("visit.active")==v.Id)DualState.SetInt("visit.active",0);}
            DualBusiness.OnArrivedOrLeft(c,false);
        }
        private static void SaveDeferred()
        {
            deferredSave=false;
            try{PlayerStore.Instance?.SaveGame();}
            catch(Exception ex){ModEntry.LogError("Committed sale save failed: "+ex.Message);StoryDirector.Notify(L.Get("er_maid_save_retry"));}
        }
        public static bool BeforeSave(){if(busy==0)return true;deferredSave=true;return false;}
    }
    [HarmonyPatch(typeof(StoreClientInstance),nameof(StoreClientInstance.CreateClientInstance))]
    internal static class DualBuyerArrivalPatch
    { private static void Postfix(StoreClient __0){DualShowcaseEffects.Arrive(__0);DualBusiness.OnArrivedOrLeft(__0,true);} }
    [HarmonyPatch(typeof(PlayerStore),nameof(PlayerStore.SellItem))]
    internal static class DualSalePatch
    {
        private static void Prefix(GameItem __0,out DualShowcaseEffects.Sale __state)=>__state=DualShowcaseEffects.BeginSale(__0);
        private static void Postfix(DualShowcaseEffects.Sale __state)=>DualShowcaseEffects.EndSale(__state,true);
        private static Exception Finalizer(Exception __exception,DualShowcaseEffects.Sale __state)
        {if(__exception!=null)DualShowcaseEffects.EndSale(__state,false);return __exception;}
    }
    [HarmonyPatch(typeof(StoreClient),nameof(StoreClient.OnItemBought))]
    internal static class DualGoodsPaymentPatch
    {private static void Postfix(StoreClient __instance,GameItem __0,int __1)=>DualShowcaseEffects.Capture(__instance,__0,__1);}
    [HarmonyPatch(typeof(PlayerStore),nameof(PlayerStore.DismissCurrentClient))]
    internal static class DualBuyerDeparturePatch
    {
        private static void Prefix(PlayerStore __instance,out StoreClient __state)=>__state=__instance.currentClientInstance?.GetClientBlueprint();
        private static void Postfix(StoreClient __state)=>DualShowcaseEffects.Close(__state);
    }
    [HarmonyPatch(typeof(PlayerStore),nameof(PlayerStore.SaveGame))]
    internal static class DualSaveBoundaryPatch {private static bool Prefix()=>DualShowcaseEffects.BeforeSave();}
    [HarmonyPatch(typeof(SaveManager),nameof(SaveManager.Save))]
    internal static class DualItemSaveBoundaryPatch {private static bool Prefix()=>DualShowcaseEffects.BeforeSave();}
    [HarmonyPatch(typeof(StoreReputation),"CheckReputation")]
    internal static class DualReputationPatch
    {private static void Postfix(){if(DualShowcaseEffects.Enabled)DualProgression.Check();}}
}
