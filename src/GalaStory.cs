using System;
using System.Linq;

namespace EleanorRomance
{
    /// <summary>Ordinary visitor/dialogue epilogue. No close/travel/sleep interception.</summary>
    public static class GalaStory
    {
        public const int VisitId=916;
        public const string NewsId="er_gala_public_partner";
        public static int Phase { get=>MaidState.Int("gala.phase");set=>MaidState.SetInt("gala.phase",value); }
        // 0 available, 1 first invitation, 2 postponed, 3 booked, 4 playing,
        // 5 completed, 6 published, 7 solo reply pending, 8 closed, 9 second invitation.
        public static int Attempts=>MaidState.Int("gala.attempts");
        public static bool Eligible=>MaidInteractions.Active && MaidState.BasicComplete && MaidState.Progress>=28;
        public static void Initialize()
        {
            if(!RomanceState.StoryActive)return;
            if(MaidState.Int("gala.version")==0)
            {MaidState.SetInt("gala.version",1);MaidState.SetInt("gala.enabled_day",MaidState.Today);}
            if(Eligible && MaidState.Int("gala.ready_day",-1)<0)SetReadyDay(MaidState.Today+3);
        }
        public static void SetReadyDay(int day)
        {if(MaidState.Int("gala.ready_day",-1)<0)MaidState.SetInt("gala.ready_day",day);}
        public static bool Due(int day)
        {
            Initialize();
            return Eligible && (Phase==0 && day>=MaidState.Int("gala.ready_day",int.MaxValue)
                || Phase==1 || Phase==4 || Phase==3 && day>=MaidState.Int("gala.booked_day",int.MaxValue));
        }
        private static Dialogue Lines(string block,string cursor=null)
        {
            var lines=MaidContent.Get(block);Dialogue head=null,tail=null;
            int start=cursor==null?0:MaidState.Int("gala."+cursor);
            for(int i=start;i<lines.Length;i++)
            {
                var line=lines[i];int index=i+1;
                string speaker=line.Speaker=="Eleanor"?L.Eleanor:line.Speaker=="Player"?L.Player:
                    L.Get(line.Speaker=="Narrator"?"er_name_narrator":line.Speaker=="Guest"?"er_gala_guest":"er_gala_manager");
                var node=StoryEngine.Text(speaker,block=="gala_b001" && i==2?PortraitCompat.GalaDescription:L.Get(line.Key));
                if(cursor!=null)node.SetEndAction(()=>MaidState.SetInt("gala."+cursor,Math.Max(index,MaidState.Int("gala."+cursor))));
                if(head==null)head=node;else tail.SetNextDialogue(node);tail=node;
            }
            return head ?? StoryEngine.Text(L.Eleanor,L.Get("er_gala_wait_answer"));
        }
        private static void FinishVisit()
        {
            if(MaidState.PendingVisit==VisitId){MaidState.PendingVisit=0;MaidState.NextVisitDay=-1;}
            MaidState.LastImportantDay=MaidState.Today;StoryEngine.Dismiss();
        }
        private static void Finish(Dialogue head,Action commit=null)
        {
            var tail=StoryEngine.Tail(head);var previous=tail.endAction;bool done=false;
            tail.SetEndAction(()=>{if(done)return;done=true;previous?.Invoke();commit?.Invoke();FinishVisit();});
        }
        private static void BeginBall()
        {if(Phase==1 || Phase==3){Phase=4;MaidState.SetInt("gala.ball_line",0);}}
        private static Dialogue Ball()
        {
            var root=Lines("gala_b004","ball_line");
            Finish(root,()=>{if(Phase!=4)return;Phase=5;MaidState.SetInt("gala.completed_day",MaidState.Today);});
            return root;
        }
        public static StoreClient CreateVisit()
        {
            if(!Eligible)return null;
            var c=EleanorStory.NewEleanorClient("gala","eleanor1",StoreClient.ClientIntent.DIALOGUE);
            c.spriteName=PortraitCompat.ResolveGala(c.identifier);
            Dialogue root;
            if(Phase==4)root=Ball();
            else if(Phase==3)
            {
                root=Lines(MaidState.Today>MaidState.Int("gala.booked_day")?"gala_b009":"gala_b008");
                var no=Lines("gala_b010");Finish(no);
                StoryEngine.AddChoices(root,L.Get("er_gala_prompt"),
                    new StoryEngine.Choice(L.Get("er_gala_go"),L.Get("er_gala_free"),Ball(),BeginBall),
                    new StoryEngine.Choice(L.Get("er_gala_cancel"),L.Get("er_gala_decline_desc"),no,()=>Solo()));
            }
            else if(Phase==2 || Phase==7)
            {root=Lines(Phase==2?"gala_b003":"gala_b010");Finish(root);}
            else
            {
                root=Lines("gala_b001","invite_line");
                root.SetStartAction(()=>{if(Phase==0){Phase=1;MaidState.SetInt("gala.attempts",1);}});
                var accept=Lines("gala_b002");StoryEngine.Tail(accept).SetNextDialogue(Ball());
                var decline=Lines("gala_b003");Finish(decline);
                StoryEngine.AddChoices(root,L.Get("er_gala_prompt"),
                    new StoryEngine.Choice(L.Get("er_gala_accept"),L.Get("er_gala_free"),accept,BeginBall),
                    new StoryEngine.Choice(L.Get("er_gala_later"),L.Get("er_gala_later_desc"),decline,()=>
                    {if(Phase==1){Phase=2;MaidState.SetInt("gala.retry_day",MaidState.Today+7);}}));
            }
            c.mainDialogue=root;c.CompleteClientCreation();return c;
        }
        private static void Solo()
        {if(Phase==9 || Phase==3){Phase=7;MaidState.SetInt("gala.solo_day",MaidState.Today+1);}}
        public static Dialogue Light(Dialogue closing)
        {
            if(!Eligible)return null;
            Dialogue root=null;
            if((Phase==2 && Attempts<2 && MaidState.Today>=MaidState.Int("gala.retry_day",int.MaxValue)) || Phase==9)
            {
                root=Lines("gala_b007");
                root.SetStartAction(()=>{if(Phase==2){Phase=9;MaidState.SetInt("gala.attempts",2);}});
                var yes=StoryEngine.Text(L.Eleanor,L.Get("er_gala_booked"));yes.SetNextDialogue(closing);
                var no=Lines("gala_b010");StoryEngine.Tail(no).SetNextDialogue(closing);
                StoryEngine.AddChoices(root,L.Get("er_gala_prompt"),
                    new StoryEngine.Choice(L.Get("er_gala_book"),L.Get("er_gala_book_desc"),yes,()=>
                    {if(Phase==9){Phase=3;MaidState.SetInt("gala.booked_day",MaidState.Today+1);}}),
                    new StoryEngine.Choice(L.Get("er_gala_no_booking"),L.Get("er_gala_decline_desc"),no,Solo));
            }
            else if(Phase==7 && MaidState.Today>=MaidState.Int("gala.solo_day",int.MaxValue))
            {root=Lines("gala_b005");StoryEngine.Tail(root).SetEndAction(()=>{if(Phase==7)Phase=8;});}
            else if(Phase==6 && !MaidState.Flag("gala.reply"))
            {root=Lines("gala_b006");StoryEngine.Tail(root).SetEndAction(()=>MaidState.Flag("gala.reply",true));}
            return root;
        }
        public static void TickNews(StoreEventManager manager)
        {
            if(!Eligible || Phase!=5 || manager==null || MaidState.Today<=MaidState.Int("gala.completed_day",int.MaxValue))return;
            try
            {
                if(manager.IsEventActive(NewsId)){Phase=6;return;}
                // Generation may run before the mother-news daily callback.
                // Respect its due/pending state as well as already active news.
                if(MaidWorldEvents.State==2 || MaidWorldEvents.State==1
                    && MaidState.Today>=MaidState.Int("world.due_day",int.MaxValue))return;
                var active=manager.GetActiveEvents();
                if(active!=null && active.Any(e=>e!=null && (e.IsPriority || e.eventType==StoreEvent.EventType.THREAT
                    || e.importance>=8 || e.identifier==MaidWorldEvents.NewsId)))return;
                manager.ActivateEvent(new StoreEvent {identifier=NewsId,newsName=L.Get("er_gala_news_title"),
                    displayName=L.Get("er_gala_news_title"),newsDescription=L.Get("er_gala_news_body"),
                    duration=1,cooldown=0,importance=3,eventType=StoreEvent.EventType.COSMETIC,eventArea=StoreEvent.EventArea.UPPER},false,false);
                if(manager.IsEventActive(NewsId))Phase=6;
            }
            catch(Exception ex){ModEntry.LogError("Gala news remains pending: "+ex);}
        }
    }
}
