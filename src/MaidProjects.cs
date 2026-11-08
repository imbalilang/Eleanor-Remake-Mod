using System;
using System.Collections.Generic;
using HarmonyLib;

namespace EleanorRomance
{
    /// <summary>One service for both native purchase UIs. No dialogue callback charges resources.</summary>
    public static class MaidProjects
    {
        public sealed class Plan
        {
            public readonly string Id;
            public readonly int Project, BaseCost, DiscountCost, UpperGate, LowerGate;
            public readonly bool Legacy;
            public Plan(int project, string suffix, int cash, int discount = 0, int upperGate = 0, int lowerGate = 0)
            { Project = project; Id = "er_maid_p" + project + "_" + suffix; BaseCost = cash; DiscountCost = discount; UpperGate = upperGate; LowerGate = lowerGate; Legacy = suffix == "favor"; }
        }
        public static readonly Plan[] Plans = {
            new Plan(1,"cash",1000), new Plan(2,"cash",3000,1500,lowerGate:30),
            new Plan(3,"cash",5000,2500,upperGate:40),
            new Plan(4,"cash",8000,4000,upperGate:50,lowerGate:50)
        };
        // Recognize old saved rows/clicks and preserve payment history, but never
        // register or offer the retired alternatives in a new game.
        private static readonly Plan[] LegacyPlans = {
            new Plan(2,"favor",1500), new Plan(3,"favor",2500), new Plan(4,"favor",4000)
        };
        private static IEnumerable<Plan> AllPlans()
        { foreach(var p in Plans) yield return p; foreach(var p in LegacyPlans) yield return p; }
        private static bool registered, busy, deferredSave, refreshing;
        private static string unlockAllowance;
        public static bool TransactionActive => busy;
        public static bool IsOurs(string id) => Find(id) != null;
        public static Plan Find(string id) { foreach (var p in AllPlans()) if (p.Id == id) return p; return null; }
        private static string Key(int p, string field) => "p" + p + "." + field;
        public static bool Paid(int p) => MaidState.Flag(Key(p,"paid"));
        public static bool ResultShown(int p) => MaidState.Flag(Key(p,"shown"));
        public static int PaidDay(int p) => MaidState.Int(Key(p,"day"), -1);
        // Result dialogue acknowledges connections used at payment time, both
        // for historical favor plans and for the new reputation discount.
        public static bool FavorPlan(int p) => MaidState.Flag(Key(p,"discount"))
            || MaidState.Text(Key(p,"plan")).EndsWith("_favor", StringComparison.Ordinal);
        public static bool DiscountEligible(Plan p) => !p.Legacy && p.DiscountCost > 0
            && (p.UpperGate <= 0 || StoryEngine.UlRep() >= p.UpperGate)
            && (p.LowerGate <= 0 || StoryEngine.LlRep() >= p.LowerGate);
        public static int BaseQuote(Plan p) => DiscountEligible(p) ? p.DiscountCost : p.BaseCost;
        // A neutral native quote retains difficulty, faction perks, starting
        // perks and native rounding without applying our project rule twice.
        private static int NativePrice(int cost) => Math.Max(0,new NetworkUpgrade("") { cost=cost }.GetCost());
        public static bool ResultPending(int p) => Paid(p) && !ResultShown(p);
        public static void ShowResult(int p) { if (Paid(p)) MaidState.Flag(Key(p,"shown"),true); }

        public static bool Available(int p)
        {
            if (!RomanceState.StoryActive || !MaidState.Active || !MaidState.SafeMother || Paid(p)) return false;
            // Reveal after completed story beats, never by calendar day:
            // reunion (16), base conclusion + housing feedback (23),
            // legal return (24), and the procurement visit (26).
            if (p == 1) return true;
            if (!MaidState.BasicComplete || !MaidState.Lovers) return false;
            if (p == 2) return ResultShown(1);
            if (p == 3) return MaidState.Progress >= 24;
            if (p == 4) return MaidState.Progress >= 26;
            return false;
        }

