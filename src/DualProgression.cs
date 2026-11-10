using System;
using System.Linq;
namespace EleanorRomance
{
    public static class DualProgression
    {
        public static readonly int[] Targets = { 50, 100, 150 };
        public static bool Passed(int stage) => DualState.Flag("gate." + stage + ".passed") || DualState.Flag("gate." + stage + ".waived");
        public static bool Earned(int stage) => DualState.Flag("reward." + stage + ".earned");
        public static bool Active(int stage) => DualState.Active && (stage == 1 ? DualState.Done("entry") : stage == 2
            ? DualState.Done("dr08") && RomanceState.RomanceConfirmed : DualState.Done("dr11") && DualState.CoreCommitted && !MaidWorldEvents.NeedsReceipt);
        public static bool TryReputation(out int value)
        {
            value = 0;
            if (PlayerStore.Instance?.storeReputations == null || !PlayerStore.Instance.storeReputations.Any(r=>r!=null && r.factionId=="FACTION_UPPER_LEVEL")) return false;
            try { value = StoreReputation.GetULReputation(); return true; }
            catch (Exception ex) { ModEntry.LogError("Upper reputation unavailable: " + ex.Message); return false; }
        }
        public static void Check()
        {
            if (!DualState.Writable || !RomanceState.StoryActive) return;
            int rep;
            if (!TryReputation(out rep)) return;
            for (int stage = 1; stage <= 3; stage++)
            {
                if (!Active(stage)) continue;
                bool firstActive=!DualState.Flag("gate." + stage + ".active");
                DualState.Flag("gate." + stage + ".active", true);
                if(firstActive && !Passed(stage) && rep<Targets[stage-1])
                    StoryDirector.Notify(L.Get("er_dual_gate_progress",rep,Targets[stage-1],DualRewards.Name(stage)));
                if (rep < Targets[stage - 1] || DualState.Flag("gate." + stage + ".passed")) continue;
                DualState.Flag("gate." + stage + ".passed", true);
                DualState.SetInt("gate." + stage + ".day", DualState.Today);
                DualState.Flag("reward." + stage + ".earned", true);
                StoryDirector.Notify(L.Get("er_dual_gate_passed", Targets[stage - 1]));
            }
        }
        public static int WaitingStage
        {
            get { for (int s = 1; s <= 3; s++) if (Active(s) && !Passed(s)) return s; return 0; }
        }
        public static string ProgressText(int stage)
        {
            int rep;
            return TryReputation(out rep) ? L.Get("er_dual_gate_progress", rep, Targets[stage - 1], DualRewards.Name(stage)) : "";
        }
    }
}
