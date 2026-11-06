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
            Add("测试：第 12 章 调查款（正常）", TestSavingsNormal);
            Add("测试：第 12 章 调查款（逾期补款）", TestSavingsLate);
            Add("测试：第 13 章 数据盘", TestDrive);
            Add("测试：第 14 章 威胁", TestThreat);
            Add("测试：第 15 章 方向选择", TestDecision);
            Add("测试：第 16 章 准备（路线乙）", TestPreparationB);
            Add("测试：第 17 章 团聚（路线甲）", TestReunionA);
            Add("测试：第 17 章 团聚（路线乙）", TestReunionB);
            Add("测试：第 20 章 表白", TestConfession);
            Add("测试：第 21 章 合作买家", TestCooperation);
            Add("测试：第 24 章 恋爱收束", TestConclusionRomance);
            Add("测试：第 24 章 朋友收束", TestConclusionFriends);
            Add("测试：路线丙第 1 段", TestRouteC);
            Add("测试：路线丁（擅自交盘）", TestRouteD);
            Add("测试：后日谈", TestEpilogue);
            Add("测试：旧档档案员", TestOldSavePrompt);
            Add("测试：好感 +5", AddAffection);
            Add("标记完整恋爱线（测试）", UnlockScenario);
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
            RomanceState.Arrested = true;
            RomanceState.Rescued = false;
            QueueTestChapter(5);
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

        private static void QueueNextChapter()
        {
            if (!RomanceState.StoryActive)
            {
                StoryDirector.Notify(L.Get("er_notice_story_inactive"));
                return;
            }

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
                    manager.clientStack.RemoveAll(c => c != null
                        && !string.IsNullOrEmpty(c.identifier)
                        && c.identifier.StartsWith("er_story_", StringComparison.Ordinal));
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

        private static void UnlockScenario()
        {
            MetaProgress.UnlockTrueEnding();
            ScenarioRegistration.RefreshAfterUnlock();
        }
    }
}
