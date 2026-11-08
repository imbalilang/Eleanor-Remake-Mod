using System;
using System.Text;

namespace EleanorRomance
{
    public static class MaidText
    {
        public static Dialogue Build(params string[] ids)
        {
            Dialogue head=null,tail=null;
            foreach (string id in ids)
            {
                var lines=MaidContent.Get(id);
                if (lines==null) throw new ArgumentException("Missing maid block: "+id);
                foreach (var line in lines)
                {
                    string speaker=line.Speaker=="Player" ? L.Player : line.Speaker=="Eleanor" ? L.Eleanor
                        : L.Get(line.Speaker=="Buyer" ? "er_name_upper_buyer" : "er_name_"+line.Speaker.ToLowerInvariant());
                    foreach(string paragraph in L.Get(line.Key).Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries))
                    {
                        Dialogue node=StoryEngine.Text(speaker,paragraph);
                        if(head==null) head=node; else tail.SetNextDialogue(node);
                        tail=node;
                    }
                }
            }
            return head;
        }
        public static Dialogue Append(Dialogue head,params string[] ids)
        { Dialogue next=Build(ids); StoryEngine.Tail(head).SetNextDialogue(next); return head; }
        public static string Plain(string id,int index=-1)
        {
            var lines=MaidContent.Get(id); if(lines==null)return "";
            if(index>=0) return index<lines.Length ? L.Get(lines[index].Key) : "";
            var result=new StringBuilder();
            foreach(var line in lines){ if(result.Length>0)result.Append("\n\n"); result.Append(L.Get(line.Key)); }
            return result.ToString();
        }
        public static Dialogue Finish(Dialogue head,Action commit)
        {
            bool done=false;
            StoryEngine.CompletionTail(head).SetEndAction(()=>{ if(done)return;done=true;commit?.Invoke();StoryEngine.Dismiss(); });
            return head;
        }
    }
}
