# 1.42 发布记录（2026-11-28）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次基于 1.41 修复跨 MOD 睡前拦截导致的睡眠锁死及闭店/窗口对白边界问题，并升版本号至 1.42：

- 修复与其他 MOD 共用时，对方在 `EndOfDayUIManager.OnEndDayButtonClick` 的 `Prefix` 中返回 `false` 拦截睡觉（如其他 MOD 的第 7 天剧情背景提示、闭店后睡前剧情或房间床铺确认）后，本 MOD 提前在 `Prefix` 置位的 `sleepStarting = true` 永不复原、导致后续彻底无法睡觉与推进天数的问题：改为在 `[HarmonyPriority(Priority.First)] Postfix(bool __runOriginal)` 仅当原版睡觉实际执行（`__runOriginal == true`）时置位，并在 `ChapterMorningPatch.CheckMorning` 补充晨间兜底复位。
- 修复提前闭店触发闭店前 CG 窗口对白（侍从线《第一场演出》、恋爱线《公主抱约会》）时，`StoreUIManager.closeShutterConfirmationPanel` 确认弹窗未被关闭并遮挡对白的问题。
- 修复关门后 4 秒内立刻睡觉触发睡前窗口对白时，原版 `StoreShutterButton.EndAndClose` 的延迟离店定时器打断对白的问题：`ChapterVisits.WindowTalk` 在 `CLOSED` 且仍有闭店离店客人时先完成离店清理；`RestoreWindowShutter()` 仅在 `storeState != OPEN` 时恢复卷帘门遮罩。
- 新增 `ChapterWindowDialogueEndPatch`（Patch `DialogUIManager.EndConversation` Prefix），当外部逻辑在窗口对白未到末句时强行结束对话时立即释放对白与独白锁。
- 离线回归 `tools/test_cg_night_flows.ps1` / `tools/test_cg_night_boundaries.ps1` 补齐 `ChapterBedtimePatch.Postfix(__runOriginal)` 与 `EndConversation` 挂钩，新增第 7 天跨 MOD 拦截睡觉、埃莉诺睡前 CG 后串联跨 MOD 拦截睡觉、非末句 `EndConversation` 恢复与晨间复位测试。
- 保留 1.41/1.40/1.39/1.38 全部内容、本地化与兼容修复；不改剧情文本、排期、选项结果或结局。

不包含：Nexus 发布包、本地 `Mods/` 部署、路线图改动、GitHub 推送；未修改 Workshop 简介或更新说明。

## 验证

- Release 编译（`dotnet build EleanorRomance.csproj -c Release`）：0 警告、0 错误。
- Harmony 目标校验（对比 `Assembly-CSharp.dll`）：58 个目标全部匹配、0 失败。
- `tools/test_cg_night_flows.ps1`：全部 PASS（含新增第 7 天跨 MOD 拦截睡觉、睡前 CG 串联跨 MOD 拦截、`EndConversation` 恢复与晨间复位回归）。
- `tools/test_cg_chapter_flows.ps1`：PASS。
- `tools/test_runtime_flows.ps1 -ReleaseQA`：全部 PASS（四路线排期、选择路径与回放全通过）。
- `python tools/check_project.py`：通过（zh/en 各 3,157 键零缺失，`manifest.xml` 字符集校验通过）。
- `tools/scan_package.ps1`（对 `dist/workshop-1.42/EleanorRomance`）：通过（扫描 1,742 个方法，`refused=false`、`findings=0`、`capabilities=[]`、`hardFlags=[]`）。
- Workshop 包：164 文件，32,909,868 bytes；DLL SHA-256 `E7CFD4BC099A66D66A5BEBDDCD58995417129F0FCCF96864B87DA7CCCD6E5B2C`；ZIP SHA-256 `F5CF738771C94B05E4A3F9490369325128947E82A810FC27FBD5C1F3923D8BBA`。

## 发布状态

- 2026-11-27T18:46:38Z（北京时间 2026-11-28 02:46:38）以 content-only 模式更新原条目，`-KeepDescription`，未传 `-NotesFile`；Steam 返回 `k_EResultOK`，耗时 10.54 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；`file_size` 32,909,868 bytes；条目公开（`visibility=0`）、未封禁（`banned=0`）；`time_updated` 1795805197（2026-11-27T18:46:37Z），与本地包字节数完全一致。
- 证据：`dist/workshop-1.42/`（`release-files.json`、`publish-result.json`）、`dist/EleanorRomance-1.42.zip`。
- Nexus 未发布；GitHub 未推送。
