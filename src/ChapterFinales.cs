using System;
using HarmonyLib;

namespace EleanorRomance
{
    internal static class ChapterFinales
    {
        private static int Today => DualState.Today;
        internal static bool ShopFinished => DualState.Flag("cg.shop.finished");
        internal static bool KnockFinished => ModHelper.GetModDataBool(ModConstants.ModId, "revv2.cg.knock", false);
        private static int ReunionDay => ModHelper.GetModDataInt(ModConstants.ModId, "revv2.cg.reunion_day", -1);

        internal static void MarkReunion() => ModHelper.SetModDataInt(ModConstants.ModId, "revv2.cg.reunion_day", Today);

        private static int Due(int day, int gap)
        {
            int deadline = StoryDirector.StoryDay(75);
            return day < 0 ? Today : day >= deadline ? day + 1 : Math.Min(day + gap, deadline);
        }

        internal static bool AtBedtime(Action resume)
        {
            if (!RomanceState.StoryActive || EndingPageUI.PageOpen || !DualState.Confirmed || !DualDirector.RouteStoryFinished
                || DualState.Flag("ending_card_shown") || Today < Due(DualState.SceneDay("dv13"), 6)) return false;
            if (ShopFinished) { return EndingPageUI.ShowDualCompletion(resume); }
            return ChapterVisits.WindowTalk("dual_final_shop", new[] {
                "莉迪亚　" + L.Get("er_cg_shop_inside"), "埃莉诺　" + L.Get("er_cg_shop_out")
            }, () =>
            {
                DualState.Flag("cg.shop.finished", true);
                if (!EndingPageUI.ShowDualCompletion(resume)) resume();
            });
        }

        internal static void AtMorning()
        {
            if (!RomanceState.StoryActive || ChapterVisits.Talking || EndingPageUI.PageOpen || !RevolutionV2State.Enabled
                || !RomanceState.RevComplete || RomanceState.RevEndingShown || Today < Due(ReunionDay, 4)) return;
            Action show = () =>
            {
                ModHelper.SetModDataBool(ModConstants.ModId, "revv2.cg.knock", true);
                EndingPageUI.ShowRevolutionCompletion();
            };
            if (KnockFinished || ModHelper.GetModDataBool(ModConstants.ModId, "revv2.cg.knock_intro", false)) show();
            else ChapterVisits.WindowTalk("rev_final_second_knock", new[] {
                "埃莉诺　" + L.Get("er_cg_knock_intro"), "店主　" + L.Get("er_cg_knock_rail"),
                "埃莉诺　" + L.Get("er_cg_knock_farewell")
            }, show);
        }
    }

    [HarmonyPatch(typeof(EndOfDayUIManager), nameof(EndOfDayUIManager.OnEndDayButtonClick))]
    internal static class ChapterBedtimePatch
    {
        private static bool sleepStarting;
        internal static void Reset() => sleepStarting = false;

        private static bool Prefix(EndOfDayUIManager __instance)
        {
            if (!LoveA2Closing.Enabled || !RomanceState.StoryActive) return true;
            if (sleepStarting || ChapterVisits.Talking || EndingPageUI.PageOpen || PlayerStore.Instance?.storeState != PlayerStore.StoreState.CLOSED) return false;
            Action sleep = __instance.OnEndDayButtonClick;
            if (!LoveA2Closing.OwnsTonight && (ChapterFinales.AtBedtime(sleep) || ChapterVisits.Bedtime(sleep)))
            {
                if (__instance.IsOpen) __instance.CloseUI(false);
                return false;
            }
            sleepStarting = true;
            LoveA2Closing.CancelTimer();
            return true;
        }

        private static void Finalizer(Exception __exception)
        {
            if (__exception != null) Reset();
        }
    }

    // A restored evening can contain old morning logs. The native report forcibly
    // changes CLOSED to MORNING without settling the night. Suppress only that load report.
    [HarmonyPatch(typeof(StartOfDayUIManager), nameof(StartOfDayUIManager.ShowMorningReport))]
    internal static class ChapterLoadedReportPatch
    {
        private static bool restoredEvening;
        internal static void OnLoaded() => restoredEvening = LoveA2Closing.Enabled && RomanceState.StoryActive
            && PlayerStore.Instance?.storeState == PlayerStore.StoreState.CLOSED && PlayerStore.Instance.nightLogs?.Count > 0;
        internal static void Reset() => restoredEvening = false;
        private static bool Prefix()
        {
            bool skip = restoredEvening && PlayerStore.Instance?.storeState == PlayerStore.StoreState.CLOSED;
            Reset();
            return !skip;
        }
    }

    [HarmonyPatch(typeof(StartOfDayUIManager), nameof(StartOfDayUIManager.OpenUI))]
    internal static class ChapterMorningPatch
    {
        internal static void CheckMorning(bool atShutter = false)
        {
            if (!LoveA2Closing.Enabled || PlayerStore.Instance?.storeState != PlayerStore.StoreState.MORNING) return;
            if (atShutter) ChapterFinales.AtMorning();
            ChapterVisits.Morning();
        }
        private static void Postfix() => CheckMorning();
    }
}
