using System;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// F9 debug-panel tools.  These are test shortcuts only; they change story
    /// state directly and do not represent a normal playthrough.
    /// </summary>
    public static class DebugTools
    {
        private static float resetArmedUntil = -1f;

        public static void Register()
        {
            Add("重置埃莉诺剧情线 / Reset Eleanor story", ResetStory, tall: true);
            Add("测试：准备资源（+10000 信用点，全声望满）", PrepareResources, tall: true);
            Add("测试：打印当前剧情状态", PrintState);
            Add("测试：重置并跳到第 1 章", ResetAndChapter1, tall: true);
            Add("测试：立刻进入下一章", QueueNextChapter, tall: true);

            Add("测试：第 4 章 假叔叔", TestUncle);
            Add("测试：第 5 章 警告", TestWarning);
            Add("测试：第 5 章 救援", TestRescue);
            Add("测试：第 7 章 拾荒", TestScavenger);
            Add("测试：第 10 章 莉迪亚", TestLydia);
            Add("测试：后宫线入口（手镯）", TestD01, tall: true);
            Add("测试：后宫线 v2 第一幕（怀表）", TestDv01, tall: true);
            Add("测试：后宫线 v2 矿石交易", TestDv03, tall: true);
            Add("测试：后宫线 v2 保护（关门前）", TestDv06, tall: true);
            Add("测试：后宫线 v2 黑幕（开门）", TestDv10, tall: true);
            Add("测试：后宫线 v2 三人确认 / E4（点头）", TestDv13, tall: true);
            Add("测试：多人演出 POC / Multi presentation", TestPresentation, true);
            Add("测试：第 12 章 调查款（正常）", TestSavingsNormal);
            Add("测试：第 12 章 调查款（逾期补款）", TestSavingsLate);
            Add("测试：第 13 章 数据盘", TestDrive);
            Add("测试：第 14 章 威胁", TestThreat);
            Add("测试：第 15 章 方向选择", TestDecision);
            Add("测试：第 16 章 准备（研究保留）", TestPreparationB);
            Add("测试：第 17 章 团聚（研究归还）", TestReunionA);
            Add("测试：第 17 章 团聚（研究保留）", TestReunionB);
            Add("测试：第 20 章 表白", TestConfession);
            Add("测试：第 21 章 合作买家", TestCooperation);
            Add("测试：第 24 章 恋爱收束", TestConclusionRomance);
            Add("测试：第 24 章 朋友收束", TestConclusionFriends);
            Add("测试：恋爱线 第一幕A（日用品）", () => TestLoveVisit(1), true);
            Add("测试：恋爱线 第一幕B（营养果）", () => TestLoveVisit(2), true);
            Add("测试：恋爱线 第一幕C（关门前）", () => TestLoveVisit(3), true);
            Add("测试：恋爱线 第二幕A（停水）", () => TestLoveVisit(4), true);
            Add("测试：恋爱线 第二幕B（价签）", () => TestLoveVisit(5), true);
            Add("测试：恋爱线 第二幕C（家常衣服）", () => TestLoveVisit(6), true);
            Add("测试：恋爱线 第三幕A（旧友）", () => TestLoveVisit(7), true);
            Add("测试：恋爱线 第三幕B（求婚）", () => TestLoveVisit(8), true);
            Add("测试：恋爱线 第三幕C（名单）", () => TestLoveVisit(9), true);
            Add("测试：恋爱线 第三幕D（婚礼）", () => TestLoveVisit(10), true);
            Add("测试：打印恋爱线状态 / Print love state", PrintLoveState);
            Add("测试：重置恋爱线 / Reset love", ResetLove, true);
            Add("测试：革命军线第 1 段", TestRouteC);
            Add("测试：失败分支（擅自交盘）", TestRouteD);
            Add("测试：后日谈", TestEpilogue);
            Add("测试：旧档档案员", TestOldSavePrompt);
            Add("测试：通关后重置档案员", TestRouteResetPrompt, true);
            Add("测试：好感 +5", AddAffection);
            Add("标记完整恋爱线（测试）", MarkRomanceComplete);
            RegisterMaidTests();
            RegisterDualTests();
        }

        private static void RegisterDualTests()
        {
            Add("测试：双恋入口 / Dual entry", TestDualEntry, true);
            Add("测试：双恋 dr07 / Dual dr07", () => TestDualScene("dr07"), true);
            Add("测试：双恋 dr17 / Dual dr17", () => TestDualScene("dr17"), true);
            Add("测试：打印双恋状态 / Print dual state", PrintDualState);
            Add("测试：重置双恋 / Reset dual", () => { DualState.Reset(); StoryDirector.Notify("双恋状态已重置。"); }, true);
        }

        private static void TestDualEntry()
        {
            SetupCommon();
            RomanceState.MotherState = 1;
            RomanceState.Route = 1;
            RomanceState.Chapter = 21;
            RomanceState.NextVisitDay = 0;
            RomanceState.UncleReported = true;
            DualState.Reset();
            DualState.FeatureVersion = DualState.CurrentVersion;
            bool queued = DualDirector.ForceQueue("entry");
            StoryDirector.Notify(queued ? "已排队双恋入口。" : "双恋入口排队失败。");
        }

        private static void TestDualScene(string id)
        {
            SetupCommon();
            RomanceState.MotherState = 1;
            RomanceState.Route = 1;
            RomanceState.Chapter = 21;
            RomanceState.Romance = 1;
            RomanceState.UncleReported = true;
            RomanceState.FairDeals = 3;
            DualState.Reset();
            DualState.Accept(1);
            foreach (string prior in new[] { "dr01", "dr02", "dr03", "dr04", "dr05", "dr06", "dr07", "dr08", "dr09", "dr10", "dr11", "dr12", "dr13", "dr14", "dr15", "dr16" })
            {
                if (prior == id) break;
                DualState.Flag("scene." + prior, true);
            }
            DualState.Flag("scene.dr05", true);
            if (id != "dr07" && id != "dr17") DualState.FirstTraded = true;
            bool queued = DualDirector.ForceQueue(id);
            StoryDirector.Notify(queued ? "已排队双恋场景 " + id : "双恋场景 " + id + " 排队失败。");
        }

        private static void TestLoveVisit(int visit)
        {
            SetupCommon();
            RomanceState.MotherState = 1;
            RomanceState.Route = 2;
            RomanceState.Chapter = 21;
            RomanceState.NextVisitDay = 0;
            RomanceState.UncleReported = true;
            StoryCoreFacts.Confess(1);
            LoveState.Reset();
            bool queued = LoveDirector.ForceQueue(visit);
            StoryDirector.Notify(queued
                ? "已排队恋爱线场景 " + visit
                : "恋爱线场景 " + visit + " 排队失败。");
        }

        private static void PrintLoveState()
        {
            StoryDirector.Notify("Love started=" + LoveState.Started
                + " active=" + LoveState.Active
                + " a1=" + LoveState.T1Done + "/" + LoveState.T2Done + "/" + LoveState.ClosingDone
                + " act1=" + LoveState.Act1Done
                + " a2=" + LoveState.A2T1Done + "/" + LoveState.A2T2Done + "/" + LoveState.A2T3Done
                + " act2=" + LoveState.Act2Done
                + " a3=" + LoveState.A3T1Done + "/" + LoveState.A3T2Done + "/" + LoveState.A3T3Done
                + " wedding=" + LoveState.WeddingDone
                + " act3=" + LoveState.Act3Done
                + " next=" + LoveState.NextVisitDay);
        }

        private static void ResetLove()
        {
            LoveState.Reset();
            StoryDirector.Notify("恋爱线状态已重置。");
        }

        private static void PrintDualState()
        {
            StoryDirector.Notify("Dual entry=" + DualState.Entry
                + " rel=" + DualState.Relation
                + " plaque=" + DualState.PlaqueReceived + "/" + DualBusiness.PlaquePlaced()
                + " biz=" + DualState.FirstBusiness + "/" + DualState.SecondBusiness
                + " traded=" + DualState.FirstTraded + "/" + DualState.SecondTraded
                + " core=" + DualState.CoreCommitted
                + " pending=" + DualState.PendingScene);
        }

        private static void Add(string label, Action action, bool tall = false)
        {
            ModHelper.AddDebugFunction(
                ModConstants.ModId,
                ModHelper.DEBUG_CATEGORY_COMMAND,
                label,
                action,
                tall);
        }

        public static void Unregister()
        {
            try
            {
                ModHelper.RemoveDebugFunctions(ModConstants.ModId);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Failed to remove debug functions: " + ex);
            }
        }

        private static void ResetStory()
        {
            if (Time.realtimeSinceStartup < resetArmedUntil)
            {
                resetArmedUntil = -1f;
                RomanceState.ManualResetOldSave();
            }
            else
            {
                resetArmedUntil = Time.realtimeSinceStartup + 10f;
                StoryDirector.Notify(L.Get("er_notice_reset_arm"));
            }
        }

        private static void PrepareResources()
        {
            try
            {
                DebugPanel.Add10000Credits();
                DebugPanel.SetRepToMax();
                StoryDirector.Notify("测试资源：+10000 信用点，全部声望拉满。");
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("PrepareResources failed: " + ex);
            }
        }

        private static void PrintState()
        {
            StoryDirector.Notify("Ch=" + RomanceState.Chapter
                + " Pending=" + RomanceState.PendingChapter
                + " Route=" + RomanceState.Route
                + " Mother=" + RomanceState.MotherState
                + " Drive=" + RomanceState.DriveState
                + " Romance=" + RomanceState.Romance
                + " Savings=" + RomanceState.Savings
                + " ScavAssist=" + RomanceState.ScavAssist
                + " PerfumeGiven=" + RomanceState.PerfumeGiven
                + " UncleReported=" + RomanceState.UncleReported
                + " Arrested=" + RomanceState.Arrested
                + " Rescued=" + RomanceState.Rescued);
        }

        private static void ResetAndChapter1()
        {
            SetupCommon();
            QueueTestChapter(1);
        }

        private static void TestUncle()
        {
            SetupCommon();
            QueueTestChapter(4);
        }

        private static void TestWarning()
        {
            SetupCommon();
            RomanceState.UncleAccepted = true;
            RomanceState.UncleInspected = true;
            RomanceState.CrateKept = true;
            QueueTestChapter(5);
        }

        private static void TestRescue()
        {
            SetupCommon();
            RomanceState.UncleAccepted = true;
            RomanceState.UncleInspected = true;
            RomanceState.TrapSprung = true;
            VanillaEleanorBridge.RecordArrest(6);
            PlayerStore.Instance.storeClientData.eleanorArrestDay = StoreStation.Instance.dayCounter - 2;
            RomanceState.LastQueuedDay = -1;
            PlayerStore.Instance.storeClientManager.clientStack.RemoveAll(VanillaEleanorBridge.IsOurClient);
            PlayerStore.Instance.storeClientManager.CheckStoryLineEleanor();
        }

        private static void TestScavenger()
        {
            SetupCommon();
            QueueTestChapter(7);
        }

        private static void TestLydia()
        {
            SetupCommon();
            QueueTestChapter(10);
        }

        private static void TestD01()
        {
            SetupCommon();
            DualState.Reset();
            RomanceState.DPreludeState = 4;
            RomanceState.DPreludeStep = 0;
            RomanceState.DPreludeOriginalPrice = 140;
            RomanceState.MotherState = 1;
            RomanceState.MotherVisited = true;
            RomanceState.Route = 1;
            if (!DualDirector.ForceQueue("entry"))
                StoryDirector.Notify("后宫线第一幕排队失败。");
        }

        private static void SetupDAct2Test()
        {
            SetupCommon();
            DualState.Reset();
            RomanceState.DPreludeState = 4;
            RomanceState.DPreludeStep = 0;
            RomanceState.DPreludeOriginalPrice = 140;
            RomanceState.MotherState = 1;
            RomanceState.MotherVisited = true;
            RomanceState.Route = 1;
            DualState.Accept(1);
            DualState.Flag("d.route", true);
            DualState.Flag("d.act1", true);
        }

        private static void MarkDvDone(int through)
        {
            for (int i = 1; i <= through; i++)
                DualState.Flag("scene.dv" + i.ToString("00"), true);
        }

        private static void TestDv01()
        {
            SetupDAct2Test();
            if (!DualDirector.ForceQueue("dv01"))
                StoryDirector.Notify("后宫线 v2 第一幕排队失败。");
        }

        private static void TestDv03()
        {
            SetupDAct2Test();
            MarkDvDone(2);
            if (!DualDirector.ForceQueue("dv03"))
                StoryDirector.Notify("后宫线 v2 矿石交易排队失败。");
        }

        private static void TestDv06()
        {
            SetupDAct2Test();
            MarkDvDone(5);
            if (!DualDirector.ForceQueue("dv06"))
                StoryDirector.Notify("后宫线 v2 保护事件排队失败。");
        }

        private static void TestDv10()
        {
            SetupDAct2Test();
            MarkDvDone(9);
            DualState.Relation = 1;
            if (!DualDirector.ForceQueue("dv10"))
                StoryDirector.Notify("后宫线 v2 黑幕日排队失败。");
        }

        private static void TestDv13()
        {
            SetupDAct2Test();
            MarkDvDone(12);
            DualState.Relation = 1;
            if (!DualDirector.ForceQueue("dv13"))
                StoryDirector.Notify("后宫线 v2 三人确认排队失败。");
        }

        private static void TestPresentation()
        {
            SetupCommon();
            if (PlayerStore.Instance == null || PlayerStore.Instance.storeClientManager == null)
            {
                StoryDirector.Notify("当前不在可测试的经营场景。");
                return;
            }
            QueueTestClient(CreatePresentationPocClient);
        }

        private static StoreClient CreatePresentationPocClient()
        {
            StoreClient client = EleanorStory.NewNpcClient(
                "presentation_poc", "黑市掮客", "maleBM5", EleanorStory.FactionKind.BlackMarket);
            Dialogue root = StoryEngine.Text(client.displayName, "……");
            Dialogue backup = StoryEngine.Text("SEC", "……");
            Dialogue end = StoryEngine.Text(L.Get("er_name_narrator"), "……");
            root.SetStartAction(() =>
                StoryPresentation.ShowBackups("maleBM5", "maleLower1", "immediate", "掮客", "打手"));
            root.SetNextDialogue(backup);
            backup.SetNextDialogue(end);
            end.SetEndAction(StoryPresentation.Clear);
            client.mainDialogue = root;
            client.OnDismiss += StoryPresentation.Clear;
            client.CompleteClientCreation();
            return client;
        }

        private static void TestSavingsNormal()
        {
            SetupCommon();
            RomanceState.Savings = 800;
            RomanceState.SavingsAutoFilled = false;
            QueueTestChapter(12);
        }

        private static void TestSavingsLate()
        {
            SetupCommon();
            RomanceState.Savings = 0;
            RomanceState.SavingsAutoFilled = true;
            QueueTestChapter(12);
        }

        private static void TestDrive()
        {
            SetupCommon();
            QueueTestChapter(13);
        }

        private static void TestThreat()
        {
            SetupCommon();
            RomanceState.DriveState = 1;
            RomanceState.UncleReported = true;
            QueueTestChapter(14);
        }

        private static void TestDecision()
        {
            SetupCommon();
            RomanceState.DriveState = 1;
            RomanceState.UncleReported = true;
            RomanceState.ThreatRecorded = true;
            RomanceState.Savings = 800;
            QueueTestChapter(15);
        }

        private static void TestPreparationB()
        {
            SetupCommon();
            RomanceState.Route = 2;
            RomanceState.DriveState = 1;
            RomanceState.UncleReported = true;
            RomanceState.ThreatRecorded = true;
            RomanceState.Savings = 800;
            QueueTestChapter(16);
        }

        private static void TestReunionA()
        {
            SetupCommon();
            RomanceState.Route = 1;
            RomanceState.DriveState = 1;
            QueueTestChapter(17);
        }

        private static void TestReunionB()
        {
            SetupCommon();
            RomanceState.Route = 2;
            RomanceState.DriveState = 1;
            RomanceState.ProtectionReady = true;
            QueueTestChapter(17);
        }

        private static void TestConfession()
        {
            SetupCommon();
            RomanceState.Route = 1;
            RomanceState.MotherState = 1;
            RomanceState.Romance = 0;
            RomanceState.FairDeals = 3;
            RomanceState.UncleReported = true;
            RomanceState.Savings = 800;
            QueueTestChapter(20);
        }

        private static void TestCooperation()
        {
            SetupCommon();
            RomanceState.Route = 1;
            RomanceState.MotherState = 1;
            RomanceState.Romance = 1;
            QueueTestChapter(21);
        }

        private static void TestConclusionRomance()
        {
            SetupCommon();
            RomanceState.Route = 1;
            RomanceState.MotherState = 1;
            RomanceState.Romance = 1;
            QueueTestChapter(24);
        }

        private static void TestConclusionFriends()
        {
            SetupCommon();
            RomanceState.Route = 1;
            RomanceState.MotherState = 1;
            RomanceState.Romance = 3;
            QueueTestChapter(24);
        }

        private static void TestRouteC()
        {
            SetupCommon();
            RomanceState.Route = 3;
            RomanceState.MotherState = 2;
            RomanceState.DriveState = 5;
            QueueTestChapter(101);
        }

        private static void TestRouteD()
        {
            SetupCommon();
            RomanceState.Route = 4;
            RomanceState.MotherState = 3;
            RomanceState.DriveState = 6;
            RomanceState.BetrayalKind = 1;
            RomanceState.Romance = 4;
            QueueTestChapter(201);
        }

        private static void TestEpilogue()
        {
            SetupCommon();
            RomanceState.Route = 1;
            RomanceState.MotherState = 1;
            RomanceState.Romance = 1;
            RomanceState.Chapter = RomanceState.ChapterComplete;
            RomanceState.EpilogueDone = true;
            RomanceState.EpilogueIndex = 0;
            RomanceState.LastEpilogueDay = StoreStation.Instance.dayCounter;
            QueueTestEpilogue();
        }

        private static void TestOldSavePrompt()
        {
            try
            {
                RomanceState.OldBlocked = true;
                RomanceState.OldPromptDone = false;
                StoreClient client = EleanorStory.CreateOldSavePromptClient();
                if (client != null)
                {
                    ModHelper.SetClientSource(client, ModConstants.ModId);
                    PlayerStore.Instance.storeClientManager.AddNextClient(client);
                    StoryDirector.Notify("已排队旧档档案员。");
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("TestOldSavePrompt failed: " + ex);
            }
        }

        private static void TestRouteResetPrompt()
        {
            try
            {
                RomanceState.RouteResetSuppressed = false;
                RomanceState.RouteResetNextOfferDay = -1;
                StoreClient client = EleanorStory.CreateRouteResetPromptClient();
                if (client != null)
                {
                    ModHelper.SetClientSource(client, ModConstants.ModId);
                    PlayerStore.Instance.storeClientManager.AddNextClient(client);
                    StoryDirector.Notify("已排队通关后重置档案员。");
                }
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("TestRouteResetPrompt failed: " + ex);
            }
        }

        private static void QueueNextChapter()
        {
            if (!RomanceState.StoryActive)
            {
                StoryDirector.Notify(L.Get("er_notice_story_inactive"));
                return;
            }

            if (VanillaEleanorBridge.RescuePending)
            {
                PlayerStore.Instance.storeClientManager.CheckStoryLineEleanor();
                return;
            }
            int maidEvent=MaidStory.EventForDay(MaidState.Today);
            if(maidEvent>0){QueueMaidEvent(maidEvent);return;}

            int chapter = RomanceState.Chapter == 0 ? 1 : RomanceState.Chapter;
            if (chapter >= RomanceState.ChapterComplete)
            {
                if (RomanceState.Route == 3 || RomanceState.Route == 4)
                {
                    StoryDirector.Notify("该路线没有后日谈。");
                    return;
                }
                QueueTestEpilogue();
                return;
            }

            QueueTestChapter(chapter);
        }

        private static void QueueTestEpilogue()
        {
            QueueTestClient(() => EleanorStory.CreateEpilogueClient(0));
        }

        private static void QueueTestChapter(int chapter)
        {
            QueueTestClient(() => EleanorStory.CreateClient(chapter), chapter);
        }

        private static void QueueTestClient(Func<StoreClient> factory, int chapter = 0)
        {
            try
            {
                if (PlayerStore.Instance == null || PlayerStore.Instance.storeClientManager == null)
                {
                    StoryDirector.Notify("当前不在可测试的经营场景。");
                    return;
                }

                if (PlayerStore.Instance.currentClientInstance != null)
                {
                    StoryDirector.Notify("请先结束当前客人，再跳转测试章节。");
                    return;
                }

                StoreClientManager manager = PlayerStore.Instance.storeClientManager;
                if (manager.clientStack != null)
                {
                    manager.clientStack.RemoveAll(VanillaEleanorBridge.IsOurClient);
                }

                StoreClient client = factory();
                if (client == null)
                {
                    StoryDirector.Notify("测试章节创建失败。");
                    return;
                }

                int day = StoreStation.Instance.dayCounter;
                if (chapter > 0)
                {
                    RomanceState.Chapter = chapter;
                    RomanceState.PendingChapter = chapter;
                    RomanceState.NextVisitDay = day;
                }
                RomanceState.LastQueuedDay = day;

                ModHelper.SetClientSource(client, ModConstants.ModId);
                manager.AddNextClient(client);
                StoryDirector.Notify(chapter > 0
                    ? "已排队第 " + chapter + " 章测试场景。"
                    : "已排队测试后日谈。");
            }
            catch (Exception ex)
            {
                StoryDirector.LogError("QueueTestClient failed: " + ex);
            }
        }

        private static void SetupCommon()
        {
            RomanceState.ResetForNewGame(false);
            RomanceState.Affection = 10;
            RomanceState.FairDeals = 3;
            RomanceState.Savings = 0;
            RomanceState.MotherState = 0;
            RomanceState.Route = 0;
            RomanceState.Romance = 0;
            RomanceState.DriveState = 0;
            RomanceState.ProtectionReady = false;
            RomanceState.ProtectionChannel = 0;
            RomanceState.ProtectionFailed = false;
            RomanceState.UncleAccepted = false;
            RomanceState.UncleInspected = false;
            RomanceState.UncleReported = false;
            RomanceState.UncleWarned = false;
            RomanceState.CrateKept = false;
            RomanceState.CrateSold = false;
            RomanceState.TrapSprung = false;
            RomanceState.Arrested = false;
            RomanceState.Rescued = false;
            RomanceState.ThreatRecorded = false;
            RomanceState.LydiaVisited = false;
            RomanceState.LydiaFairBracelet = false;
            RomanceState.DPreludeStep = 0;
            RomanceState.DPreludeState = 0;
            RomanceState.DPreludeOriginalPrice = 0;
            RomanceState.DPreludeBuybackMode = 0;
        }

        private static void AddAffection()
        {
            if (!RomanceState.StoryActive)
            {
                StoryDirector.Notify(L.Get("er_notice_story_inactive"));
                return;
            }

            RomanceState.AddAffection(5);
            StoryDirector.Notify("Affection: " + RomanceState.Affection);
        }

        private static void MarkRomanceComplete()
        {
            MetaProgress.UnlockTrueEnding();
            StoryDirector.Notify(L.Get("er_notice_true_unlock"));
        }

        private static void RegisterMaidTests()
        {
            Add("测试：侍从入口 / Attendant entry",()=>{SetupCommon();RomanceState.Savings=800;QueueTestChapter(12);},true);
            Add("测试：旧800序章 / Legacy continuation",()=>
            {
                SetupCommon();RomanceState.Chapter=13;RomanceState.PendingChapter=0;
                RomanceState.Romance=4;RomanceState.Savings=800;
                MaidState.FeatureVersion=MaidState.CurrentVersion;MaidState.LegacyResume=1;
                MaidState.LegacyDueDay=MaidState.Today;MaidState.Text("legacy_source","debug-fixture");
                RomanceState.Romance=0;QueueMaidEvent(MaidStory.LegacyVisit);
            },true);
            Add("测试：女仆首秀 / Maid debut",()=>{SetupMaidFixture(false);QueueTestChapter(19);},true);
            Add("测试：侍从告白 / Attendant confession",()=>{SetupMaidFixture(false);QueueTestChapter(21);},true);
            Add("测试：侍从基础结局 / Base conclusion",()=>{SetupMaidFixture(true);QueueTestChapter(24);},true);
            Add("测试：王尔德P1 / Wilde housing",()=>{SetupMaidFixture(false);MaidProjects.OnLoaded();ModHelper.OpenWildNetwork();},true);
            Add("测试：23章后未付P1 / Late housing",()=>
            {SetupMaidFixture(true);StoryDirector.CompleteCore();MaidState.BasicComplete=true;MaidState.BasicDay=MaidState.Today;MaidState.ProgressDay=MaidState.Today;MaidProjects.OnLoaded();ModHelper.OpenWildNetwork();},true);
            foreach(int value in new[]{24,25,26,27,28})
            {
                int chapter=value;
                Add("测试：归来"+chapter+" / Return "+chapter,()=>
                {
                    SetupMaidFixture(true);StoryDirector.CompleteCore();MaidState.BasicComplete=true;
                    MaidState.BasicDay=MaidState.Today-12;MaidState.Progress=chapter-1;MaidState.ProgressDay=MaidState.Today-3;
                    // Explicit debug-only paid fixtures: no actual deduction, not a playthrough.
                    for(int p=1;p<=4;p++)
                    {
                        bool delivered=p==1 || p==2&&chapter>24 || p==3&&chapter>25 || p==4&&chapter>27;
                        MaidState.Flag("p"+p+".paid",true);MaidState.Flag("p"+p+".shown",delivered);
                        MaidState.Text("p"+p+".plan","er_maid_p"+p+"_cash");MaidState.SetInt("p"+p+".day",MaidState.Today-5);
                    }
                    QueueMaidEvent(chapter);
                },true);
            }
            Add("测试：研究保留撤案新闻 / Review news",()=>
            {
                SetupMaidFixture(true);StoryDirector.CompleteCore();MaidWorldEvents.SubmitReceipt();
                MaidState.SetInt("world.due_day",MaidState.Today);MaidWorldEvents.TickNews(StoreStation.Instance?.storeEventManager);
                StoryDirector.Notify("Review state="+MaidWorldEvents.State+" (3=published)");
            },true);
            Add("测试：侍从状态 / Attendant state",()=>StoryDirector.Notify(
                "Maid="+MaidState.Contract+" Entry="+MaidState.EntryKind+" Paid="+MaidState.EntryPaidCash
                +" Love="+MaidState.Confession+" Base="+MaidState.BasicComplete+" Return="+MaidState.Progress
                +" Pending="+MaidState.PendingVisit+" P1="+MaidProjects.Paid(1)+"/"+MaidProjects.ResultShown(1)
                +" Review="+MaidWorldEvents.State+" Due="+MaidState.Int("world.due_day",-1)
                +" Tx="+MaidState.Int("tx.phase")),true);
        }
        private static void SetupMaidFixture(bool lover)
        {
            SetupCommon();MaidState.Enter(2);RomanceState.Route=2;RomanceState.MotherState=1;
            RomanceState.ProtectionReady=true;RomanceState.DriveState=4;MaidState.DebutShown=true;
            if(lover){MaidState.Confession=1;RomanceState.Romance=1;}
        }
        private static void QueueMaidEvent(int id)
        {
            if(PlayerStore.Instance?.currentClientInstance!=null)
            {StoryDirector.Notify("请先结束当前客人 / Finish the current visitor first.");return;}
            MaidState.PendingVisit=id;MaidState.NextVisitDay=MaidState.Today;
            QueueTestClient(()=>MaidStory.CreateEvent(id));
        }
    }
}
