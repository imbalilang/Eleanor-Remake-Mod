using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>Names authored by this mod, within the existing client bubble.</summary>
    public static class DialogueSpeakers
    {
        private static readonly ConditionalWeakTable<Dialogue,object> authored=new ConditionalWeakTable<Dialogue,object>();
        [ThreadStatic] private static Dialogue displaying;
        public static bool Enabled { get; set; }=true;

        public static Dialogue Mark(Dialogue line)
        {
            if(line!=null)authored.GetValue(line,_=>new object());
            return line;
        }
        public static bool HasOwnLabel(Dialogue line)
        {
            if(!Enabled || !VersionGate.IsSupported || line==null || string.IsNullOrEmpty(line.title)
                || line.title==L.Player || line.title==DialogUIManager.SEC)return false;
            if(authored.TryGetValue(line,out _))return true;
            // Older queued/restored graphs may predate the weak ownership map.
            // Accept only a verified mod visitor and one of our known labels.
            var client=PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            if(client==null || client.sourceModId!=ModConstants.ModId)return false;
            string label=line.title;
            return label==L.Eleanor || label==L.Get("er_name_narrator") || label==L.Get("er_name_mother")
                || label==L.Get("er_name_lydia") || label==L.Get("er_name_uncle") || label==L.Get("er_name_thug")
                || label==L.Get("er_name_upper_buyer") || label==L.Get("er_name_news")
                || label==L.Get("er_name_security_notice") || label==L.Get("er_name_officer")
                || label==L.Get("er_name_archivist") || label==L.Get("er_gala_guest") || label==L.Get("er_gala_manager");
        }
        public static Dialogue Begin(Dialogue line)
        {
            var previous=displaying;displaying=HasOwnLabel(line)?line:null;return previous;
        }
        public static void RewriteHistory(ref string text)
        {
            var line=displaying;
            if(!Enabled || line==null)return;
            var client=PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            string prefix=client==null?"<color=#eb4034>Customer :</color> ":"<color=#eb4034>"+client.displayName+":</color> ";
            // Only the current native rendering call's own log entry is ours.
            // Notifications or another mod's unrelated entries pass through.
            if(text==prefix+line.plainText)text="<color=#eb4034>"+line.title+":</color> "+line.plainText;
        }
        private static void ApplyCaption(DialogUIManager ui,string name)
        {
            if(ui.clientNameTMP!=null)ui.clientNameTMP.text=name;
            if(ui.clientNameBubble!=null)ui.clientNameBubble.SetActive(true);
        }
        public static void End(DialogUIManager ui,Dialogue line,Dialogue previous)
        {
            try
            {
                if(Enabled && line!=null && ReferenceEquals(displaying,line))ApplyCaption(ui,line.title);
            }
            catch(Exception ex){ModEntry.LogError("Dialogue caption update failed; native conversation continues: "+ex);}
            finally{displaying=previous;}
        }
        public static void Restore(Dialogue previous)=>displaying=previous;
    }

    [HarmonyPatch(typeof(DialogUIManager),nameof(DialogUIManager.DisplayClientText))]
    internal static class ModDialogueSpeakerPatch
    {
        private static void Prefix(Dialogue __0,out Dialogue __state)=>__state=DialogueSpeakers.Begin(__0);
        private static void Postfix(DialogUIManager __instance,Dialogue __0,Dialogue __state)=>DialogueSpeakers.End(__instance,__0,__state);
        private static Exception Finalizer(Exception __exception,Dialogue __state)
        {DialogueSpeakers.Restore(__state);return __exception;}
    }
    [HarmonyPatch(typeof(DialogUIManager),nameof(DialogUIManager.UpdateDialogHistory))]
    internal static class ModDialogueHistoryPatch
    {private static void Prefix(ref string __0)=>DialogueSpeakers.RewriteHistory(ref __0);}
}
