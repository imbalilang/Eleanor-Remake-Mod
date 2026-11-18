# 1.16 发布记录（2026-11-18）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 背景与纠正

- 线上 1.15 来自 `origin/codex/1.12-stabilization`（`5625056`）：包含 1.14 游戏版本策略（`VersionGate` 不再按游戏版本停用剧情、`<GameVersions />` 清空）、1.15 双人演出名字与后宫约会日修复；本地 `main` 停在 1.13，未合入这些改动。
- 2026-11-18 早前的 1.14 content-only 上传由本地 `main` 打包，误将线上 1.15 内容降级为 1.13+立绘修复。1.16 以 1.15 提交为基线，叠加 maid v2 晚会立绘修复，恢复 1.14/1.15 内容并前进版本号。
- 1.16 是纠正版本；未改正文、路线、选项、排期或结局。

## 变更

- 修复 maid v2 路线第 21/22/25/26/27 场（晚会/驻场剧情）的埃莉诺晚会立绘回退：这些场景请求 `er_portrait_eleanor_gala_female_beauty`，但 `PortraitCompat.EnsureKey` 只允许独立晚会场景 `er_story_gala` 注册该键，导致 SpriteDict 找不到键并由原版回退成随机拾荒者 NPC 立绘。
- `EnsureKey` 现在对任何请求的晚会立绘键都进行桥接；若晚会图缺失，则回退到埃莉诺默认立绘，不再触发随机 NPC 回退。
- `SanitizeClient` 在读档与次日刷新时保留处于 maid v2 gala 场景（`er_story_v2_*`）的晚会立绘，不再把这些客户改成默认常服；独立晚会场景保持原有逻辑，其他遗留晚会键仍会清理。
- 保留 1.15 双人演出名字与后宫约会日修复、1.14 游戏版本策略、1.13 母亲交易与保释修复、1.12 手镯修复、1.11 立绘选择等全部改动。

## 验证

- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。
- `python tools/check_project.py`：通过，中英无缺键、无无效块引用。
- `python tools/check_route_map.py`：通过，1.16 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 实机回归建议：载入 maid v2 第 21/26 场存档（最好同时启用一个美化立绘 MOD），确认非 `expr` 行显示晚会立绘、`expr` 行显示晚会表情、存读档后不回退；确认独立晚会剧情、双人场景名字、后宫约会日、游戏 1.0.0.15 和其他路线不受影响。日志中不应再出现 `Sprite Value not found for key in Dict: er_portrait_eleanor_gala_female_beauty`。

## 发布状态

- 发布包：139 个文件，13,260,027 bytes；ZIP `dist/EleanorRomance-1.16.zip`，SHA-256 `0CB9F5B231C558F549AD1BA508DB21D81A04A8E93C82FED1A4E6C8A89E831D1F`；`EleanorRomance.dll` SHA-256 `CF731E1634DF115740D4DD9AE8D6B0E42627FF60F965B62F722AB7C5C12AC33B`。
- 2026-11-18 10:04:46 UTC 以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.05 秒；标题、简介和内容描述符未改动。
- 远程结果：条目 3834588749，语言 english，139 个本地文件，13,260,027 bytes。证据：`dist/workshop-1.16/publish-result.json`。
- 远程查询复核（Standalone Steamworks Uploader `--verify`）：标题/简介未改动（简介长度 256），Public，所有者 76561198109201343，文件大小 13,260,027 bytes（与本地包一致），预览 859,455 bytes。证据：`dist/workshop-1.16/remote-after.txt`。
- 包内 manifest 为 1.16，`<GameVersions />` 为空，`VersionGate.IsSupported` 恒为 true；已恢复 1.15 的双人演出名字、后宫约会日修复和 1.14 游戏版本策略，并叠加 maid v2 晚会立绘修复。
- 原生能力扫描：尝试用 `tools/scan_package.ps1` 对发布包运行游戏自带 `ModScanner.Scan`；本机 Windows PowerShell 5.1 出现环境错误 `MissingMethodException: System.String[] System.String.Split(Char, System.StringSplitOptions)`，报告 `refused=true`、`error` 为该环境错误，但 `capabilities`/`hardFlags`/`findings`/`pending` 均为空。这是扫描器运行环境问题，不是能力发现；日志见 `dist/workshop-1.16/native-scan.log`。
- 未执行本地 `Mods/` 部署；未推送 GitHub。1.16 取代 2026-11-18 早前的误发 1.14 内容更新。
