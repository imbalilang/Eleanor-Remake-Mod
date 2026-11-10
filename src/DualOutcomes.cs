using System;
namespace EleanorRomance
{
    public static class DualOutcomes
    {
        public static string Ending(string description)
        {
            if(!DualState.Writable || !DualState.HasHistory || DualState.Ended)return null;
            string block=DualState.Confirmed ? DualState.Done("dr20")?"v05_ending_complete":"v05_ending_confirmed":"v05_ending_discussing";
            string text=DualText.Plain(block);
            string native=description??"";
            string suffix=Contains(native,"死亡","死了","dead","died","killed")?"dr_epilogue_death"
                :Contains(native,"被捕","监禁","prison","arrest","captured","detained")?"dr_epilogue_arrested"
                :Contains(native,"离开","远行","流放","leave","depart","exile")?"dr_epilogue_departure":null;
            if(suffix!=null)text+="\n\n"+DualText.Plain(suffix);
            return text;
        }
        private static bool Contains(string text,params string[] words)
        {foreach(var word in words)if(text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0)return true;return false;}
    }
}
