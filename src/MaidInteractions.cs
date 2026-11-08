using System;
using System.Linq;
using HarmonyLib;

namespace EleanorRomance
{
    public static class MaidInteractions
    {
        private const string P="interaction.";
        public static int Number(string key,int fallback=0)=>MaidState.Int(P+key,fallback);
        public static void Set(string key,int value)=>MaidState.SetInt(P+key,value);
        public static bool Flag(string key)=>MaidState.Flag(P+key);
        public static void Flag(string key,bool value)=>MaidState.Flag(P+key,value);
        public static bool Active=>RomanceState.StoryActive && MaidState.Lovers && MaidState.SafeMother && !RomanceState.RomanceLocked;
        public static void OnLoaded()
        {
            if(!RomanceState.StoryActive)return;
            if(Number("version")==0)
            {
                Set("version",1);Set("enabled_day",MaidState.Today);
                if(MaidProjects.ResultShown(3) && MaidState.Progress>=25)Set("first_order_day",MaidState.Today);
            }
            GalaStory.Initialize();
        }
        public static void OnProgress(int chapter)
        {
            OnLoaded();
            if(chapter==25 && Number("first_order_day",-1)<0)Set("first_order_day",MaidState.Today);
            if(chapter==28)GalaStory.SetReadyDay(MaidState.Today+3);
        }
        public static bool IsLesson(StoreClient c)=>c!=null && c.identifier=="er_story_maid_m19";
        public static Dialogue AppraisalIntro(StoreClient c)
        {
            if(!IsLesson(c))return MaidBusiness.Say("appraisal_intro");
            var root=MaidText.Build("interaction_b001");
            root.SetStartAction(()=>{if(!MaidBusiness.AppraisalPaid(c))Flag("lesson.opened",true);});
            return root;
        }
        public static void ConfigureLesson(StoreClient c,Dialogue wrap,Dialogue after)
        {
            bool built=false;
            wrap.SetStartAction(()=>
            {
                if(built || !Flag("lesson.opened"))return;
                built=true;bool accepted=MaidBusiness.AppraisalPaid(c);
                if(!accepted)after.SetText(L.Get("er_name_narrator"),L.Get("er_interaction_unfinished_papers"));
                var next=wrap.nextDialogue;
                var feedback=MaidText.Build(accepted?"interaction_b002":"interaction_b003");
                var tail=StoryEngine.Tail(feedback);
                tail.SetEndAction(()=>{if(Number("lesson.result")==0)Set("lesson.result",accepted?1:2);});
                tail.SetNextDialogue(next);wrap.SetNextDialogue(feedback);
            });
        }
        public static void AppendLessonEcho(Dialogue head)
        {
            int result=Number("lesson.result");
            if(result==0 || Flag("lesson.echo"))return;
            MaidText.Append(head,result==1?"interaction_b004":"interaction_b005");
            StoryEngine.Tail(head).SetEndAction(()=>Flag("lesson.echo",true));
        }
        private static Dialogue CustomerEcho()
        {
            int day=Number("first_order_day",-1);
            if(!Active || Flag("customer.shown") || day<0 || MaidState.Today<day+2 || !MaidProjects.ResultShown(3))return null;
            var echo=MaidText.Build("interaction_b006");
            StoryEngine.Tail(echo).SetEndAction(()=>Flag("customer.shown",true));return echo;
        }
        // Insert before the host's final line, preserving its original completion
        // action and leaving the new passage's exact-line commits intact.
        public static Dialogue WithLight(Dialogue head)
        {
            if(head==null || !Active)return head;
            OnLoaded();
            var closing=StoryEngine.Tail(head);
            var extra=GalaStory.Light(closing) ?? CustomerEcho();
            if(extra==null)return head;
            if(StoryEngine.Tail(extra).choices.Count==0 && StoryEngine.Tail(extra)!=closing)
                StoryEngine.Tail(extra).SetNextDialogue(closing);
            if(head==closing)return extra;
            var previous=head;while(previous.nextDialogue!=closing)previous=previous.nextDialogue;
            previous.SetNextDialogue(extra);return head;
        }
        public static bool MorningEntry=>Flag("morning.entry") && RomanceState.PendingChapter==17;
        private static void PresentMorning(StoreClient client)
        {
            var ui=StoreUIManager.Instance;
            ui.clientGameObject.image.sprite=SpriteDict.Instance.ShowClientSprite(client.GetSprite());
            ui.clientGameObject.SetImmediatlyArrive();
            PlayerStore.Instance.currentClientInstance=StoreClientInstance.CreateClientInstance(client);
            ui.RunMorningDialogue();
        }
        public static void ResolveMorning()
        {
            if(!RomanceState.StoryActive || !MaidState.Active || RomanceState.Route<1 || RomanceState.Route>2)return;
            if(StoryDirector.PendingChapterForDay(MaidState.Today)!=17)return;
            var store=PlayerStore.Instance;var manager=store?.storeClientManager;
            if(manager?.clientStack==null || store.currentClientInstance!=null || MaidStory.HasUrgentClient(manager))return;
            var queued=manager.clientStack.FirstOrDefault(c=>c!=null && c.identifier=="er_story_maid_m16");
            if(queued==null)return; // Ordinary scheduling owns eligibility and the fallback.
            try
            {
                Flag("morning.entry",true);
                var client=EleanorStory.CreateClient(17);
                if(client==null){Flag("morning.entry",false);return;}
                ModHelper.SetClientSource(client,ModConstants.ModId);
                PresentMorning(client);
                manager.clientStack.Remove(queued);
            }
            catch(Exception ex)
            {
                // Retain the ordinary queued copy and release only our partial
                // presentation, so a failed UI boundary cannot strand the story.
                if(store.currentClientInstance!=null && store.currentClientInstance.GetClientBlueprint()?.identifier=="er_story_maid_m16")
                    store.currentClientInstance=null;
                Flag("morning.entry",false);
                ModEntry.LogError("Morning reunion fallback: "+ex);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerStore),nameof(PlayerStore.OnLateShutterOpen))]
    internal static class InteractionMorningPatch
    { private static void Postfix()=>MaidInteractions.ResolveMorning(); }
}
