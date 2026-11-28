using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace EleanorRomance
{
    internal sealed class CgChapter
    {
        internal readonly string Id, Title, Caption, BedtimeCue;
        internal readonly string[] Intro, Sold, Unsold;
        internal CgChapter(string id, string title, string[] intro, string[] sold, string[] unsold, string caption, string bedtimeCue = null)
        {
            Id = id; Title = title; Intro = intro; Sold = sold; Unsold = unsold; Caption = caption; BedtimeCue = bedtimeCue;
        }
        internal bool ShowStill(Action next) => EndingPageUI.ShowScript(Title, ChapterVisits.Art[Id], Caption, next);
    }

    internal static class ChapterVisits
    {
        private const string P = "er.cg.";
        private const string TradeClientPrefix = "er_story_cg_";
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
        private static (string Lead, string Companion) PortraitsFor(string id) => id switch
        {
            "love_night_book" or "love_trade_wall" or "love_night_supper" or "love_day_promenade"
                or "love_night_stay" or "love_trade_kiss" => (ModConstants.EleanorIntimatePortrait, null),
            "love_night_bathhouse" => (ModConstants.EleanorIntimatePortrait, ModConstants.MotherHomePortraitBeauty),
            "dual_trade_dice" or "dual_night_hotel" => (ModConstants.EleanorPlainCasualPortrait, ModConstants.LydiaPortraitBeauty),
            "dual_final_shop" => ("eleanor1", ModConstants.LydiaPortraitBeauty),
            "dual_night_bath" => (ModConstants.LydiaPortraitBeauty, null),
            "dual_trade_brooch" => (ModConstants.EleanorBlackMeshPortrait, ModConstants.LydiaWhiteMeshPortrait),
            "dual_night_fireworks" => (PortraitCompat.GalaKey, PortraitCompat.LydiaDateBeautyKey),
            "maid_stage_soundcheck" => ("eleanor_scav", null),
            "maid_trade_shopkeeper" => ("maleBM5", PortraitCompat.MaidKey),
            "maid_night_steps" or "maid_trade_shutters" or "maid_night_costume" => (PortraitCompat.MaidKey, null),
            "maid_night_encore" => (PortraitCompat.GalaKey, null),
            "rev_trade_sill" or "rev_trade_wrist" => ("eleanor_rev", null),
            "rev_ride" or "rev_night_jacket" => (ModConstants.EleanorLeatherPortrait, null),
            "rev_night_departure" or "rev_night_barracks" or "rev_final_second_knock" => (ModConstants.EleanorVestPortrait, null),
            _ => (ModConstants.EleanorPlainCasualPortrait, null)
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
                case "love_trade_wall": return love && LoveState.A2T2Done && Window(59,62);
                case "love_night_supper": return love && LoveState.Act1Done && Window(61,62);
                case "love_day_promenade": return love && LoveState.A3T1Done && !LoveState.A3T2Done && Date == 62 && LoveState.NextVisitDay > Day;
                case "love_night_stay": return love && LoveState.A3T2Done && LoveState.LastImportantDay == Day && !LoveState.A3T3Done;
                case "love_trade_kiss": return love && LoveState.A3T2Done && Window(64,68);
                case "love_night_bathhouse": return love && LoveState.A3T3Done && Window(67,68);
                case "dual_trade_dice": return dual && DualState.Done("dv01") && !DualState.Done("dv04") && Window(54,56);
                case "dual_night_bath": return pair && Window(68,69);
                case "dual_trade_brooch": return pair && Window(70,72);
                case "dual_night_fireworks": return pair && Window(70,71) && (Date == 71 || Get("trade") != Day);
                case "dual_night_hotel": return pair && Date == 72;
                case "maid_stage_soundcheck": return maid && MaidState.Progress >= 17 && MaidState.Progress < 20 && Window(46,50);
                case "maid_trade_shopkeeper": return maid && MaidState.Progress >= 18 && Window(48,50);
                case "maid_night_steps": return maid && MaidV2State.Act2Done && Window(51,53);
                case "maid_trade_shutters": return maid && MaidV2State.Residency && Window(56,59);
                case "maid_night_costume": return maid && MaidState.Progress >= 26 && Window(58,60) && (Date == 60 || MaidState.ProgressDay != Day);
                case "maid_night_encore": return maid && MaidV2State.GalaInvited && MaidState.Progress == 27 && Window(61,63) && (Date == 63 || MaidState.ProgressDay != Day);
                case "rev_trade_sill": return station && RevolutionV2State.Progress >= 281 && Window(49,51);
                case "rev_ride": return station && RevolutionV2State.Progress >= 281 && Window(49,51) && (Date == 51 || Get("trade") != Day);
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
                : ChapterScripts.All.FirstOrDefault(x => x.BedtimeCue != null && Eligible(x.Id));
            if (c == null) return false;
            int day = Day;
            return WindowTalk(c.Id, c.Intro, () =>
            {
                void Finish()
                {
                    Set(c.Id, day);
                    Set("night", day);
                    sleep();
                }
                if (c.Id == "love_night_stay")
                {
                    Set("wake", day + 1);
                    Finish();
                }
                else if (!c.ShowStill(Finish)) Finish();
            }, c.BedtimeCue);
        }
        // Ignore stale end actions from a dialogue belonging to another day or loaded slot.
        private static object windowTicket;
        private static bool windowPortraitsShown;
        private static bool windowMonologueOwned;
        private static Dialogue closedWindowCue;
        private static StoreShutterButton raisedShutter;
        private static GameObject hiddenShutterBlocker;
        private static HashSet<Dialogue> windowNodes;
        internal static bool Talking => windowTicket != null;
        internal static bool IsOwningDialogue(Dialogue dialogue) => Talking && dialogue != null && windowNodes != null && windowNodes.Contains(dialogue);
        internal static void ResetWindow()
        {
            windowTicket = null;
            windowNodes = null;
            closedWindowCue = null;
            if (windowMonologueOwned)
            {
                windowMonologueOwned = false;
                var ui = DialogUIManager.Instance;
                if (!ReferenceEquals(ui, null)) ui.isSecMonologuePlaying = false;
            }
            if (windowPortraitsShown) StoryPresentation.Clear();
            windowPortraitsShown = false;
            RestoreWindowShutter();
        }
        internal static bool CanAdvance(DialogUIManager ui)
        {
            if (ReferenceEquals(ui, null) || !IsOwningDialogue(ui.currentDialog)) return true;
            if (!ui.isSecMonologuePlaying)
            {
                ui.isSecMonologuePlaying = true;
                windowMonologueOwned = true;
            }
            if (ui.currentDialog != closedWindowCue) return true;
            if (!ReferenceEquals(raisedShutter, null)) return false;
            var shutter = StoreShutterButton.Instance;
            try
            {
                if (ReferenceEquals(shutter, null) || ReferenceEquals(shutter.blockerGameObject, null)
                    || shutter.shutterGameObject == null || !shutter.isActiveAndEnabled) return true;
                // The player can click sleep while the shutter is still descending. Use
                // only the native motion and sound. OpenShutter would start a trading day.
                raisedShutter = shutter;
                shutter.ForceImmediateClose();
                shutter.ToggleShutterDoor();
                if (EmporiumEntry.Instance != null) EmporiumEntry.Instance.ShowFrontArea();
                ui.HideBubbles();
                if (ModHelper.StartCoroutine(ModConstants.ModId, WaitForShutter(ui, shutter, windowTicket)) != null)
                    return false;
            }
            catch (Exception ex) { ModEntry.LogError("Bedtime shutter: " + ex); }
            return true;
        }

        private static IEnumerator WaitForShutter(DialogUIManager ui, StoreShutterButton shutter, object ticket)
        {
            // LerpShutter normally reaches the endpoint in 2.75 game seconds. Resume
            // even if another presentation stops or replaces the shutter mid-animation.
            float elapsed = 0f;
            while (true)
            {
                yield return null;
                if (!ReferenceEquals(ticket, windowTicket) || ui.currentDialog != closedWindowCue) yield break;
                if (shutter == null || !shutter.isActiveAndEnabled || shutter.blockerGameObject == null
                    || shutter.blockerGameObject.transform.localPosition == shutter.blockerEndPosition) break;
                elapsed += Time.deltaTime;
                if (elapsed >= 4f) break;
            }
            closedWindowCue = null;
            ui.ContinueConversation();
        }

        internal static void OnDialogueAborted(DialogUIManager ui)
        {
            if (!Talking) return;
            ResetWindow();
            if (ReferenceEquals(ui, null) || ReferenceEquals(ui.clientBubble, null)) return;
            try
            {
                ui.HideBubbles();
                ui.HideChoiceBubble();
            }
            catch (Exception ex) { ModEntry.LogError("CG window dialogue cleanup: " + ex); }
        }
        internal static void OnExternalDialogueStarted(Dialogue current)
        {
            if (Talking && !IsOwningDialogue(current)) ResetWindow();
        }
        internal static void OnConversationEnded(DialogUIManager ui)
        {
            // Normal completion calls EndConversation on the final node right before ExecuteEndAction.
            // Only treat an early EndConversation on a non-final node as an external abort.
            if (!Talking || ReferenceEquals(ui, null)) return;
            if (IsOwningDialogue(ui.currentDialog) && ui.currentDialog.nextDialogue != null)
                ResetWindow();
        }
        internal static void OnLeaveStore()
        {
            if (Talking)
            {
                var ui = DialogUIManager.Instance;
                if (!ReferenceEquals(ui, null) && !ReferenceEquals(ui.clientBubble, null))
                {
                    try { ui.InterruptDialog(); }
                    catch (Exception ex) { ModEntry.LogError("CG window leave-store interrupt: " + ex); }
                }
                ResetWindow();
            }
            if (StoryPresentation.IsShown) StoryPresentation.Clear();
            NarrationBox.Hide();
        }
        private static void RevealWindowForTalk()
        {
            if (PlayerStore.Instance?.storeState == PlayerStore.StoreState.OPEN) return;
            try
            {
                var shutter = StoreShutterButton.Instance;
                if (ReferenceEquals(shutter, null) || ReferenceEquals(shutter.blockerGameObject, null)) return;
                var parent = shutter.blockerGameObject.transform?.parent;
                if (ReferenceEquals(parent, null)) return;
                var host = parent.gameObject;
                if (!ReferenceEquals(host, null) && host.activeSelf)
                {
                    host.SetActive(false);
                    hiddenShutterBlocker = host;
                }
            }
            catch (Exception ex) { ModEntry.LogError("CG window shutter reveal: " + ex); }
        }
        private static void RestoreWindowShutter()
        {
            var shutter = raisedShutter;
            raisedShutter = null;
            if (!ReferenceEquals(shutter, null))
            {
                try
                {
                    if (shutter != null && PlayerStore.Instance?.storeState != PlayerStore.StoreState.OPEN)
                    {
                        shutter.ForceImmediateClose();
                        if (EmporiumEntry.Instance != null) EmporiumEntry.Instance.HideFrontArea();
                    }
                }
                catch (Exception ex) { ModEntry.LogError("CG window shutter restore animation: " + ex); }
            }
            var host = hiddenShutterBlocker;
            hiddenShutterBlocker = null;
            if (ReferenceEquals(host, null)) return;
            try
            {
                if (host != null && PlayerStore.Instance?.storeState != PlayerStore.StoreState.OPEN)
                    host.SetActive(true);
            }
            catch (Exception ex) { ModEntry.LogError("CG window shutter restore: " + ex); }
        }
        internal static bool WindowTalk(string id, string[] lines, Action finish, string bedtimeCue = null)
        {
            var ui = DialogUIManager.Instance;
            var store = PlayerStore.Instance;
            if (!ReferenceEquals(store, null) && (store.isClientBeingArrested || store.isHandlingShootout)) return false;
            var first = DialogueFor(id, "intro", lines);
            if (Talking || ReferenceEquals(ui, null) || first == null) return false;
            if (!ReferenceEquals(store, null) && store.storeState == PlayerStore.StoreState.CLOSED && store.currentClientInstance != null)
            {
                // If the player closed the shutter on a customer and clicked sleep within 4 seconds,
                // dismiss that departing customer now so EndAndClose's delayed coroutine cannot abort WindowTalk.
                try
                {
                    var storeUi = StoreUIManager.Instance;
                    if (!ReferenceEquals(storeUi, null) && !ReferenceEquals(storeUi.clientGameObject, null))
                        storeUi.HideClient();
                    store.DismissCurrentClient(callNextWhenPossible: false, right: true, interruptDialog: true);
                }
                catch (Exception ex) { ModEntry.LogError("CG window closing-client dismiss: " + ex); }
            }
            closedWindowCue = bedtimeCue == null ? null : DialogueFor(id, "bedtime", new[] { bedtimeCue });
            var start = closedWindowCue?.SetNextDialogue(first) ?? first;
            var nodes = new HashSet<Dialogue>();
            for (var n = start; n != null && nodes.Add(n); n = n.nextDialogue) n.SetPriority(7);
            var ticket = new object();
            windowTicket = ticket;
            windowNodes = nodes;
            if (!ui.isSecMonologuePlaying)
            {
                // Native ContinueConversation aborts whenever currentClientInstance is null
                // unless monologue mode is active. Own and clear this flag with the window ticket.
                ui.isSecMonologuePlaying = true;
                windowMonologueOwned = true;
            }
            var (lead, companion) = PortraitsFor(id);
            first.SetStartAction(() =>
            {
                if (!ReferenceEquals(windowTicket, ticket)) return;
                windowPortraitsShown = StoryPresentation.ShowBackups(lead, companion);
                if (windowPortraitsShown) RevealWindowForTalk();
            });
            StoryEngine.OnDialogueFinished(first, () =>
            {
                if (!ReferenceEquals(windowTicket, ticket)) return;
                ResetWindow();
                finish();
            });
            try
            {
                if (ui.StartDialogue(start, forcedStart: true)) return true;
            }
            catch (Exception ex) { ModEntry.LogError("CG window dialogue: " + ex); }
            if (ReferenceEquals(windowTicket, ticket)) ResetWindow();
            return false;
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
            var store = PlayerStore.Instance;
            if (!Active || EndingPageUI.PageOpen || store?.currentClientInstance != null) return false;
            var scene = Eligible("maid_stage_soundcheck") ? Find("maid_stage_soundcheck")
                : DayOffAvailable(store) ? Find("love_day_promenade") : null;
            if (scene == null) return false;
            int day = Day;
            return WindowTalk(scene.Id, scene.Intro, () =>
            {
                void Finish()
                {
                    Set(scene.Id, day);
                    if (store.storeState != PlayerStore.StoreState.OPEN || store.currentClientInstance != null) return;
                    if (scene.Id != "love_day_promenade" || ChapterTradeQueuePatch.OnlyOrdinaryGuestsRemain(store.storeClientManager)) close();
                }
                if (!scene.ShowStill(Finish)) Finish();
            });
        }
        private static bool DayOffAvailable(PlayerStore store) => Eligible("love_day_promenade")
            && StoryDirector.PendingChapterForDay(Day) == 0 && !LoveDirector.SerpicoStoryVisitDue(Day)
            && ChapterTradeQueuePatch.OnlyOrdinaryGuestsRemain(store.storeClientManager);

        private static int TradeWindowEnd(string id) => id switch
        {
            "love_trade_wall" => 62,
            "love_trade_kiss" => 68,
            "dual_trade_dice" => 56,
            "dual_trade_brooch" => 72,
            "maid_trade_shopkeeper" => 50,
            "maid_trade_shutters" => 59,
            "rev_trade_sill" => 51,
            "rev_trade_wrist" => 58,
            _ => 0
        };
        // Avoid putting an optional trade CG in the same daytime queue as a main/daily mod visit,
        // unless today is the final day of its window so a delayed schedule never misses the CG.
        private static bool TradeSpacedToday(string id) => RomanceState.LastQueuedDay != Day || Date >= TradeWindowEnd(id);

        internal static CgChapter TradeDue() => Active && Get("trade") != Day
            ? ChapterScripts.All.FirstOrDefault(c => c.Id.Contains("_trade_") && Eligible(c.Id) && TradeSpacedToday(c.Id)) : null;
        internal static StoreClient Trade(CgChapter c)
        {
            bool seller = c.Id == "love_trade_wall" || c.Id == "maid_trade_shutters" || c.Id == "rev_trade_wrist";
            bool rev = c.Id.StartsWith("rev_");
            bool shopkeeper = c.Id == "maid_trade_shopkeeper";
            var (lead, companion) = PortraitsFor(c.Id);
            var intent = seller ? StoreClient.ClientIntent.SELL : StoreClient.ClientIntent.BUY;
            var client = shopkeeper ? EleanorStory.NewNpcClient("cg_" + c.Id, "客人", lead, EleanorStory.FactionKind.Lower)
                : EleanorStory.NewEleanorClient("cg_" + c.Id, lead, intent, !rev);
            string companionName = companion == null ? null : shopkeeper ? L.Eleanor : L.Get("er_name_lydia");

            Dialogue TradeDialogue(string section, string[] lines)
            {
                var dialogue = DialogueFor(c.Id, section, lines, companionName);
                if (dialogue != null && companion != null)
                    dialogue.SetStartAction(() => StoryPresentation.ShowBackups(companion, backup1Name: companionName));
                return dialogue;
            }

            client.clientIntent = intent;
            client.dismissable = true; // Optional slot: preserve the original walk-in's early-close permission.
            client.mainDialogue = TradeDialogue("intro", c.Intro);
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
            StoryEngine.InterceptDismiss(client, () => TradeDialogue(sold ? "sold" : "unsold", sold ? c.Sold : c.Unsold), () =>
            {
                Set(c.Id, Day); Set("trade", Day);
                if (sold) c.ShowStill(null);
            });
            client.CompleteClientCreation();
            ModHelper.SetClientSource(client, ModConstants.ModId);
            return client;
        }
        internal static bool IsChapterClient(StoreClient client) =>
            client?.identifier?.StartsWith(TradeClientPrefix, StringComparison.Ordinal) == true;

        internal static bool RestoreTradePortrait(StoreClient client)
        {
            if (!IsChapterClient(client)) return false;
            var id = client.identifier.Substring(TradeClientPrefix.Length);
            if (!Art.ContainsKey(id)) return false;
            client.spriteName = PortraitCompat.Resolve(PortraitsFor(id).Lead);
            return true;
        }

        private static Dialogue DialogueFor(string id, string section, string[] lines, string companionName = null)
        {
            Dialogue root = null, tail = null;
            int index = 0;
            foreach (string line in lines)
            {
                index++;
                int pos = line.IndexOf('　');
                string who = line.Substring(0, pos);
                string name = who == "店主" ? L.Player
                    : who == "埃莉诺" ? L.Eleanor
                    : who == "莉迪亚" ? L.Get("er_name_lydia")
                    : who == "索菲娅" ? L.Get("er_name_sophia")
                    : who == "旁白" ? L.Get("er_name_narrator")
                    : who;
                bool companion = name == companionName;
                Dialogue node = StoryEngine.Text(companion ? DialogUIManager.SEC : name, line.Substring(pos + 1));
                if (companion) StoryPresentation.MarkCompanion(node, name);
                PortraitExpression.Apply(node, $"er_cg_{id}_{section}_{index:02d}", name);
                if (root == null) root = node; else tail.SetNextDialogue(node);
                tail = node;
            }
            return root;
        }
    }
}
