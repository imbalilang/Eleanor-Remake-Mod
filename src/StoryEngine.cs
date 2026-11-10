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
                    else if (paid > baseValue) Discount = true; // includes native +10% / +20% buying markup
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
                // The native dialogue dispatcher recognizes SEC, not the
                // localized name. It selects both the officer bubble and the
                // recorder's officer attribution, independently of the client.
                case StorySpeaker.Officer: return "SEC";
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
                Dialogue node = Text(SpeakerName(line.Speaker), L.Get(line.Key));
                PortraitExpression.Apply(node, line.Key, SpeakerName(line.Speaker));
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
                Dialogue node = Text(SpeakerName(line.Speaker), L.Get(line.Key));
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
            return DialogueSpeakers.Mark(new Dialogue().SetText(speaker, text));
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
        /// Shares one chapter wrap-up between native trade completion and the
        /// player's next-customer button. Build it after the final transaction,
        /// so reactions use the actual prices and stock/budget state.
        /// </summary>
        public static void InterceptDismiss(StoreClient client, Func<Dialogue> build, Action onComplete)
        {
            Dialogue completion = null;
            bool built = false;
            bool committed = false;
            Action commit = () =>
            {
                if (committed) return;
                committed = true;
                onComplete?.Invoke();
            };
            Func<Dialogue> getCompletion = () =>
            {
                if (built) return completion;
                built = true;
                completion = build();
                if (completion == null) return null;

                completion.SetPriority(7);
                Action start = completion.startAction;
                completion.SetStartAction(() =>
                {
                    // Prevent new trades or a second bell press during the wrap-up.
                    client.tryInterceptDismiss = null;
                    client.dismissable = false;
                    if (!client.isBusinessComplete) client.CompleteBusiness();
                    start?.Invoke();
                });
                Dialogue tail = CompletionTail(completion);
                Action end = tail.endAction;
                tail.SetEndAction(() =>
                {
                    end?.Invoke();
                    commit();
                    Dismiss(callNextWhenPossible: true);
                });
                return completion;
            };

            // CompleteClientCreation wires acceptDealDialogue to OnDealAccepted.
            // This event runs before vanilla chooses allDoneDialogue, including
            // wholesale purchases, the last item and an exhausted buyer budget.
            client.onDealAccepted += () =>
            {
                StoreClientInstance current = PlayerStore.Instance?.currentClientInstance;
                if (current == null || current.GetClientBlueprint() != client) return;
                if (client.isBusinessComplete || StoreClientInstance.IsClientBusinessDone(current))
                {
                    client.allDoneDialogue = getCompletion();
                }
            };

            // The bell checks dismissable before invoking this interceptor.
            client.dismissable = true;
            client.tryInterceptDismiss = () =>
            {
                Dialogue dialogue = getCompletion();
                if (dialogue == null)
                {
                    commit();
                    return false;
                }
                return DialogUIManager.Instance.StartDialogue(dialogue, forcedStart: true);
            };
        }

        public static void Dismiss() => Dismiss(callNextWhenPossible: false);

        // A light invitation may split and then rejoin the host's original
        // closing line. Keep its completion on that common leaf, never on the
        // choice prompt, whose END runs before the player has answered.
        public static Dialogue CompletionTail(Dialogue head)
        {
            var seen=new System.Collections.Generic.HashSet<Dialogue>();
            var pending=new System.Collections.Generic.Stack<Dialogue>();
            pending.Push(head);Dialogue leaf=null;
            while(pending.Count>0)
            {
                var node=pending.Pop();if(node==null || !seen.Add(node))continue;
                if(node.nextDialogue!=null)pending.Push(node.nextDialogue);
                foreach(var choice in node.choices)pending.Push(choice.Item2);
                if(node.nextDialogue==null && node.choices.Count==0)
                {
                    if(leaf!=null && leaf!=node)return Tail(head);
                    leaf=node;
                }
            }
            return leaf ?? Tail(head);
        }

        private static void Dismiss(bool callNextWhenPossible)
        {
            try
            {
                if (PlayerStore.Instance != null)
                {
                    PlayerStore.Instance.DismissCurrentClient(callNextWhenPossible: callNextWhenPossible);
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

        /// <summary>
        /// Vanilla Eleanor9's keepsake, restored as the endgame reward for the
        /// mother-alive routes and as Eleanor's parting gift on route C.
        /// </summary>
        public static void GiveSelfRefillingPerfume()
        {
            if (RomanceState.PerfumeGiven) return;
            try
            {
                if (GiveToPlayer("self_refilling_neuroactive_perfume",
                    item => ItemFeatureHelper.SetSecondHand(item)))
                {
                    RomanceState.PerfumeGiven = true;
                    StoryDirector.Log("Self-refilling perfume granted as the endgame keepsake.");
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("Self-refilling perfume grant failed: " + ex);
            }
        }

        public static bool GiveToPlayer(string itemId, Action<GameItem> prepare = null)
        {
            try
            {
                GameItem item = ItemSpawner.Spawn(itemId);
                if (item == null) return false;
                prepare?.Invoke(item);
                return DeliverToPlayer(item);
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("GiveToPlayer failed for " + itemId + ": " + ex);
                return false;
            }
        }

        public static bool DeliverToPlayer(GameItem item)
        {
            if (item == null) return false;
            try
            {
                GeneralHelper.SetItemOwned(item, true);

                int remaining = GraphUtils.TryAcceptAllMid(EmporiumEntry.Instance.frontInvinvElement, item);
                if (remaining > 0)
                {
                    remaining = GraphUtils.TryAcceptAll(EmporiumEntry.Instance.invElement, item, remaining);
                }
                if (remaining > 0)
                {
                    remaining = GraphUtils.TryAcceptAll(EmporiumEntry.Instance.afterhourInventory, item, remaining);
                }
                if (remaining > 0)
                {
                    GraphUtils.DestroyIfUnplaced(item, "Eleanor gift could not be placed");
                    StoryDirector.LogError("GiveToPlayer could not place " + item.identifier + " (inventory full).");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("GiveToPlayer placement failed for " + item.identifier + ": " + ex);
                return false;
            }
        }

        public static void TryFreeNutrifruit()
        {
            if (RomanceState.DriveState != 4) return;
            FreeNutrifruitOnce("route B");
        }

        /// <summary>Route C publishes the research; apply the vanilla leak world effect.</summary>
        public static void FreeNutrifruitForLeak()
        {
            FreeNutrifruitOnce("route C");
        }

        private static void FreeNutrifruitOnce(string source)
        {
            try
            {
                if (PlayerStore.Instance != null
                    && PlayerStore.Instance.storeClientData != null
                    && PlayerStore.Instance.storeClientData.nutrifruitSeedsFreed)
                {
                    return;
                }

                PlantList.FreeNutrifruit();
                StoryDirector.Log("Nutrifruit seed restriction lifted (" + source + ").");
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("FreeNutrifruit failed (" + source + "): " + ex);
            }
        }
    }
}
