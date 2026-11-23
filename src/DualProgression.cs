using System;
using System.Linq;
namespace EleanorRomance
{
    public static class DualProgression
    {
        public static readonly int[] Targets = { 30, 60, 90 };
        public static bool Earned(int stage) => DualState.Flag("reward." + stage + ".earned");

        public static bool TryReputation(out int value)
        {
            value = 0;
            if (PlayerStore.Instance?.storeReputations == null || !PlayerStore.Instance.storeReputations.Any(r=>r!=null && r.factionId=="FACTION_UPPER_LEVEL")) return false;
            try { value = StoreReputation.GetULReputation(); return true; }
            catch (Exception ex) { ModEntry.LogError("Upper reputation unavailable: " + ex.Message); return false; }
        }
        public static void Check()
        {
            if (!DualState.Active || !TryReputation(out int rep)) return;
            for (int stage = 1; stage <= Targets.Length; stage++)
            {
                if (Earned(stage) || rep < Targets[stage - 1]) continue;
                DualState.SetInt("reward." + stage + ".earned_day", DualState.Today);
                DualState.Flag("reward." + stage + ".earned", true);
            }
        }
    }
}
