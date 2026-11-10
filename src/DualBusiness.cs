using System;
using System.Collections.Generic;
using System.Linq;
namespace EleanorRomance
{
    public static class DualBusiness
    {
        public const int FirstBudget=500,SecondBudget=600,SecondMinDurability=150;
        public const string PurifierId="portable_water_purifier";
        public static IEnumerable<GameItem> ShowcaseItems()
        {
            try
            {
                if(EmporiumEntry.Instance?.showcaseElement==null)return Enumerable.Empty<GameItem>();
                return EmporiumEntry.Instance.showcaseElement.childItems
                    .Where(i=>i!=null && i.parentInventory==EmporiumEntry.Instance.showcaseElement).ToArray();
            }
            catch(Exception ex){ModEntry.LogError("Showcase unavailable: "+ex.Message);return Enumerable.Empty<GameItem>();}
        }
        public static bool PlaquePlaced()=>DualShowcaseEffects.Placed(1);
        public static bool PlaqueExists()=>DualRewards.Exists(1)==true;
        public static bool HasFirstItem()=>ShowcaseItems().Any(IsFirstItem);
        public static bool HasSecondItem()=>ShowcaseItems().Any(i=>i.identifier==PurifierId && DurabilityHelper.GetRealDurability(i)>=SecondMinDurability);
        private static bool IsFirstItem(GameItem i)=>i!=null && GeneralHelper.IsItemOwned(i)
            && !i.IsTag("IMPORTANT_TAG") && !i.IsTag("ER_KEEPSAKE") && !DualRewards.ItemIds.Contains(i.identifier)
            && (i.IsTag(TypeHelper.LUXURY_ITEM)||i.IsTag(TypeHelper.ART)||i.IsTag(TypeHelper.HIGHEND));
        private static bool Present(StoreClientManager m,string id)=>
            PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint()?.identifier==id
            || (m?.clientStack!=null && m.clientStack.Any(c=>c!=null && c.identifier==id));
        public static bool TryQueue(StoreClientManager manager,int day)
        {
            if(manager==null || !DualState.Writable || RomanceState.LastQueuedDay==day)return false;
            StoreClient client=null;
            if(StoryCoreFacts.CooperationPending && DualState.HasHistory)
                client=EleanorStory.CreateCooperationBuyer();
            else if(DualState.Flag("biz.legacy_second") && !DualState.SecondTraded && day>=DualState.BusinessDueDay)
                client=CreateSecondBuyer(DualState.Int("biz.second.serial")+1);
            else
            {
                string pending=DualState.Text("referral.pending_id");
                if(pending!="")
                {
                    if(Present(manager,pending))return false;
                    client=CreateFirstBuyer(DualState.Int("referral.serial"));
                }
                else
                {
                    if(!DualState.PlaqueActive || !PlaquePlaced() || !HasFirstItem())return false;
                    if(DualState.LastExtraBuyerDay>=0 && day<DualState.LastExtraBuyerDay+2)return false;
                    if(DualState.ReferralDay!=day)
                    {
                        DualState.ReferralDay=day;DualState.ReferralRoll=UnityEngine.Random.value<0.5f;
                        DualState.ReferralBlockedToday=false;
                    }
                    if(!DualState.ReferralRoll || DualState.ReferralBlockedToday)return false;
                    int serial=DualState.Int("referral.serial")+1;DualState.SetInt("referral.serial",serial);
                    client=CreateFirstBuyer(serial);
                }
            }
            if(client==null || Present(manager,client.identifier))return false;
            ModHelper.SetClientSource(client,ModConstants.ModId);manager.AddClient(client);
            if(manager.clientStack==null || !manager.clientStack.Contains(client))return false;
            if(client.identifier.StartsWith("er_story_dual_referral_",StringComparison.Ordinal))
            { DualState.Text("referral.pending_id",client.identifier);DualState.ReferralBlockedToday=true; }
            RomanceState.LastQueuedDay=day;return true;
        }
        public static StoreClient CreateFirstBuyer(int serial)
        {
            var c=EleanorStory.NewNpcClient("dual_referral_"+serial,L.Get("er_name_upper_buyer"),
                PortraitCompat.Resolve(SpriteDict.GetRandomUpperFemaleSpriteName()),EleanorStory.FactionKind.Upper);
            c.clientIntent=StoreClient.ClientIntent.BUY;
            c.clientBuyingTagList.Add(TypeHelper.LUXURY_ITEM);c.clientBuyingTagList.Add(TypeHelper.ART);c.clientBuyingTagList.Add(TypeHelper.HIGHEND);
            c.SetBudget(FirstBudget);c.SetVariety();c.willAcceptItem=IsFirstItem;
            c.mainDialogue=StoryEngine.Text(c.displayName,L.Get("er_dual_referral_intro"));
            StoryEngine.InterceptDismiss(c,()=>StoryEngine.Text(c.displayName,L.Get("er_dual_referral_close")),()=>{});
            c.CompleteClientCreation();return c;
        }
        // Compatibility only: a v0.81 order already accepted by the player.
        public static StoreClient CreateSecondBuyer(int serial)
        {
            var c=EleanorStory.NewNpcClient("dual_biz_second_"+serial,L.Get("er_name_upper_buyer"),
                PortraitCompat.Resolve(SpriteDict.GetRandomUpperMaleSpriteName()),EleanorStory.FactionKind.Upper);
            c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add(TypeHelper.HOUSEHOLD_GOOD);c.clientBuyingTagList.Add(TypeHelper.TOOL);
            c.SetBudget(SecondBudget);c.SetVariety();
            c.willAcceptItem=i=>i!=null && i.identifier==PurifierId && DurabilityHelper.GetRealDurability(i)>=SecondMinDurability;
            c.onItemBought+=(i,paid)=>{if(paid<0)return;DualState.SecondTraded=true;DualState.SecondBusiness=3;c.OverrideBudget(0);c.CompleteBusiness();};
            c.mainDialogue=StoryEngine.Text(c.displayName,L.Get("er_dual_biz_second_intro",SecondBudget,SecondMinDurability));
            StoryEngine.InterceptDismiss(c,()=>StoryEngine.Text(c.displayName,L.Get("er_dual_referral_close")),()=>
            {DualState.Flag("biz.legacy_second",false);if(!DualState.SecondTraded)DualState.SecondBusiness=4;});
            c.CompleteClientCreation();return c;
        }
        public static void OnArrivedOrLeft(StoreClient c,bool arrived)
        {
            if(!DualShowcaseEffects.Enabled || !DualState.Writable || c==null)return;
            if(c.identifier==DualState.Text("referral.pending_id") && arrived)
            {DualState.Text("referral.pending_id","");DualState.LastExtraBuyerDay=DualState.Today;}
        }
        public static bool CanReissuePlaque()=>DualRewards.Ready(1);
        public static void MarkPlaqueLost(){DualRewards.Ready(1);}
        public static bool GivePlaque()=>DualRewards.Give(1);
    }
}
