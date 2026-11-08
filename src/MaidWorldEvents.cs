using System;
using System.Linq;
using HarmonyLib;

namespace EleanorRomance
{
    public static class MaidWorldEvents
    {
        public const string NewsId="er_maid_mother_acquitted";
        // 0 not submitted, 1 submitted, 2 news pending, 3 acquitted.
        public static int State { get=>MaidState.Int("world.state"); set => MaidState.SetInt("world.state",value); }
        public static bool Acquitted=>State==3;
        public static bool Eligible=>RomanceState.StoryActive && RomanceState.Route==2 && RomanceState.MotherState==1
            && RomanceState.ProtectionReady && RomanceState.DriveState==4 && RomanceState.BetrayalKind==0;
        public static bool NeedsReceipt=>Eligible && State==0;
        public static bool ChatPending=>Acquitted && !MaidState.Flag("world.chat_shown")
            && RomanceState.Route!=4 && !RomanceState.RomanceLocked && RomanceState.EpilogueDone;

        public static bool AppendReceipt(Dialogue head)
        {
            if(!NeedsReceipt)return false;
            MaidText.Append(head,"m23_b006");
            return true;
        }
        public static void SubmitReceipt()
        {
            if(!NeedsReceipt)return;
            MaidState.SetInt("world.submitted_day",MaidState.Today);
            MaidState.SetInt("world.due_day",MaidState.Today+7);
            State=1;
        }
        public static void TickNews(StoreEventManager manager)
        {
            if(!RomanceState.StoryActive || manager==null || State==0 || Acquitted)return;
            if(MaidState.Today<MaidState.Int("world.due_day",int.MaxValue))return;
            State=2;
            try
            {
                if(manager.IsEventActive(NewsId)){ State=3; return; }
                // Native urgent news keeps its day. No removal or priority override.
                var active=manager.GetActiveEvents();
                if(active!=null && active.Any(e=>e!=null && e.startDay==MaidState.Today
                    && (e.IsPriority || e.eventType==StoreEvent.EventType.THREAT || e.importance>=9)))return;
                var news=new StoreEvent { identifier=NewsId,newsName=L.Get("er_maid_news_title"),
                    displayName=L.Get("er_maid_news_title"),newsDescription=MaidText.Plain("world_b003"),
                    duration=1,eventType=StoreEvent.EventType.COSMETIC,eventArea=StoreEvent.EventArea.ALL,
                    importance=8,cooldown=0 };
                manager.ActivateEvent(news,false,false);
                if(manager.IsEventActive(NewsId)) State=3; // Published in the native newspaper, not merely queued.
            }
            catch(Exception ex){ ModEntry.LogError("Acquittal news remains pending: "+ex); }
        }
        public static void AppendChat(Dialogue head)
        {
            MaidText.Append(head,"world_b004");
            if(MaidState.Progress>=27 && !MaidState.Flag("mother_at_27"))MaidText.Append(head,"world_b005");
        }
        public static void CompleteChat()=>MaidState.Flag("world.chat_shown",true);
        public static string Ending()
        {
            if(State==0)return ""; // No retrospective submission for old completed runs.
            return Acquitted ? MaidText.Plain("ending_b004",MaidState.Lovers && MaidState.Progress>=28 ? 0 : 1)
                : MaidText.Plain("ending_b005",0);
        }
    }
    [HarmonyPatch(typeof(StoreEventManager),"OnDayStart")]
    internal static class MaidWorldDayPatch
    { private static void Postfix(StoreEventManager __instance)=>MaidWorldEvents.TickNews(__instance); }
}
