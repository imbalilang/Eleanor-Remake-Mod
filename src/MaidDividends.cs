using System;
using System.Globalization;
using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>Accrue by game day; settle only during Eleanor's own visits.</summary>
    public static class MaidDividends
    {
        public const int VisitId=915, FirstOrderRate=200, EstablishedRate=600, DeliveryInterval=4;
        private const string Prefix="dividend.";
        private static bool settling;
        private static long Read(string key)
            =>long.TryParse(MaidState.Text(Prefix+key),NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)?Math.Max(0,n):0;
        private static void Write(string key,long n)=>MaidState.Text(Prefix+key,Math.Max(0,n).ToString(CultureInfo.InvariantCulture));
        public static long Pending=>Read("pending");
        public static long TotalEarned=>Read("earned");
        public static long TotalPaid=>Read("paid");
        public static int LastDay=>MaidState.Int(Prefix+"day",-1);
        public static int NextDelivery=>MaidState.Int(Prefix+"next",int.MaxValue);
        private static bool Cooperative=>VersionGate.IsSupported && RomanceState.StoryActive && !RomanceState.OldBlocked
            && MaidState.HasHistory && MaidState.SafeMother && !RomanceState.RomanceLocked;
        public static int DailyRate
        {
            get
            {
                if(!Cooperative || !MaidProjects.Paid(3) || !MaidProjects.ResultShown(3) || MaidState.Progress<25)return 0;
                return MaidProjects.Paid(4) && MaidProjects.ResultShown(4) && MaidState.Progress>=27?EstablishedRate:FirstOrderRate;
            }
        }
        public static Dialogue Say(string key,params object[] args)=>StoryEngine.Text(L.Eleanor,L.Get("er_div_"+key,args));

        public static void Accrue()
        {
            if(settling || !VersionGate.IsSupported || !RomanceState.StoryActive || RomanceState.OldBlocked)return;
            int day=MaidState.Today,rate=DailyRate,last=LastDay;
            if(last<0)
            {
                if(rate==0)return;
                // Existing completed saves start here; no invented historical profit.
                MaidState.SetInt(Prefix+"day",day);MaidState.SetInt(Prefix+"rate",rate);
                MaidState.SetInt(Prefix+"next",day+DeliveryInterval);return;
            }
            if(day<last)return; // Never rewind the accrual cursor.
            int previous=MaidState.Int(Prefix+"rate");
            long addition=((long)day-last)*Math.Max(0,previous);
            if(addition>0)
            {
                Write("pending",checked(Pending+addition));
                Write("earned",checked(TotalEarned+addition));
            }
            MaidState.SetInt(Prefix+"day",day);
            MaidState.SetInt(Prefix+"rate",rate);
        }

        public static int PayableNow()
        {
            var store=PlayerStore.Instance;
            if(store==null)return 0;
            long room=(long)int.MaxValue-store.playerCash;
            return (int)Math.Max(0,Math.Min(Pending,Math.Min(int.MaxValue,room)));
        }
        public static int Collect(int maximum=int.MaxValue)
        {
            if(settling || !Cooperative || maximum<=0)return 0;
            Accrue();int amount=Math.Min(maximum,PayableNow());if(amount<=0)return 0;
            var store=PlayerStore.Instance;int beforeCash=store.playerCash;
            long beforePending=Pending,beforePaid=TotalPaid;
            settling=true;
            try
            {
                // No callbacks, I/O or yields within these writes. Native appraisal
                // fees also mutate playerCash directly. Commit cash last, so UI
                // feedback cannot credit money and then fail before its receipt.
                int afterCash=checked(beforeCash+amount);
                Write("pending",beforePending-amount);Write("paid",checked(beforePaid+amount));
                MaidState.SetInt(Prefix+"next",MaidState.Today+DeliveryInterval);
                store.playerCash=afterCash;
            }
            catch
            {
                Write("pending",beforePending);Write("paid",beforePaid);store.playerCash=beforeCash;
                throw;
            }
            finally{settling=false;}
            try{StoryDirector.Notify(L.Get("er_div_received",amount,Pending));}
            catch(Exception ex){ModEntry.LogError("Dividend paid; notification failed: "+ex);}
            return amount;
        }

        public static Dialogue WithReceipt(Dialogue head)
        {
            Accrue();
            if(!Cooperative || Pending<=0)return head;
            int amount=PayableNow();if(amount<=0)return head;
            var receipt=Say("receipt",amount);bool received=false;
            receipt.SetEndAction(()=>
            {
                if(received)return;
                if(Collect(amount)>0)received=true;
            });
            StoryEngine.Tail(head).SetNextDialogue(receipt);
            // Keep payment off the final node: other chapter finishers own it.
            receipt.SetNextDialogue(Say("receipt_end"));return head;
        }
        public static bool Due(int day)
        {
            Accrue();
            return Cooperative && Pending>0 && PayableNow()>0 && day>=NextDelivery
                && day>=MaidState.LastImportantDay+2;
        }
        public static StoreClient CreateVisit()
        {
            var c=EleanorStory.NewEleanorClient("dividend_"+MaidState.Today,"eleanor_scav",StoreClient.ClientIntent.DIALOGUE);
            var root=MaidInteractions.WithLight(WithReceipt(Say("visit")));
            MaidText.Finish(root,()=>
            {
                if(MaidState.PendingVisit!=VisitId)return;
                MaidState.PendingVisit=0;MaidState.NextVisitDay=-1;MaidState.LastImportantDay=MaidState.Today;
            });
            c.mainDialogue=root;c.CompleteClientCreation();return c;
        }
        public static string Summary()=>L.Get("er_div_summary",DailyRate,Pending,TotalPaid);
    }

    [HarmonyPatch(typeof(StoreEventManager),"OnDayStart")]
    internal static class MaidDividendDayPatch
    { private static void Postfix()=>MaidDividends.Accrue(); }
}
