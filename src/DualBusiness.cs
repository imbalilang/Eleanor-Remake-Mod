using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// The recommendation plaque and the two real referral commissions. All
    /// checks read the live showcase/inventory; no trade is simulated.
    /// </summary>
    public static class DualBusiness
    {
        public const int FirstBudget = 500;
        public const int SecondBudget = 600;
        public const int SecondMinDurability = 150;
        public const string PurifierId = "portable_water_purifier";
        private const string PaidTag = "ER_DUAL_BIZ_PAID";

        public static IEnumerable<GameItem> ShowcaseItems()
        {
            if (EmporiumEntry.Instance == null || EmporiumEntry.Instance.showcaseWindow == null)
                return Enumerable.Empty<GameItem>();
            try
            {
                return GraphUtils.FindAllChildrenType<GameItem>(EmporiumEntry.Instance.showcaseWindow)
                    .Where(i => i != null);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Dual showcase scan failed: " + ex);
                return Enumerable.Empty<GameItem>();
            }
        }

        public static bool PlaquePlaced()
        {
            return ShowcaseItems().Any(i => i.identifier == ModConstants.PlaqueItemId);
        }

        public static bool PlaqueExists()
        {
            try
            {
                if (PlayerStore.Instance == null) return false;
                return PlayerStore.Instance.FindAllItem(false)
                    .Any(i => i != null && i.identifier == ModConstants.PlaqueItemId);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Dual plaque scan failed: " + ex);
                return false;
            }
        }

        public static bool HasFirstItem()
        {
            return ShowcaseItems().Any(IsFirstItem);
        }

        public static bool HasSecondItem()
        {
            return ShowcaseItems().Any(IsSecondItem);
        }

        private static bool IsFirstItem(GameItem item)
        {
            if (item == null || item.identifier == ModConstants.PlaqueItemId) return false;
            if (item.IsTag("ER_KEEPSAKE") || item.IsTag("IMPORTANT_TAG")) return false;
            return item.IsTag(TypeHelper.LUXURY_ITEM) || item.IsTag(TypeHelper.ART)
                || item.IsTag(TypeHelper.HIGHEND) || item.IsTag(TypeHelper.HOUSEHOLD_GOOD);
        }

        private static bool IsSecondItem(GameItem item)
        {
            if (item == null || item.identifier != PurifierId) return false;
            try { return DurabilityHelper.GetRealDurability(item) >= SecondMinDurability; }
            catch { return false; }
        }

        /// <summary>Called from the daily hook. Returns true when a short business client was queued.</summary>
        public static bool TryQueue(StoreClientManager manager, int day)
        {
            if (manager == null || !DualState.PlaqueActive) return false;
            if (HasBusinessClient(manager)) return false;
            if (DualState.CooldownUntil >= 0 && day < DualState.CooldownUntil) return false;

            // A direct appointment survives taking the plaque down; only new
            // random referrals require a live showcase display.
            if (DualState.SecondBusiness == 1 && day >= DualState.BusinessDueDay)
            {
                return QueueBuyer(manager, day, true);
            }

            bool shown = PlaquePlaced();
            if (!shown) return false;

            if (DualState.FirstBusiness == 0 && DualState.PlaqueReceived && HasFirstItem()
                && day >= FirstEarliestDay())
            {
                DualState.FirstBusiness = 1;
                DualState.BusinessDueDay = day;
            }
            if (DualState.FirstBusiness == 1 && day >= DualState.BusinessDueDay)
            {
                return QueueBuyer(manager, day, false);
            }

            // Ongoing referral traffic. One roll per day; a blocked queue does
            // not get re-rolled later the same day.
            if (DualState.ReferralDay != day)
            {
                DualState.ReferralDay = day;
                DualState.ReferralRoll = UnityEngine.Random.value < ReferralChance();
                DualState.ReferralBlockedToday = false;
            }
            if (!DualState.ReferralRoll) return false;
            if (DualState.ReferralBlockedToday) return false;
            if (DualState.LastExtraBuyerDay >= 0 && day < DualState.LastExtraBuyerDay + 2) return false;
            if (!HasFirstItem()) return false;

            if (HasBusinessClient(manager))
            {
                DualState.ReferralBlockedToday = true;
                return false;
            }
            return QueueBuyer(manager, day, false);
        }

        private static int FirstEarliestDay()
        {
            int day = DualState.SceneDay("dr02");
            if (day < 0) day = DualState.SceneDay("dr01");
            return day < 0 ? 0 : day + 1;
        }

        private static float ReferralChance()
        {
            // The 75% reputation stage only applies while the dual romance is
            // still active or completed. After an exit, keep the base plaque.
            return DualState.Confirmed ? 0.75f : 0.50f;
        }

        private static bool HasBusinessClient(StoreClientManager manager)
        {
            if (manager?.clientStack == null) return false;
            return manager.clientStack.Any(c => c != null && VanillaEleanorBridge.IsOurClient(c)
                && !string.IsNullOrEmpty(c.identifier)
                && c.identifier.Contains("dual_biz"));
        }

        public static StoreClient CreateFirstBuyer(int serial)
        {
            StoreClient c = EleanorStory.NewNpcClient("dual_biz_first_" + serial,
                L.Get("er_name_upper_buyer"),
                PortraitCompat.Resolve(SpriteDict.GetRandomUpperFemaleSpriteName()),
                EleanorStory.FactionKind.Upper);
            c.clientIntent = StoreClient.ClientIntent.BUY;
            c.clientBuyingTagList.Add(TypeHelper.LUXURY_ITEM);
            c.clientBuyingTagList.Add(TypeHelper.ART);
            c.clientBuyingTagList.Add(TypeHelper.HIGHEND);
            c.SetBudget(FirstBudget);
            c.SetVariety();
            c.willAcceptItem = item => IsFirstItem(item) && !item.IsTag(PaidTag);
            c.onItemBought += (item, paid) =>
            {
                if (item == null || item.IsTag(PaidTag)) return;
                item.EnableTag(PaidTag);
                DualState.FirstTraded = true;
                DualState.FirstBusiness = 3;
                c.OverrideBudget(0);
                c.CompleteBusiness();
                TryMarkFeedback();
            };
            var root = StoryEngine.Text(c.displayName, L.Get("er_dual_biz_first_intro", FirstBudget));
            StoryEngine.InterceptDismiss(c, () =>
                StoryEngine.Text(c.displayName, DualState.FirstTraded
                    ? L.Get("er_dual_biz_first_close")
                    : L.Get("er_dual_biz_first_no_trade")),
                () =>
                {
                    if (!DualState.FirstTraded) DualState.FirstBusiness = 4;
                    DualState.LastExtraBuyerDay = DualState.Today;
                    CompleteBusinessVisit();
                });
            c.mainDialogue = root;
            c.CompleteClientCreation();
            return c;
        }

        public static StoreClient CreateSecondBuyer(int serial)
        {
            StoreClient c = EleanorStory.NewNpcClient("dual_biz_second_" + serial,
                L.Get("er_name_upper_buyer"),
                PortraitCompat.Resolve(SpriteDict.GetRandomUpperMaleSpriteName()),
                EleanorStory.FactionKind.Upper);
            c.clientIntent = StoreClient.ClientIntent.BUY;
            c.clientBuyingTagList.Add("HOUSEHOLD_GOOD");
            c.clientBuyingTagList.Add(TypeHelper.TOOL);
            c.SetBudget(SecondBudget);
            c.SetVariety();
            c.willAcceptItem = item => IsSecondItem(item) && !item.IsTag(PaidTag);
            c.onItemBought += (item, paid) =>
            {
                if (item == null || item.IsTag(PaidTag)) return;
                item.EnableTag(PaidTag);
                DualState.SecondTraded = true;
                DualState.SecondBusiness = 3;
                c.OverrideBudget(0);
                c.CompleteBusiness();
                TryMarkFeedback();
            };
            var root = StoryEngine.Text(c.displayName, L.Get("er_dual_biz_second_intro", SecondBudget, SecondMinDurability));
            StoryEngine.InterceptDismiss(c, () =>
                StoryEngine.Text(c.displayName, DualState.SecondTraded
                    ? L.Get("er_dual_biz_second_close")
                    : L.Get("er_dual_biz_second_no_trade")),
                () =>
                {
                    if (!DualState.SecondTraded) DualState.SecondBusiness = 4;
                    DualState.LastExtraBuyerDay = DualState.Today;
                    CompleteBusinessVisit();
                });
            c.mainDialogue = root;
            c.CompleteClientCreation();
            return c;
        }

        private static bool QueueBuyer(StoreClientManager manager, int day, bool second)
        {
            int serial = DualState.Int(second ? "biz.second.serial" : "biz.first.serial") + 1;
            DualState.SetInt(second ? "biz.second.serial" : "biz.first.serial", serial);
            StoreClient client = second ? CreateSecondBuyer(serial) : CreateFirstBuyer(serial);
            if (client == null) return false;
            ModHelper.SetClientSource(client, ModConstants.ModId);
            manager.AddClient(client);
            if (manager.clientStack == null || !manager.clientStack.Contains(client)) return false;
            DualState.BusinessDueDay = -1;
            DualState.LastExtraBuyerDay = day;
            DualState.ReferralBlockedToday = true;
            ModEntry.Log("Dual business buyer queued: " + client.identifier);
            return true;
        }

        private static void TryMarkFeedback()
        {
            if (!DualState.FirstTraded || !DualState.SecondTraded || DualState.FeedbackShown) return;
            DualState.FeedbackPending = true;
            ModEntry.Log("Dual business feedback qualified.");
        }

        private static void CompleteBusinessVisit()
        {
            if (DualState.FeedbackPending && !DualState.FeedbackShown)
            {
                DualState.FeedbackPending = false;
                DualState.FeedbackShown = true;
                DualState.CooldownUntil = DualState.Today + 2;
                StoryDirector.Notify(L.Get("er_dual_biz_feedback_notice"));
            }
        }

        /// <summary>A lost plaque can be reissued once the supported storages no longer contain it.</summary>
        public static bool CanReissuePlaque()
        {
            return DualState.PlaqueReceived && !PlaqueExists()
                && DualState.PlaqueRetryDay <= DualState.Today;
        }

        public static void MarkPlaqueLost()
        {
            if (!DualState.PlaqueReceived) return;
            DualState.PlaqueLost = true;
            DualState.PlaqueRetryDay = DualState.Today + 1;
            ModEntry.Log("Dual plaque missing from supported storages; reissue allowed.");
        }

        public static void ArrangeSecondBusiness(int dueDay)
        {
            if (DualState.SecondTraded) return;
            DualState.SecondBusiness = 1;
            DualState.BusinessDueDay = Math.Max(DualState.Today + 1, dueDay);
        }

        public static bool GivePlaque()
        {
            if (DualState.PlaqueReceived && PlaqueExists()) return true;
            try
            {
                GameItem item = ItemSpawner.Spawn(ModConstants.PlaqueItemId);
                if (item == null) return false;
                if (!StoryEngine.DeliverToPlayer(item)) return false;
                DualState.PlaqueReceived = true;
                DualState.PlaqueLost = false;
                DualState.PlaqueRetryDay = -1;
                ModEntry.Log("Dual recommendation plaque delivered.");
                return true;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Dual plaque delivery failed: " + ex);
                return false;
            }
        }
    }
}
