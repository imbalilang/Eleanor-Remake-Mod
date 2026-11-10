using System;
using System.Linq;
namespace EleanorRomance
{
    public static class DualRewards
    {
        public static readonly string[] ItemIds = { ModConstants.PlaqueItemId, ModConstants.PatronItemId, ModConstants.GratitudeItemId };
        public static string Name(int s) => L.Get(s==1?"er_dual_plaque_name":s==2?"er_dual_patron_name":"er_dual_gratitude_name");
        // null means the supported storages could not be inspected. Never duplicate on an error.
        public static bool? Exists(int s)
        {
            try
            {
                if(PlayerStore.Instance==null || EmporiumEntry.Instance==null)return null;
                return PlayerStore.Instance.FindAllItem(false).Concat(EmporiumEntry.Instance.GetAllItems())
                    .Any(i=>i!=null && i.identifier==ItemIds[s-1] && GeneralHelper.IsItemOwned(i));
            }
            catch(Exception ex){ModEntry.LogError("Ornament scan unavailable: "+ex.Message);return null;}
        }
        public static bool Received(int s) => s==1 ? DualState.PlaqueReceived : DualState.Flag("reward."+s+".delivered");
        public static bool Ready(int s)
        {
            if(!DualState.Writable || !DualProgression.Earned(s))return false;
            bool? exists=Exists(s);
            if(exists==true){DualState.SetInt("reward."+s+".lost_day",-1);return false;}
            if(exists==null)return false;
            if(!Received(s))return true;
            string key="reward."+s+".lost_day";
            int day=DualState.Int(key,-1);
            if(day<0){DualState.SetInt(key,DualState.Today);return false;}
            return DualState.Today>day;
        }
        public static bool Give(int s)
        {
            if(!DualState.Writable || !DualProgression.Earned(s))return false;
            bool? exists=Exists(s);
            if(exists==null)return false;
            if(exists==true){DualState.Flag("reward."+s+".delivered",true);if(s==1)DualState.PlaqueReceived=true;return true;}
            if(Received(s) && !Ready(s))return false;
            if(!StoryEngine.GiveToPlayer(ItemIds[s-1])){DualState.Flag("reward."+s+".pending",true);return false;}
            DualState.Flag("reward."+s+".delivered",true);
            DualState.Flag("reward."+s+".pending",false);
            DualState.SetInt("reward."+s+".lost_day",-1);
            if(s==1){DualState.PlaqueReceived=true;DualState.PlaqueLost=false;DualState.PlaqueRetryDay=-1;}
            StoryDirector.Notify(L.Get("er_dual_reward_received",Name(s)));
            return true;
        }
        public static bool NeedsClaim => Enumerable.Range(1,3).Any(Ready) || StoryCoreFacts.PerfumePending || StoryCoreFacts.LocketPending || MaidWorldEvents.NeedsReceipt && StoryCoreFacts.Done(24);
        public static void AppendClaims(Dialogue root)
        {
            for(int s=1;s<=3;s++)
            {
                if(!Ready(s))continue;
                int stage=s;
                var claim=StoryEngine.Text(L.Get("er_name_lydia"),L.Get("er_dual_claim_line",Name(s)));
                claim.SetEndAction(()=>{if(!Give(stage))StoryDirector.Notify(L.Get("er_dual_plaque_full"));});
                StoryEngine.Tail(root).SetNextDialogue(claim);
            }
            if(StoryCoreFacts.PerfumePending || MaidWorldEvents.NeedsReceipt && StoryCoreFacts.Done(24))
            {
                var claim=StoryEngine.Text(L.Eleanor,L.Get("er_dual_perfume_claim"));
                claim.SetEndAction(()=>{StoryEngine.GiveSelfRefillingPerfume();if(!RomanceState.PerfumeGiven)StoryDirector.Notify(L.Get("er_dual_plaque_full"));});
                if(MaidWorldEvents.AppendReceipt(claim))StoryEngine.Tail(claim).SetEndAction(MaidWorldEvents.SubmitReceipt);
                StoryEngine.Tail(root).SetNextDialogue(claim);
            }
            if(StoryCoreFacts.LocketPending)
            {
                var locket=StoryEngine.Build(StoryV06Content.B_C22_B007);
                var accept=StoryEngine.Build(StoryV06Content.B_C22_B008);
                var refuse=StoryEngine.Build(StoryV06Content.B_C22_B009);
                StoryEngine.Tail(accept).SetEndAction(()=>StoryCoreFacts.FinishLocket(false));
                StoryEngine.Tail(refuse).SetEndAction(()=>StoryCoreFacts.FinishLocket(true));
                MaidStory.GateOriginalLocket(locket,accept,refuse);
                StoryEngine.Tail(root).SetNextDialogue(locket);
            }
        }
    }
}