        public static void Register()
        {
            // Called only during LoadGame or after a new store has started.
            // PlayerStore.Instance is a creating getter, not a readiness probe.
            if (!VersionGate.IsSupported) return;
            if (registered) return;
            foreach (Plan plan in Plans)
            {
                Plan captured = plan;
                // AddNetworkUpgrade's final bool is "recommended", not "hidden".
                // Keep the source definition hidden too: the native UI rebuilds
                // its definition cache from these templates whenever it opens.
                var definition = new NetworkUpgrade(plan.Id)
                {
                    source = ModConstants.ModId, cost = plan.BaseCost,
                    tab = NetworkUpgrade.DEFAULT_TAB, iconKey = "yes",
                    state = 0, hidden = true,
                    titleProvider = () => L.Get("er_maid_project_" + captured.Project),
                    descriptionProvider = () => Description(captured),
                    subtitleProvider = () => L.Get("er_maid_projects_subtitle"),
                    alreadyBoughtProvider = () => L.Get("er_maid_paid")
                };
                NetworkUpgrade.RegisterModUpgrade(definition, () => { });
            }
            registered = true;
        }
        public static void Unregister() { ModHelper.RemoveNetworkUpgrades(ModConstants.ModId); registered = false; }
        public static void OnLoaded()
        {
            Register();
            Recover();
            NetworkUpgrade.AddMissingAfterLoad();
            RepairAbnormalUnlocks();
            RefreshStates();
            NotifyRepairNotice();
        }

        /// <summary>
        /// Only the current step and completed steps are listed.  A paid project
        /// remains as a settled record even after the agreement ends; unpaid
        /// projects follow the story gate.
        /// </summary>
        public static bool Visible(Plan plan)
        {
            if (plan == null || plan.Legacy) return false;
            if (!VersionGate.IsSupported || !RomanceState.StoryActive || !MaidState.HasHistory) return false;
            // A terminated relationship keeps no active project list; delivered
            // story results remain in the save/history but the rows are hidden.
            if (RomanceState.Route == 4) return false;
            if (Paid(plan.Project))
            {
                return true; // One settled row, including old connections payments.
            }
            return Available(plan.Project);
        }
        private static bool ProjectVisible(int project)
        {
            foreach (var plan in Plans) if (plan.Project == project && Visible(plan)) return true;
            return false;
        }
        public static void RefreshStates(bool notifyNew = false)
        {
            if (refreshing) return;
            refreshing = true;
            try
            {
                var store = PlayerStore.Instance;
                var dict = store != null ? store.networkUpgrade : null;
                foreach (var p in AllPlans())
                {
                    bool visible = Visible(p);
                    int state = !visible ? 0 : Paid(p.Project) ? 2 : Available(p.Project) ? 1 : 0;
                    if (dict != null && dict.TryGetValue(p.Id, out var u) && u != null)
                    {
                        u.state = state;
                        u.hidden = !visible;
                    }
                    var definition = NetworkUpgrade.GetDefinition(p.Id);
                    if (definition != null) definition.hidden = !visible;
                }
                if (notifyNew)
                    for (int project = 1; project <= 4; project++) NotifyProjectOpen(project);
            }
            finally { refreshing = false; }
        }
        private static void NotifyProjectOpen(int project)
        {
            string flag = "notified.p" + project;
            if (MaidState.Flag(flag) || !ProjectVisible(project)) return;
            MaidState.Flag(flag, true);
            StoryDirector.Notify(L.Get("er_maid_notice_project_open", L.Get("er_maid_project_" + project)));
        }

        public sealed class Quote
        {
            public int Total, Favor, Cash, UpperNow, LowerNow, CashNow;
            public bool Discount;
        }
        public static Quote GetQuote(Plan plan)
        {
            var store = PlayerStore.Instance;
            bool discount = DiscountEligible(plan);
            var upgrade = ModHelper.GetNetworkUpgrade(plan.Id);
            int total = Math.Max(0,upgrade != null ? upgrade.GetCost() : NativePrice(BaseQuote(plan)));
            int favor = Math.Min(total, Math.Max(0, store?.wildFavor ?? 0));
            return new Quote { Total = total, Favor = favor, Cash = total - favor, Discount = discount,
                UpperNow = StoryEngine.UlRep(), LowerNow = StoryEngine.LlRep(), CashNow = store?.GetCash() ?? 0 };
        }
        public static string Description(Plan p)
        {
            string text = L.Get("er_maid_project_desc_" + p.Project);
            if(p.Project>=3)
                text+="\n"+L.Get(p.Project==3?"er_div_project_first":"er_div_project_established");
            if (Paid(p.Project))
            {
                text+="\n\n"+L.Get(ResultShown(p.Project)?"er_maid_result_done":"er_maid_result_waiting");
                if(p.Project>=3 && MaidDividends.LastDay>=0)text+="\n"+MaidDividends.Summary();
                return text;
            }
            if (p.DiscountCost > 0)
            {
                int normal=NativePrice(p.BaseCost), reduced=NativePrice(p.DiscountCost);
                string terms=p.UpperGate>0 && p.LowerGate>0
                    ? L.Get("er_maid_discount_both",p.UpperGate,p.LowerGate,normal,reduced)
                    : L.Get(p.UpperGate>0?"er_maid_discount_upper":"er_maid_discount_lower",
                        Math.Max(p.UpperGate,p.LowerGate),normal,reduced);
                text+="\n\n"+terms+"\n"+L.Get(DiscountEligible(p)?"er_maid_discount_active":"er_maid_discount_pending");
            }
            return text;
        }

