# 1.41 发布记录（2026-11-27）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次基于 1.40（`d1f5536`）修复无客人 CG 窗口对白卡死、卷帘门遮挡立绘与外出立绘泄漏问题，并升版本号至 1.41：

- 修复夜间睡前、闭店前与晨间开门前无客人 CG 窗口对白（如后宫线《莉迪亚邀请》`dual_night_bath`）在第一句后无法推进、导致睡眠/开关门死锁的问题：`ChapterVisits.WindowTalk` 期间正确管理 `DialogUIManager.isSecMonologuePlaying` 并在 `ResetWindow()` 归还，使原生 `DialogUIManager.ContinueConversation()` 不再因 `currentClientInstance == null` 中断对话。
- 修复关门/晨间状态下窗口对白立绘被卷帘门遮罩挡住的问题：窗口对白显示立绘时临时隐藏关闭状态的卷帘门遮罩（`StoreShutterButton.blockerGameObject` 父容器），正常展示角色立绘与 1.40 表情变化，并在对白结束或重置时自动恢复。
- 窗口对白与全屏 CG 页展示期间禁止点击侧门出门、重开睡袋结算或打开夜游地图；挂接 `RemoveCurrentDialog`、`StartDialogue` 与 `EmporiumEntry.OnLeaveStore` 兜底清理，杜绝对白锁死或立绘泄漏到博士等外出场景。
- 离线测试 `tools/test_cg_night_flows.ps1` / `tools/test_cg_night_boundaries.ps1` 改为真实执行原生 `DialogUIManager.ContinueConversation()`，新增 `dual_night_bath` 完整推进、控件锁定、异常中止与离店清理回归用例。
- 保留 1.40/1.39/1.38 全部内容、本地化与兼容修复；不改剧情文本、排期、选项结果或结局。

不包含：Nexus 发布包、本地 `Mods/` 部署、路线图改动、GitHub 推送；未修改 Workshop 简介或更新说明。

## 验证

- Release 编译（`dotnet build EleanorRomance.csproj -c Release`）：0 警告、0 错误。
- `tools/test_cg_night_flows.ps1`：全部 PASS（覆盖全部 13 场夜间 CG、E4/E9 终章、打烊窗口对白、`dual_night_bath` 原生 `ContinueConversation()` 推进及锁定/恢复回归）。
- `tools/test_cg_chapter_flows.ps1`：PASS。
- `tools/test_runtime_flows.ps1 -ReleaseQA`：全部 PASS（117 条选择路径、468 次回放、四路线排期全通过）。
- `python tools/check_project.py`：通过（zh/en 各 3,157 键零缺失）。
- `tools/scan_package.ps1`（对 `dist/workshop-1.41/EleanorRomance`）：通过（扫描 1,739 个方法，`refused=false`、`findings=0`、`capabilities=[]`、`hardFlags=[]`）。
- Workshop 包：164 文件，32,912,098 bytes；DLL SHA-256 `0D72C733B959EFFDB7FA54006EAE33AF162794C62C11BC98AA90AB2A00C311AB`；ZIP SHA-256 `10871B1B598E5608066B7F20DDB824439A603C142C70C5C1CFF201A797EE207C`。

## 发布状态

- 2026-11-27T12:36:59Z 以 content-only 模式更新原条目，`-KeepDescription`，未传 `-NotesFile`；Steam 返回 `k_EResultOK`，耗时 6.03 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；`file_size` 32,912,098 bytes；条目公开（`visibility=0`）、未封禁（`banned=0`）；`time_updated` 1795783019（2026-11-27T12:36:59Z），与本地包字节数完全一致。
- 证据：`dist/workshop-1.41/`（`release-files.json`、`publish-result.json`）、`dist/EleanorRomance-1.41.zip`。
- Nexus 未发布；GitHub 未推送。
