# 1.43 发布记录（2026-11-28）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次基于 1.42 为本 MOD 旁白新增独立旁白横幅框，并升版本号至 1.43：

- 新增独立旁白横幅模块 `src/NarrationBox.cs`：从原生 `DialogUIManager.clientBubble` 克隆独立节点 `er_narration_box`（不修改原版 `clientBubble`），隐藏角色名牌（`clientNameBubble`）与多余气泡尾巴贴图，按原画布像素单位对称收整内边距，并生成像素风无尾巴 9-slice 深石板底框配冷银灰居中文字，将旁白与 NPC 对话气泡彻底区分。
- 定位锚定在营业窗口正上方（优先投影 `StoreShutterButton.blockerMaskObject` 顶边中点，退化回 `clientGameObjectParent` 或画布顶部居中，并限制在顶部安全带与左右边界内；放大镜与举枪特写模式居中置顶），不遮挡角色立绘、柜台物品与选项气泡。
- 通过 12 个 Harmony 补丁完整接管原生打字机速度同步（含中日韩 `0.35x` 速率、关闭打字机瞬显、`resetTypingSpeedAtStartup = false`）、点击气泡与空格推进/跳过、对话缩放（`EmporiumOption.DialogBubbleScale`）、常规/特写模式切换，以及 `HideBubbles` / `HideAllDialogUI` / `InterruptDialog` / `PlayClientSingleText` / `ResumeIfWaitingForAdvance` 清理。
- 仅拦截本 MOD 标记的旁白台词（`DialogueSpeakers.HasOwnLabel(line)` 且 `title == 旁白`），原版、NPC、玩家、保安与其他 MOD 气泡不受影响；历史记录（Recorder）继续写入 `旁白: ...`；独立克隆节点天然不触发声音类 MOD（如 `PawnshopMurmurs`）的 NPC 发声。
- 更新 `EleanorRomance.csproj`（新增 `Febucci.TextAnimator.Runtime.dll` 编译引用）、`src/DialogueSpeakers.cs`、`src/ModEntry.cs`、`src/StoryDirector.cs`、`src/ChapterVisits.cs` 与 `tools/test_arrest_speakers.ps1`。
- 保留 1.42/1.41/1.40/1.39/1.38 全部内容、本地化与兼容修复；不改剧情文本、排期、选项结果或结局。

不包含：Nexus 发布包、本地 `Mods/` 部署、路线图改动、GitHub 推送；未修改 Workshop 简介或更新说明。

## 验证

- Release 编译（`dotnet build EleanorRomance.csproj -c Release`）：0 警告、0 错误。
- Harmony 目标校验（对比 `Assembly-CSharp.dll`）：70 个目标（含新增 12 个 `NarrationBox*Patch` 及私有字段注入）全部匹配、0 失败。
- `tools/test_cg_night_flows.ps1`：全部 PASS。
- `tools/test_cg_chapter_flows.ps1`：PASS。
- `tools/test_runtime_flows.ps1 -ReleaseQA`：全部 PASS（含 `tools/test_arrest_speakers.ps1` 新增 `NarrationBox.IsNarration` 边界与 `Enabled` 开关回归，四路线排期、选择路径与回放全通过）。
- `python tools/check_project.py`：通过（zh/en 各 3,157 键零缺失，`manifest.xml` 字符集校验通过）。
- `tools/scan_package.ps1`（对 `dist/workshop-1.43/EleanorRomance`）：通过（扫描 1,780 个方法，`refused=false`、`findings=0`、`capabilities=[]`、`hardFlags=[]`）。
- Workshop 包：164 文件，32,913,688 bytes；DLL SHA-256 `5FB01F527066933FA463D017C03378E149FBB4FCCAB6E773CBCD2BEDF8517DBF`；ZIP SHA-256 `4890F134E0E171DABFEDF869AC4B5F72F3DF05E2035BBA4159A3C41DB8F1ECE9`。

## 发布状态

- 2026-11-28T05:08:37Z（北京时间 2026-11-28 13:08:37）以 content-only 模式更新原条目，`-KeepDescription`，未传 `-NotesFile`；Steam 返回 `k_EResultOK`，耗时 11.56 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；`file_size` 32,913,688 bytes；条目公开（`visibility=0`）、未封禁（`banned=0`）；`time_updated` 1795842517（2026-11-28T05:08:37Z），与本地包字节数完全一致。
- 证据：`dist/workshop-1.43/`（`release-files.json`、`publish-result.json`）、`dist/EleanorRomance-1.43.zip`。
- Nexus 未发布；GitHub 未推送。