        public static bool TryPurchase(string id)
        {
            Plan p = Find(id);
            if (p == null || busy) return false;
            if (p.Legacy) { RefreshStates(); return false; }
            if (Paid(p.Project)) { StoryDirector.Notify(L.Get("er_maid_paid")); return false; }
            if (!Available(p.Project)) { StoryDirector.Notify(L.Get("er_maid_prereq_" + p.Project)); return false; }
            Quote q = GetQuote(p);
            int cashMissing = Math.Max(0,q.Cash-q.CashNow);
            if (cashMissing > 0)
            {
                StoryDirector.Notify(L.Get("er_maid_project_cash_missing",cashMissing));
                return false;
            }
            bool success = false;
            busy = true;
            try
            {
                Snapshot(p.Project,id);
                PlayerStore store = PlayerStore.Instance;
                store.wildFavor -= q.Favor;
                store.ModCash(-q.Cash,false);
                MaidState.Text(Key(p.Project,"plan"),id);
                MaidState.Flag(Key(p.Project,"discount"),q.Discount);
                MaidState.Text(Key(p.Project,"transaction"),MaidState.Text("tx.id"));
                MaidState.SetInt(Key(p.Project,"cash"),q.Cash);
                MaidState.SetInt(Key(p.Project,"favor"),q.Favor);
                MaidState.SetInt(Key(p.Project,"upper"),0);
                MaidState.SetInt(Key(p.Project,"lower"),0);
                MaidState.SetInt(Key(p.Project,"day"),MaidState.Today);
                MaidState.Flag(Key(p.Project,"shown"),false);
                MaidState.Flag(Key(p.Project,"paid"),true);
                RefreshStates();
                // All writes and resources are in one synchronous section. SaveGame and
                // Save are deferred by the local guards until this boundary is reached.
                MaidState.SetInt("tx.phase",2);
                success = true;
            }
            catch (Exception ex)
            {
                try { Rollback(); } catch (Exception rollback) { ModEntry.LogError("Project recovery failed: " + rollback); }
                ModEntry.LogError("Project purchase rolled back: " + ex);
            }
            finally
            {
                busy = false;
                if (deferredSave)
                {
                    deferredSave = false;
                    FlushSave();
                }
            }
            if (success)
            {
                // Statistics are observational, after the atomic commit.
                try { RunStats.OnWildUpgradeBought(q.Cash); } catch (Exception ex) { ModEntry.LogError("Upgrade stats: " + ex); }
                unlockAllowance = id;
                try { ModHelper.GetNetworkUpgrade(id)?.Unlock(); }
                catch (Exception ex) { ModEntry.LogError("Committed upgrade notification: " + ex); }
                finally { if (unlockAllowance == id) unlockAllowance = null; }
            }
            StoryDirector.Notify(L.Get(success ? "er_maid_purchase_success" : "er_maid_purchase_failed"));
            return success;
        }

        public static bool TryEnter()
        {
            if (MaidState.HasHistory) return MaidState.EntryKind==1 && MaidState.Contract==1;
            if (busy || !StoryEngine.HasCash(800)) return false;
            bool success=false;
            busy=true;
            try
            {
                Snapshot(0,"entry");
                PlayerStore.Instance.ModCash(-800,false);
                MaidState.Enter(1);
                MaidState.Text("entry_transaction",MaidState.Text("tx.id"));
                MaidState.SetInt("tx.phase",2);
                success=true;
            }
            catch(Exception ex){ Rollback(); ModEntry.LogError("Entry payment rolled back: "+ex); }
            finally
            {
                busy=false;
                if(deferredSave){ deferredSave=false;FlushSave(); }
            }
            return success;
        }

