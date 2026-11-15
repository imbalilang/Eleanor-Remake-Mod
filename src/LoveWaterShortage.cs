using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>
    /// The love route's Act 2 water-shortage day must have a real water
    /// shortage. Force the native event on the day love_a2_01 is due, for one
    /// day only, so the next morning's price-tag scene can say the pipe is fixed.
    /// </summary>
    internal static class LoveWaterShortage
    {
        private const string EventId = "water_treatment_breakdown";
        internal static bool Enabled { get; set; }

        internal static void Ensure(StoreEventManager manager)
        {
            if (!Enabled || manager == null || !LoveState.Active) return;
            int day = LoveState.Today;
            if (day <= 0 || !LoveDirector.IsA2FirstDue(day)) return;

            StoreEvent active = manager.GetActiveEvent(EventId);
            if (active != null)
            {
                // The price-tag scene says the pipe is fixed the next morning.
                if (active.duration > 1) active.duration = 1;
                LoveState.SetInt("a2.water_day", day);
                return;
            }

            int triesDay = LoveState.Int("a2.water.tries_day", -1);
            int tries = triesDay == day ? LoveState.Int("a2.water.tries", 0) : 0;
            if (tries >= 4) return;
            LoveState.SetInt("a2.water.tries_day", day);
            LoveState.SetInt("a2.water.tries", tries + 1);

            StoreEvent water = EventListNormal.CreateWaterTreatmentPlantBreakdownDuration(1);
            if (water == null) return;
            manager.ActivateEvent(water, false, false);
            if (!manager.IsEventActive(EventId))
                manager.ActivateEvent(water, false, true);

            if (manager.IsEventActive(EventId))
            {
                LoveState.SetInt("a2.water_day", day);
                ModEntry.Log("Forced water_treatment_breakdown for love_a2_01 on day " + day);
            }
            else
            {
                ModEntry.LogError("Could not force water_treatment_breakdown for love_a2_01 on day " + day);
            }
        }
    }

    [HarmonyPatch(typeof(StoreEventManager), nameof(StoreEventManager.OnDayStart))]
    internal static class LoveWaterShortageDayPatch
    {
        private static void Prefix(StoreEventManager __instance) => LoveWaterShortage.Ensure(__instance);
        private static void Postfix(StoreEventManager __instance) => LoveWaterShortage.Ensure(__instance);
    }
}
