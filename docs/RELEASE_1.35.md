# 1.35 发布记录（2026-11-24）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 修复 `PredatesStoryRewrite` 存档只能永久暂停、没有游戏内退出路径的问题（1.33 引入的回归）。
  - `src/StoryDirector.cs` 在读档时增加安全迁移：`Initialized && !OldBlocked && MaidState.FeatureVersion == 0 && !MaidState.HasHistory && Chapter == 0 && PendingChapter == 0 && RevVersion != 1 && !DualState.HasRetiredProgress` 时，补写 `MaidState.FeatureVersion = MaidState.V2Version`，继续当前剧情。只处理没有剧情进度的新档，不覆盖真正的旧路线存档。
  - `OnGenerateCustomerVeryEarly` 的 `PredatesStoryRewrite` 分支不再直接返回，而是排队现有档案员旧档询问；玩家可选择重置埃莉诺剧情（`ManualResetOldSave`，保留物品和世界效果）或保留原进度。
  - `PredatesStoryRewrite` 暂停日志增加 `maid / rev / retiredDual` 字段，便于定位。
- 不改剧情、路线、选项结果、目标日、结局或排期；保留 1.34 第 5 章警告场景叫号修复与 1.33 的 CG/结局页语言字体、立绘管理器兼容和旧内容清理。
- 本热修基于已发布 1.34；未包含其他并行开发中的剧情/CG 章节改动。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，`route map version: 1.35 (manifest 1.35)`。
- 原生扫描：1,573 个方法，0 capabilities / 0 findings / 0 pending / 0 hard flags。证据：`dist/workshop-1.35/native-scan.log`。
- 发布包：116 个文件，18,985,534 bytes；ZIP SHA-256 `D3E26A1C97E46CF114726E487D7EC2546BD26524E045951DC7DA5A7CDF6B1667`；DLL SHA-256 `E593C694FD422E75E276C482135810FF5528F48324957B64CADB1DBBAD6CF9A8`；包内 `manifest.xml` ModVersion = 1.35。
- 本包从 `f7dd670`（已发布 1.34）的干净 worktree 打出，只包含本次 `StoryDirector.cs` 热修和版本文件；没有另一条并行剧情/CG 线的改动。

## 发布状态

- 2026-11-24 08:48:45（UTC）以 **content-only** 模式更新原条目，`-KeepDescription`，Steam 返回 `k_EResultOK`，耗时 5.15 秒。
- Steam Web API 复核：标题与简介未改动；公开（Public），所有者 `76561198109201343`，文件大小 18,985,534 bytes，与本地包一致。
- 证据：`dist/workshop-1.35/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-before.txt`、`remote-after.txt`。
- 未执行本地 `Mods/` 部署；未提交 git、未推送 GitHub。
