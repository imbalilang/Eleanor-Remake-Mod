using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace EleanorRomance
{
    internal sealed class CgChapter
    {
        internal readonly string Id, Title, Caption;
        internal readonly string[] Intro, Labels, Yes, No, Picture;
        internal CgChapter(string id, string title, string[] intro, string[] labels, string[] yes, string[] no, string[] picture, string caption)
        {
            Id = id; Title = title; Intro = intro; Labels = labels; Yes = yes; No = no; Picture = picture; Caption = caption;
        }
        internal bool ShowStill(Action next)
        {
            string cg = ChapterVisits.Art[Id];
            string body = PortraitCompat.GetLocalSprite(cg) == null && Picture.Length > 0
                ? string.Join("\n", Picture) + "\n\n" + Caption : Caption;
            return EndingPageUI.ShowScript(Title, cg, body, next);
        }
        internal void Still(Action next)
        {
            if (!ShowStill(next)) next?.Invoke();
        }
        internal void Read(string[] text, Action next)
        {
            if (text.Length == 0 || !EndingPageUI.ShowScript(Title, null, string.Join("\n\n", text), next)) next?.Invoke();
        }
    }

    internal static class ChapterVisits
    {
        private const string P = "er.cg.";
        private static int Day => DualState.Today;
        private static int Date => Day + StoryDirector.StoryOffset();
        private static int Get(string key) => ModHelper.GetModDataInt(ModConstants.ModId, P + key, -1);
        private static void Set(string key, int value) => ModHelper.SetModDataInt(ModConstants.ModId, P + key, value);
        internal static bool Active => LoveA2Closing.Enabled && RomanceState.StoryActive &&
            (LocalizationSettings.SelectedLocale?.Identifier.Code ?? "").StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        internal static readonly Dictionary<string, string> Art = new Dictionary<string, string>
        {
            {"love_night_book", "er_event_love_book_cg"}, {"love_trade_wall", "er_event_love_wall_cg"},
            {"love_night_supper", "er_event_love_supper_cg"}, {"love_day_promenade", "er_event_love_promenade_cg"},
            {"love_night_stay", "er_event_love_wakeup_cg"}, {"love_trade_kiss", "er_event_love_heart_kiss_cg"},
            {"love_night_bathhouse", "er_event_love_bathhouse_cg"}, {"dual_trade_dice", "er_event_harem_dice_cg"},
            {"dual_night_bath", "er_event_harem_bath_cg"}, {"dual_trade_brooch", "er_event_harem_rivals_cg"},
            {"dual_night_fireworks", "er_event_harem_fireworks_cg"}, {"dual_night_hotel", "er_event_harem_hotel_cg"},
            {"maid_stage_soundcheck", "er_event_maid_soundcheck_cg"}, {"maid_trade_shopkeeper", "er_event_maid_shopkeeper_cg"},
            {"maid_night_steps", "er_event_maid_steps_cg"}, {"maid_trade_shutters", "er_event_maid_shutters_cg"},
            {"maid_night_costume", "er_event_maid_costume_cg"}, {"maid_night_encore", "er_event_maid_encore_cg"},
            {"rev_trade_sill", "er_event_revolution_sill_cg"}, {"rev_ride", "er_event_revolution_ride_cg"},
            {"rev_trade_wrist", "er_event_revolution_wrist_cg"}, {"rev_night_departure", "er_event_revolution_departure_cg"},
            {"rev_night_jacket", "er_event_revolution_jacket_cg"}, {"rev_night_barracks", "er_event_revolution_barracks_cg"}
        };
        private static bool Window(int first, int last) => Date >= first && Date <= last;
        private static bool Eligible(string id)
        {
            if (Get(id) >= 0) return false;
            bool love = LoveState.Active && !DualState.Active && !MaidState.Active;
            bool dual = DualState.Active && !ChapterFinales.ShopFinished && !DualState.Flag("ending_card_shown");
            bool pair = dual && DualState.Confirmed && DualDirector.RouteStoryFinished;
            bool maid = MaidState.Active && MaidV2State.CoWork && !MaidV2State.Ended;
            bool rev = RevolutionV2State.Enabled && !RomanceState.RevEndingShown && !ChapterFinales.KnockFinished;
            bool station = rev && !RevolutionV2State.Expedition && !RomanceState.RevComplete && Date < 63;
            switch (id)
            {
                case "love_night_book": return love && LoveState.T1Done && Window(54,56);
                case "love_trade_wall": return love && LoveState.A2T2Done && Window(60,62);
                case "love_night_supper": return love && LoveState.Act1Done && Window(61,62);
                case "love_day_promenade": return love && LoveState.A3T1Done && !LoveState.A3T2Done && Date == 62 && LoveState.NextVisitDay > Day;
                case "love_night_stay": return love && LoveState.A3T2Done && LoveState.LastImportantDay == Day && !LoveState.A3T3Done;
                case "love_trade_kiss": return love && LoveState.A3T2Done && Window(64,68);
                case "love_night_bathhouse": return love && LoveState.A3T3Done && Window(67,68);
                case "dual_trade_dice": return dual && DualState.Done("dv01") && !DualState.Done("dv04") && Window(54,56);
                case "dual_night_bath": return pair && Window(68,69);
                case "dual_trade_brooch": return pair && Window(70,72);
                case "dual_night_fireworks": return pair && Window(70,71);
                case "dual_night_hotel": return pair && Date == 72;
                case "maid_stage_soundcheck": return maid && MaidState.Progress >= 17 && MaidState.Progress < 20 && Window(46,50);
                case "maid_trade_shopkeeper": return maid && MaidState.Progress >= 18 && Window(48,50);
                case "maid_night_steps": return maid && MaidV2State.Act2Done && Window(51,53);
                case "maid_trade_shutters": return maid && MaidV2State.Residency && Window(56,58);
                case "maid_night_costume": return maid && MaidState.Progress >= 26 && Window(58,59);
                case "maid_night_encore": return maid && MaidV2State.GalaInvited && MaidState.Progress == 27 && Window(61,62);
                case "rev_trade_sill": case "rev_ride": return station && RevolutionV2State.Progress >= 281 && Window(49,51);
                case "rev_trade_wrist": return station && RevolutionV2State.Progress >= 283 && Window(55,58);
                case "rev_night_departure": return station && RevolutionV2State.Progress == 286 && Date == 62;
                case "rev_night_jacket": return rev && RomanceState.RevComplete && Date == 73;
                case "rev_night_barracks": return rev && RomanceState.RevComplete && Date == 74;
                default: return false;
            }
        }
        private static CgChapter Find(string id) => ChapterScripts.All.First(c => c.Id == id);
        internal static bool Bedtime(Action sleep)
        {
            if (!Active || Get("night") == Day) return false;
            // A proposal invitation expires tonight. Flexible visits can use their remaining nights.
            var c = Eligible("love_night_stay") ? Find("love_night_stay")
                : ChapterScripts.All.FirstOrDefault(x => (x.Id.Contains("_night_") || x.Id == "rev_ride") && Eligible(x.Id));
            if (c == null) return false;
            return Offer(c, sleep, sleep, true);
        }
        private static bool Offer(CgChapter c, Action accepted, Action declined, bool night)
        {
            int day = Day;
            void Commit()
            {
                Set(c.Id, day);
                if (night) Set("night", day);
            }
            Action take = () =>
            {
                if (c.Id == "love_night_stay")
                {
                    Commit();
                    Set("wake", day + 1);
                    c.Read(c.Yes, accepted);
                }
                else c.Read(c.Yes, () => c.Still(() => { Commit(); accepted(); }));
            };
            Action leave = () => { Commit(); c.Read(c.No, declined); };
            return EndingPageUI.ShowScript(c.Title, null, string.Join("\n\n", c.Intro), take,
                c.Labels.Length == 2 ? c.Labels : null, leave);
        }
        internal static void Morning()
        {
            if (!Active || EndingPageUI.PageOpen) return;
            if (Get("wake") == Day)
            {
                Find("love_night_stay").ShowStill(() => Set("wake", -1));
            }
        }
        internal static bool AtClosing(Action close)
        {
            return Active && !EndingPageUI.PageOpen && Eligible("maid_stage_soundcheck")
                && Offer(Find("maid_stage_soundcheck"), close, close, false);
        }
        internal static bool DayOffAvailable(PlayerStore store) => Active && !EndingPageUI.PageOpen
            && store?.storeState == PlayerStore.StoreState.OPEN && store.currentClientInstance == null
            && Eligible("love_day_promenade") && StoryDirector.PendingChapterForDay(Day) == 0
            && !LoveDirector.SerpicoStoryVisitDue(Day) && ChapterTradeQueuePatch.OnlyOrdinaryGuestsRemain(store.storeClientManager);

        internal static bool OfferDayOff(Action resume, Action close) => Offer(Find("love_day_promenade"), () =>
        {
            var store = PlayerStore.Instance;
            if (store?.storeState == PlayerStore.StoreState.OPEN && store.currentClientInstance == null
                && ChapterTradeQueuePatch.OnlyOrdinaryGuestsRemain(store.storeClientManager)) close();
        }, resume, false);
        internal static CgChapter TradeDue() => Active && Get("trade") != Day
            ? ChapterScripts.All.FirstOrDefault(c => c.Id.Contains("_trade_") && Eligible(c.Id)) : null;
        internal static StoreClient Trade(CgChapter c)
        {
            bool seller = c.Id == "love_trade_wall" || c.Id == "maid_trade_shutters" || c.Id == "rev_trade_wrist";
            bool maid = c.Id.StartsWith("maid_");
            bool rev = c.Id.StartsWith("rev_");
            var intent = seller ? StoreClient.ClientIntent.SELL : StoreClient.ClientIntent.BUY;
            var client = c.Id == "maid_trade_shopkeeper" ? EleanorStory.NewNpcClient("cg_" + c.Id, "客人", "maleBM5", EleanorStory.FactionKind.Lower)
                : EleanorStory.NewEleanorClient("cg_" + c.Id, maid ? "er_portrait_eleanor_maid_female_beauty" : rev ? "eleanor_rev" : "eleanor1", intent, !rev);
            client.clientIntent = intent;
            client.dismissable = true; // Optional slot: preserve the original walk-in's early-close permission.
            client.mainDialogue = DialogueFor(c.Intro);
            if (seller)
            {
                client.SetWholesale(20);
                string stock = c.Id == "love_trade_wall" ? "scrap_metal" : c.Id == "rev_trade_wrist" ? "wire_cutter" : "screwdriver";
                StoryEngine.OnDialogueFinished(client.mainDialogue, () => StoryEngine.SpawnForSale(stock));
            }
            else
            {
                client.SetBudget(300);
                string tag = c.Id == "love_trade_kiss" ? "FOOD" : c.Id == "rev_trade_sill" ? "CONTRABAND" : c.Id == "dual_trade_brooch" ? "LUXURY_ITEM" : c.Id == "maid_trade_shopkeeper" ? "TOOL" : "HOUSEHOLD_GOOD";
                client.clientBuyingTagList.Add(tag);
                client.SetVariety();
            }
            bool sold = false;
            client.onItemSold += (item, price) => sold |= item != null && price > 0;
            client.onItemBought += (item, price) => sold |= item != null && price > 0;
            StoryEngine.InterceptDismiss(client, () => DialogueFor(sold ? c.Yes : c.No), () =>
            {
                Set(c.Id, Day); Set("trade", Day);
                if (sold) c.Still(null);
            });
            client.CompleteClientCreation();
            ModHelper.SetClientSource(client, ModConstants.ModId);
            return client;
        }
        internal static bool IsChapterClient(StoreClient client) =>
            client?.identifier?.StartsWith("er_story_cg_", StringComparison.Ordinal) == true;
        private static Dialogue DialogueFor(string[] lines)
        {
            Dialogue root = null, tail = null;
            foreach (string line in lines)
            {
                int pos = line.IndexOf('　');
                string who = pos > 0 ? line.Substring(0, pos) : null;
                string name = who == "店主" ? L.Player : who == null ? L.Get("er_name_narrator") : who;
                Dialogue node = StoryEngine.Text(name, pos > 0 ? line.Substring(pos + 1) : line);
                if (root == null) root = node; else tail.SetNextDialogue(node);
                tail = node;
            }
            return root;
        }
    }
}