        private static void Snapshot(int project,string id)
        {
            MaidState.Text("tx.id",Guid.NewGuid().ToString("N"));
            MaidState.SetInt("tx.project",project);
            MaidState.Text("tx.plan",id);
            MaidState.SetInt("tx.cash",PlayerStore.Instance.GetCash());
            MaidState.SetInt("tx.favor",PlayerStore.Instance.wildFavor);
            MaidState.Number("tx.upper",StoreReputation.GetStoreReputation("FACTION_UPPER_LEVEL").GetReputationExact());
            MaidState.Number("tx.lower",StoreReputation.GetStoreReputation("FACTION_LOWER_LEVEL").GetReputationExact());
            MaidState.SetInt("tx.phase",1);
        }
        private static void Rollback()
        {
            if (MaidState.Int("tx.phase") != 1) return;
            int p = MaidState.Int("tx.project");
            PlayerStore store = PlayerStore.Instance;
            store.ModCash(MaidState.Int("tx.cash")-store.GetCash(),false);
            store.wildFavor = MaidState.Int("tx.favor");
            StoreReputation.GetStoreReputation("FACTION_UPPER_LEVEL").SetReputation(MaidState.Number("tx.upper"));
            StoreReputation.GetStoreReputation("FACTION_LOWER_LEVEL").SetReputation(MaidState.Number("tx.lower"));
            if(p==0)
            {
                MaidState.EntryKind=0; MaidState.EntryPaidCash=0; MaidState.Contract=0;
                MaidState.Text("entry_transaction","");
            }
            MaidState.Flag(Key(p,"paid"),false);
            MaidState.Text(Key(p,"plan"),"");
            foreach (string field in new[] { "cash","favor","upper","lower","day" }) MaidState.SetInt(Key(p,field),0);
            MaidState.Flag(Key(p,"shown"),false);
            MaidState.Flag(Key(p,"discount"),false);
            MaidState.SetInt("tx.phase",3);
            RefreshStates();
        }
        public static void Recover()
        {
            busy = false; deferredSave = false; refreshing = false; unlockAllowance = null;
            if (MaidState.Int("tx.phase") == 1) Rollback();
        }

        private static int NativeCost(Plan plan, NetworkUpgrade upgrade)
        {
            try { return Math.Max(0, upgrade != null ? upgrade.GetCost() : plan.BaseCost); }
            catch (Exception ex) { ModEntry.LogError("Native quote failed for " + plan.Id + ": " + ex); return plan.BaseCost; }
        }

        /// <summary>
        /// A 0.68 save can contain native-unlocked ids with no transaction record.
        /// Restore the story-gated state and refund the load-time native quote as
        /// cash once per id.  No story result or reputation is invented.
        /// </summary>
        private static void RepairAbnormalUnlocks()
        {
            var store = PlayerStore.Instance;
            var dict = store != null ? store.networkUpgrade : null;
            if (dict == null) return;
            int total = 0, repaired = 0;
            foreach (var plan in AllPlans())
            {
                if (Paid(plan.Project)) continue;
                string flag = "repair." + plan.Id;
                if (MaidState.Flag(flag)) continue;
                if (!dict.TryGetValue(plan.Id, out var upgrade) || upgrade == null || upgrade.state != 2) continue;
                // These pre-transaction unlocks predate the new discount rule.
                int refund = NativePrice(plan.BaseCost);
                if (refund > 0)
                    try { store.ModCash(refund, false); }
                    catch (Exception ex) { ModEntry.LogError("Refund failed for " + plan.Id + ": " + ex); }
                MaidState.Flag(flag, true);
                upgrade.state = 0; upgrade.hidden = true;
                var definition = NetworkUpgrade.GetDefinition(plan.Id);
                if (definition != null) definition.hidden = true;
                total += refund; repaired++;
                ModEntry.Log("Revoked premature maid project unlock id=" + plan.Id + " refund=" + refund + " day=" + MaidState.Today);
            }
            if (repaired > 0) QueueRepairNotice(total);
        }

        private static void QueueRepairNotice(int amount)
        {
            int total = MaidState.Flag("repair.pending") ? MaidState.Int("repair.total") : 0;
            MaidState.SetInt("repair.total", total + Math.Max(0, amount));
            MaidState.Flag("repair.pending", true);
            NotifyRepairNotice();
        }

        /// <summary>Shows the repair summary once, when a notification UI is available.</summary>
        internal static void NotifyRepairNotice()
        {
            if (!MaidState.Flag("repair.pending") || MaidState.Flag("repair.notified")) return;
            if (!UiReady()) return;
            MaidState.Flag("repair.notified", true);
            StoryDirector.Notify(L.Get("er_maid_notice_repair", MaidState.Int("repair.total")));
        }

        private static bool UiReady()
        {
            try { return StoreUIManager.Instance != null; }
            catch { return false; }
        }

        internal static bool ConsumeUnlockAllowance(string id)
        {
            if (!string.Equals(unlockAllowance, id, StringComparison.Ordinal)) return false;
            unlockAllowance = null;
            return true;
        }

