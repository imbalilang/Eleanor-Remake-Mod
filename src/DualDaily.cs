using System;

namespace EleanorRomance
{
    public static class DualDaily
    {
        public static string Next(int day,int stage) => day >= DualState.LastDailyDay+4
            ? "wait_"+stage+"_"+(DualState.Int("daily.wait."+stage)%2+1) : null;
        public static StoreClient Create(string id)
        {
            int stage=id[5]-'0';
            var client=id=="wait_2_1"
                ? EleanorStory.NewEleanorClient("dual_"+id,"eleanor1",StoreClient.ClientIntent.DIALOGUE)
                : EleanorStory.NewNpcClient("dual_"+id,L.Get("er_name_lydia"),EleanorStory.LydiaSpriteKey(),EleanorStory.FactionKind.Upper);
            var root=DualText.Build(id);
            string progress=DualProgression.ProgressText(stage);
            if(!string.IsNullOrEmpty(progress))
            {
                Action previous=root.startAction;
                root.SetStartAction(()=>{previous?.Invoke();StoryDirector.Notify(progress);});
            }
            DualRewards.AppendClaims(root);
            DualText.FinishAll(root,()=>
            {
                DualState.SetInt("daily.wait."+stage,DualState.Int("daily.wait."+stage)+1);
                DualState.LastDailyDay=DualState.Today;
                DualDirector.CompleteScene(id);
            });
            client.mainDialogue=root;client.CompleteClientCreation();return client;
        }
    }
}
