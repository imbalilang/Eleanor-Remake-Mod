# 1.44 发布记录（2026-11-28）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次基于 1.43 为 E1/E3/E4/E9 的 CG 章节补齐睡前/次晨窗口串场，并收尾 CG 章节与终章的窗口对白衔接，升版本号至 1.44：

- E1《回家》、E3《侍从柜台》、E4《两位老板娘》、E9《第二下》的夜访与晨图章节加入窗口旁白起手，按实际睡前、关门后或次日开门前的时机进入，不再沿用“刚收摊 / 还没锁窗”的旧状态开场；`Content/CG章节/` 源稿与 `src/ChapterScripts.g.cs` 同步。
- `src/ChapterVisits.cs`、`src/ChapterVisitHooks.cs` 与 `src/ChapterFinales.cs` 调整 CG 章节及终章的窗口对白收尾、旁白台词和店主内心句；对白、独白与卷帘门状态在外部对话抢占或中途结束时继续清理。
- 旁白台词统一使用 1.43 的独立旁白框（`src/NarrationBox.cs`），保持与角色气泡分离。
- 本地化新增 1 个键，简中 / 英文 / 日文 / 韩文均为 3,158 键并保持键集一致；同步 `tools/ui_text_zh.json`、`tools/ui_text_en.json` 与 `tools/ja_translations.json`。重新生成 `docs/CURRENT_TEXT_READING.md`，`docs/NEXUS_README.txt` 版本头更新至 1.44。
- 更新 `tools/build_cg_chapters.py`、`tools/render_current_reading.py`、`tools/test_arrest_speakers.ps1`、`tools/test_cg_night_boundaries.ps1`、`tools/test_cg_night_flows.ps1` 与相关立绘/表情贴图。
- 不改路线、选项结果、目标日、排期或结局；保留 1.43/1.42/1.41/1.40/1.39/1.38 全部内容与修复。

不包含：Nexus 发布包、本地 `Mods/` 部署、路线图改动；未修改 Workshop 简介或更新说明。

## 验证

- Release 编译（`dotnet build EleanorRomance.csproj -c Release`）：0 警告、0 错误。
- `tools/test_cg_night_flows.ps1`：全部 PASS（含 `test_cg_night_boundaries.ps1` 的闭店/睡眠/自动存档/终章边界、13 场线性夜晚、`dual_night_bath` 原生推进、跨 MOD 睡眠拦截与恢复用例）。
- `tools/test_cg_chapter_flows.ps1 -Managed 'B:\Probably Stolen_Data\Managed'`：PASS（全屏 CG 章节与 E4/E9 终章排期、Sophia 选择与无成交护送）。
- `python tools/check_project.py`：通过（zh/en 各 3,158 键零缺失，四语言键集一致，`manifest.xml` 字符集校验通过）。
- `tools/scan_package.ps1`（PowerShell 7，对 `dist/workshop-1.44/EleanorRomance` 与游戏 Managed）：通过（扫描 1,787 个方法，`refused=false`、`findings=0`、`capabilities=[]`、`hardFlags=[]`、`pending=0`）。
- Workshop 包：164 文件，32,907,863 bytes；DLL SHA-256 `F3878C1177547D243704871274A8CD00F1782EEC129ECB6C9F285E2D06E7672C`；ZIP SHA-256 `85054B1BCA220A7F4ACAC96A3F2B4A776ACE5C8ADE7EB2B36B82940886236508`。

## 发布状态

- 2026-11-28T06:50:30Z（北京时间 2026-11-28 14:50:30）以 content-only 模式更新原条目，`-KeepDescription`，未传 `-NotesFile`；Steam 返回 `k_EResultOK`，耗时 7.53 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；`file_size` 32,907,863 bytes，与本地包一致；`time_updated` 1795848630（2026-11-28T06:50:30Z）；简介 SHA-256 仍为 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`，标题与简介未被修改。
- 证据：`dist/workshop-1.44/`（`release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`）、`dist/EleanorRomance-1.44.zip`。
- Nexus 未发布；GitHub 本次同步 1.41–1.44 的仓库内容。
