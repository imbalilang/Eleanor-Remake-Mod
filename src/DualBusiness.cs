using System;
using System.Collections.Generic;
using System.Linq;
namespace EleanorRomance
{
    public static class DualBusiness
    {
        public const int FirstBudget=500;
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
        private static bool IsFirstItem(GameItem i)=>i!=null && GeneralHelper.IsItemOwned(i)
            && !i.IsTag("IMPORTANT_TAG") && !i.IsTag("ER_KEEPSAKE") && !DualRewards.ItemIds.Contains(i.identifier)
            && (i.IsTag(TypeHelper.LUXURY_ITEM)||i.IsTag(TypeHelper.ART)||i.IsTag(TypeHelper.HIGHEND));
        private static bool Present(StoreClientManager m,string id)=>
            PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint()?.identifier==id
            || (m?.clientStack!=null && m.clientStack.Any(c=>c!=null && c.identifier==id));
        public static bool TryQueue(StoreClientManager manager,int day)
        {
            if(manager==null || !DualState.Writable || RomanceState.LastQueuedDay==day)return false;
            int serial = DualState.Int("referral.serial");
            string pending = DualState.Text("referral.pending_id");
            if (pending != "")
            {
                if (Present(manager, pending)) return false;
            }
            else
            {
                if (!DualState.PlaqueActive || !PlaquePlaced() || !HasFirstItem()) return false;
                if (DualState.LastExtraBuyerDay >= 0 && day < DualState.LastExtraBuyerDay + 2) return false;
                if (DualState.ReferralDay != day)
                {
                    DualState.ReferralDay = day;
                    DualState.ReferralRoll = UnityEngine.Random.value < 0.5f;
                    DualState.ReferralBlockedToday = false;
                }
                if (!DualState.ReferralRoll || DualState.ReferralBlockedToday) return false;
                DualState.SetInt("referral.serial", ++serial);
            }
            StoreClient client = CreateFirstBuyer(serial);
            if(client==null || Present(manager,client.identifier))return false;
            ModHelper.SetClientSource(client,ModConstants.ModId);manager.AddClient(client);
            if(manager.clientStack==null || !manager.clientStack.Contains(client))return false;
            DualState.Text("referral.pending_id",client.identifier);
            DualState.ReferralBlockedToday=true;
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
