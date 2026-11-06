using System;
using System.Collections.Generic;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>Small helpers shared by all v0.6 chapters.</summary>
    public static class StoryEngine
    {
        public sealed class Choice
        {
            public readonly string Label;
            public readonly string Description;
            public readonly Dialogue Next;
            public readonly Action Action;
            public readonly Func<bool> Enabled;

            public Choice(string label, string description, Dialogue next, Action action, Func<bool> enabled = null)
            {
                Label = label;
                Description = description;
                Next = next;
                Action = action;
                Enabled = enabled;
            }
        }

        public sealed class DealTracker
        {
            public bool Any;
            public bool Fair;
            public bool Discount;
            public bool Overcharge;
            public bool Lowball;
            public int TotalPaid;
            public long TotalBase;

            public void Record(GameItem item, int paid, bool eleanorIsSeller)
            {
                Any = true;
                TotalPaid += Mathf.Max(0, paid);
                long baseValue = item != null ? item.GetBaseValue() : 0;
                if (baseValue <= 0 && item != null) baseValue = item.unitValue;
                if (baseValue <= 0) baseValue = Mathf.Max(1, paid);
                TotalBase += baseValue;

                float ratio = paid / (float)baseValue;
                if (eleanorIsSeller)
                {
                    if (ratio < 0.65f) Lowball = true;
                    else if (ratio > 1.25f) Discount = true; // player paid above value
                    else Fair = true;
                }
                else
                {
                    if (ratio > 1.25f) Overcharge = true;   // player charged her above value
                    else if (ratio < 0.65f) Discount = true; // player gave her a deal
                    else Fair = true;
                }
            }

            /// <summary>Worst behaviour wins for the reaction line.</summary>
            public char Dominant
            {
                get
                {
                    if (Lowball) return 'L';
                    if (Overcharge) return 'O';
                    if (Discount) return 'D';
                    if (Fair) return 'F';
                    return 'N';
                }
            }
        }

        public static string SpeakerName(StorySpeaker speaker)
        {
            switch (speaker)
            {
                case StorySpeaker.Eleanor: return L.Eleanor;
                case StorySpeaker.Player: return L.Player;
                case StorySpeaker.Mother: return L.Get("er_name_mother");
                case StorySpeaker.Lydia: return L.Get("er_name_lydia");
                case StorySpeaker.Uncle: return L.Get("er_name_uncle");
                case StorySpeaker.Thug: return L.Get("er_name_thug");
                case StorySpeaker.Officer: return L.Get("er_name_officer");
                case StorySpeaker.News: return L.Get("er_name_news");
                case StorySpeaker.SecurityNotice: return L.Get("er_name_security_notice");
                default: return L.Get("er_name_narrator");
            }
        }

        public static Dialogue Build(string blockId)
        {
            return BuildFrom(blockId, 0);
        }

        /// <summary>Builds a block without its first line (used when a choice label repeats the player line).</summary>
        public static Dialogue BuildSkipFirst(string blockId)
        {
            return BuildFrom(blockId, 1);
        }

        private static Dialogue BuildFrom(string blockId, int start)
        {
            StoryLine[] lines = StoryV06Content.Get(blockId);
            if (lines == null || lines.Length == 0)
            {
                StoryDirector.LogError("Missing story block: " + blockId);
                return null;
            }

            Dialogue head = null;
            Dialogue tail = null;
            for (int i = start; i < lines.Length; i++)
            {
                StoryLine line = lines[i];
                Dialogue node = new Dialogue().SetText(SpeakerName(line.Speaker), L.Get(line.Key));
                if (head == null)
                {
                    head = node;
                    tail = node;
                }
                else
                {
                    tail.SetNextDialogue(node);
                    tail = node;
                }
            }
            return head;
        }

        /// <summary>Returns the first line of the block chain.</summary>
        public static Dialogue Chain(params string[] blockIds)
        {
            Dialogue head = null;
            foreach (string id in blockIds)
            {
                if (head == null) head = Build(id);
                else AppendTo(head, id);
            }
            return head;
        }

        /// <summary>
        /// Appends a content block after the end of the supplied graph and returns
        /// the new tail.  Passing a head or a tail both work.
        /// </summary>
        public static Dialogue Append(Dialogue current, string blockId)
        {
            StoryLine[] lines = StoryV06Content.Get(blockId);
            if (lines == null || lines.Length == 0)
            {
                StoryDirector.LogError("Missing story block: " + blockId);
                return Tail(current);
            }

            Dialogue tail = Tail(current);
            foreach (StoryLine line in lines)
            {
                Dialogue node = new Dialogue().SetText(SpeakerName(line.Speaker), L.Get(line.Key));
                if (tail == null)
                {
                    tail = node;
                }
                else
                {
                    tail.SetNextDialogue(node);
                    tail = node;
                }
            }
            return tail;
        }

        /// <summary>Appends a block to a dialogue graph without changing its head.</summary>
        public static void AppendTo(Dialogue head, string blockId)
        {
            Dialogue tail = Tail(head);
            Append(tail, blockId);
        }

        public static Dialogue Tail(Dialogue head)
        {
            Dialogue current = head;
            while (current != null && current.nextDialogue != null)
            {
                current = current.nextDialogue;
            }
            return current;
        }

        public static Dialogue Text(string speaker, string text)
        {
            return new Dialogue().SetText(speaker, text);
        }

        public static void AddChoices(Dialogue head, string prompt, params Choice[] choices)
        {
            Dialogue tail = Tail(head);
            Dialogue node = tail.NextDialogue().SetText(L.Player, prompt);
            foreach (Choice choice in choices)
            {
                Dialogue choiceLine = new Dialogue().SetText(L.Player, choice.Label);
                choiceLine.SetNextDialogue(choice.Next);
                node.AddChoice(
                    DirectoryMaster.Item("yes"),
                    choice.Label,
                    choice.Description,
                    choiceLine,
                    choice.Action,
                    choice.Enabled);
            }
        }

        /// <summary>
        /// Shows a leaving conversation once, then commits the chapter.  If the
        /// client is dismissed by the game before this runs, nothing is advanced
        /// and the chapter can be queued again on the next day.
        /// </summary>
        public static void InterceptDismiss(StoreClient client, Func<Dialogue> build, Action onComplete)
        {
            bool shown = false;
            client.tryInterceptDismiss = () =>
            {
                if (shown) return false;
                shown = true;

                Dialogue dialogue = build();
                if (dialogue == null)
                {
                    onComplete?.Invoke();
                    Dismiss();
                    return false;
                }

                dialogue.SetPriority(7);
                Dialogue tail = Tail(dialogue);
                tail.SetEndAction(() =>
                {
                    onComplete?.Invoke();
                    Dismiss();
                });
                return DialogUIManager.Instance.StartDialogue(dialogue, forcedStart: true);
            };
        }

        public static void Dismiss()
        {
            try
            {
                if (PlayerStore.Instance != null)
                {
                    PlayerStore.Instance.DismissCurrentClient(callNextWhenPossible: false);
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Dismiss failed: " + ex);
            }
        }

        public static bool HasCash(int amount)
        {
            try { return PlayerStore.Instance != null && PlayerStore.Instance.GetCash() >= amount; }
            catch { return false; }
        }

        public static bool Spend(int amount)
        {
            if (!HasCash(amount)) return false;
            try
            {
                ModHelper.AddCredit(-amount, false);
                return true;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Spend failed: " + ex);
                return false;
            }
        }

        public static void AddCredit(int amount)
        {
            try { ModHelper.AddCredit(amount, false); }
            catch (Exception ex) { StoryDirector.LogError("AddCredit failed: " + ex); }
        }

        public static int SecRep() => SafeRep(StoreReputation.GetSecReputation);
        public static int UlRep() => SafeRep(StoreReputation.GetULReputation);
        public static int LlRep() => SafeRep(StoreReputation.GetLLReputation);
        public static int BmRep() => SafeRep(StoreReputation.GetBMReputation);
        public static int RevRep() => SafeRep(StoreReputation.GetRevReputation);

        private static int SafeRep(Func<int> getter)
        {
            try { return getter(); } catch { return 0; }
        }

        public static bool CanPrepareProtection()
        {
            return (SecRep() >= 30 && (RomanceState.UncleReported || RomanceState.ThreatRecorded))
                || (UlRep() >= 30 && RomanceState.DriveState != 0)
                || (RomanceState.UncleReported && RomanceState.Savings >= 800);
        }

        public static int PositiveDeals => RomanceState.FairDeals + RomanceState.Discounts;

        public static bool CanConfess()
        {
            if (RomanceState.MotherState != 1) return false;
            if (RomanceState.Route == 3 || RomanceState.Route == 4) return false;
            if (RomanceState.Romance == 4) return false;
            bool risk = RomanceState.UncleReported || RomanceState.Rescued || RomanceState.ThreatRecorded;
            return PositiveDeals >= 3 && risk;
        }

        public static bool NeedReconciliation()
        {
            return RomanceState.KnewTrapUnwarned;
        }

        public static GameItem SpawnForSale(string itemId)
        {
            try
            {
                GameItem item = ItemSpawner.Spawn(itemId);
                if (item == null)
                {
                    StoryDirector.LogError("ItemSpawner returned null: " + itemId);
                    return null;
                }
                PlayerStore.Instance.AddDirectSellingItemToTableMiddle(item);
                return item;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("SpawnForSale failed for " + itemId + ": " + ex);
                return null;
            }
        }

        public static bool GiveToPlayer(string itemId)
        {
            try
            {
                GameItem item = ItemSpawner.Spawn(itemId);
                if (item == null) return false;

                GraphUtils.TryAcceptAll(EmporiumEntry.Instance.invElement, item);
                if (item.parentInventory == null)
                {
                    GraphUtils.TryAcceptAll(EmporiumEntry.Instance.afterhourInventory, item);
                }
                if (item.parentInventory == null)
                {
                    StoryDirector.LogError("GiveToPlayer could not place " + itemId + " (inventory full).");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("GiveToPlayer failed for " + itemId + ": " + ex);
                return false;
            }
        }

        public static void TryFreeNutrifruit()
        {
            if (RomanceState.DriveState != 4) return;
            try
            {
                PlantList.FreeNutrifruit();
                StoryDirector.Log("Nutrifruit seed restriction lifted for route B.");
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("FreeNutrifruit failed: " + ex);
            }
        }
    }
}
