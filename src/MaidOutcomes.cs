using System;

namespace EleanorRomance
{
    public static partial class MaidStory
    {
        private static StoreClient CreateTragic(int code)
        {
            StoreClient c;
            if(code==106)c=EleanorStory.NewNpcClient("maid_c6",L.Get("er_name_lydia"),EleanorStory.LydiaSpriteKey(),EleanorStory.FactionKind.Upper);
            else c=Eleanor("c"+(code-100),normal:code>=104?"eleanor_rev":"eleanor3",upper:code<104);
            Dialogue root;
            switch(code)
            {
                case 101:
                    root=MaidText.Build("tragic_b002");FinishMain(root,101,102,()=>RomanceState.DriveState=5);break;
                case 102:
                    root=MaidText.Build("tragic_b004");
                    if(MaidState.EntryKind==2)
                    {
                        var tail=root;for(int i=0;i<9;i++)tail=tail.nextDialogue;
                        tail.SetNextDialogue(MaidText.Build("tragic_b005"));
                    }
                    else
                    {
                        // The final two common turns live with the replacement block.
                        var common=MaidText.Build("tragic_b005");common=common.nextDialogue.nextDialogue;StoryEngine.Tail(root).SetNextDialogue(common);
                    }
                    FinishMain(root,102,103,()=>{RomanceState.MotherState=2;MaidState.End("mother-died",3);StoryEngine.FreeNutrifruitForLeak();});break;
                case 103:
                    c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add("FOOD");c.SetBudget(40);
                    root=MaidText.Build("tragic_b006",RomanceState.LocketWithEleanor && !RomanceState.LocketSold?"tragic_b007":"tragic_b008");
                    StoryEngine.InterceptDismiss(c,()=>StoryEngine.Text(L.Eleanor,L.Get("er_maid_tragic_food_close")),()=>
                    {RomanceState.PendingChapter=103;EleanorStory.Advance(103,104);MaidState.LastImportantDay=MaidState.Today;});break;
                case 104:
                    root=MaidText.Build("tragic_b009");FinishMain(root,104,105,()=>RomanceState.WorkState=3);break;
                case 105:
                    c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add("WEAPON");c.SetBudget(350);
                    root=MaidText.Build("tragic_b010");
                    StoryEngine.InterceptDismiss(c,()=>MaidText.Build("tragic_b012"),()=>
                    {RomanceState.PendingChapter=105;EleanorStory.Advance(105,106);MaidState.LastImportantDay=MaidState.Today;});break;
                case 106:root=MaidText.Build("tragic_b013");FinishMain(root,106,107);break;
                case 107:root=MaidText.Build("tragic_b014");FinishMain(root,107,108);break;
                default:
                    c.clientIntent=StoreClient.ClientIntent.BUY;c.clientBuyingTagList.Add("FOOD");c.SetBudget(120);
                    root=MaidText.Build("tragic_b015");
                    StoryEngine.InterceptDismiss(c,()=>MaidText.Build("tragic_b016"),()=>
                    {StoryEngine.GiveSelfRefillingPerfume();RomanceState.WorkState=3;StoryDirector.CompleteCore();MaidState.LastImportantDay=MaidState.Today;});break;
            }
            return Ready(c,root);
        }
        private static StoreClient CreateExit()
        {
            var c=Eleanor("exit",normal:"eleanor3");
            int kind=RomanceState.BetrayalKind;
            var root=kind==4?StoryEngine.Build(StoryV06Content.B_RD_B005)
                : kind==3?MaidText.Build("exit_b008") : MaidText.Build("exit_b005","exit_b006");
            // Preserve the original route-D facts; normal withdrawal never invents a death.
            if(kind==1)StoryEngine.Append(root,StoryV06Content.B_RD_B002);
            else if(kind==2 || kind==3)
                StoryEngine.Tail(root).SetNextDialogue(StoryEngine.Text(L.Get("er_name_narrator"),L.Get("er_maid_betrayal_mother_died")));
            MaidText.Finish(root,()=>
            {
                MaidState.Contract=2;RomanceState.Romance=4;
                if(kind==1)RomanceState.MotherState=3;
                if(kind==2)RomanceState.MotherState=2;
                if(kind==3){RomanceState.MotherState=2;StoryEngine.FreeNutrifruitForLeak();}
                if(kind==4)RomanceState.DriveState=RomanceState.DriveState==1?2:RomanceState.DriveState;
                StoryDirector.CompleteCore();
            });return Ready(c,root);
        }
    }
}