        /// <summary>Last line of defence for any native unlock path not audited into TryPurchase.</summary>
        internal static void RejectUnapprovedUnlock(NetworkUpgrade upgrade, Plan plan)
        {
            string flag = "repair." + plan.Id;
            ModEntry.LogError("Unapproved maid project unlock blocked: id=" + plan.Id
                + " day=" + MaidState.Today + "\n" + Environment.StackTrace);
            int refund = 0;
            if (!MaidState.Flag(flag))
            {
                refund = NativeCost(plan, upgrade);
                if (refund > 0)
                    try { PlayerStore.Instance.ModCash(refund, false); }
                    catch (Exception ex) { ModEntry.LogError("Unapproved-unlock refund failed for " + plan.Id + ": " + ex); }
                MaidState.Flag(flag, true);
                QueueRepairNotice(refund);
            }
            if (upgrade != null) { upgrade.state = 0; upgrade.hidden = true; }
            var definition = NetworkUpgrade.GetDefinition(plan.Id);
            if (definition != null) definition.hidden = true;
        }
        public static bool BeforeSave()
        {
            if (!busy) return true;
            deferredSave = true;
            return false;
        }
        private static void FlushSave()
        {
            try { PlayerStore.Instance?.SaveGame(); }
            catch(Exception ex)
            {
                ModEntry.LogError("Deferred save failed after an atomic purchase; committed in-memory state kept: "+ex);
                StoryDirector.Notify(L.Get("er_maid_save_retry"));
            }
        }
    }

    // BuildTabs runs after native Open invalidates/recreates the definitions.
    // Refresh before its hidden filter/layout, rather than during IsReady,
    // when the cards (and their spoiler text) have already been built.
    [HarmonyPatch(typeof(WildNetworkUI), "BuildTabs")]
    internal static class MaidProjectLayoutPatch
    {
        private static void Prefix() => MaidProjects.RefreshStates();
    }
    [HarmonyPatch(typeof(WildUIManager), "OpenUI")]
    internal static class MaidLegacyProjectLayoutPatch
    {
        private static void Prefix() => MaidProjects.RefreshStates();
    }

    [HarmonyPatch(typeof(WildUIManager),"OnUnlockClicked")]
    internal static class MaidLegacyPurchasePatch
    {
        private static bool Prefix(WildUIManager __instance,string ___currentSelectedId)
        { if (!MaidProjects.IsOurs(___currentSelectedId)) return true; MaidProjects.TryPurchase(___currentSelectedId); __instance.OnElementClicked(___currentSelectedId); return false; }
    }
    [HarmonyPatch(typeof(WildNetworkUI),"OnUseClicked")]
    internal static class MaidNetworkPurchasePatch
    {
        private static bool Prefix(string ___selectedId)
        { if (!MaidProjects.IsOurs(___selectedId)) return true; MaidProjects.TryPurchase(___selectedId); WildNetworkUI.RefreshAll(); return false; }
    }
    [HarmonyPatch(typeof(NetworkUpgrade),"IsReady")]
    internal static class MaidProjectReadyPatch
    {
        private static void Postfix(NetworkUpgrade __instance,ref bool __result)
        {
            var p=MaidProjects.Find(__instance.id);
            if (p == null) return;
            // List rendering asks readiness for each row; use that as the
            // Wilde-UI open/rebuild refresh point without guessing private
            // screen method names.
            MaidProjects.RefreshStates();
            __result=!p.Legacy && MaidProjects.Available(p.Project);
        }
    }
    [HarmonyPatch(typeof(NetworkUpgrade),"GetCost")]
    internal static class MaidProjectCostPatch
    {
        private static void Prefix(NetworkUpgrade __instance)
        {
            var p=MaidProjects.Find(__instance.id);
            if(p!=null && !p.Legacy) __instance.cost=MaidProjects.BaseQuote(p);
        }
    }
    [HarmonyPatch(typeof(NetworkUpgrade),"Unlock")]
    internal static class MaidProjectUnlockGuardPatch
    {
        private static bool Prefix(NetworkUpgrade __instance)
        {
            if (__instance == null) return true;
            var plan = MaidProjects.Find(__instance.id);
            if (plan == null) return true;
            if (plan.Legacy) return false;
            if (MaidProjects.Paid(plan.Project)) return true;
            if (MaidProjects.ConsumeUnlockAllowance(__instance.id)) return true;
            MaidProjects.RejectUnapprovedUnlock(__instance, plan);
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerStore),"SaveGame")]
    internal static class MaidSaveBoundaryPatch { private static bool Prefix() => MaidProjects.BeforeSave(); }
    [HarmonyPatch(typeof(SaveManager),"Save")]
    internal static class MaidItemSaveBoundaryPatch { private static bool Prefix() => MaidProjects.BeforeSave(); }
}
