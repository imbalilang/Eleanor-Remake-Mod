# 1.36 发布记录（2026-11-24）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 修复被误判为 `PredatesStoryRewrite` 的 v2 侍从存档：
  - `src/StoryDirector.cs` 检测只由 v2 侍从线写入的标记（`v2.act2_done`、`v2.act3_done`、`v2.residency`、`v2.coWork`、gap 选择/付款）。存在这些标记时，把 `MaidState.FeatureVersion` 修复为 2 并继续当前剧情。
  - P1–P4、王尔德已购项目、王尔德列表和已有进度均保留，不调用重置。
  - 之前隐藏的已购王尔德卡片会随着剧情恢复重新出现。
- 收紧 `PredatesStoryRewrite` 的旧路线判断（`src/RomanceState.cs`）：
  - 旧革命军 `RevVersion == 1` 只在当前 `Route == 3` 时暂停。
  - 旧后宫 `DualState.HasRetiredProgress` 只在没有当前侍从历史时暂停。
  - 真正的旧 v1 侍从存档仍因 `MaidState.FeatureVersion < 2` 暂停，并保留 1.35 的档案员重置/保留兜底。
- 暂停日志增加 `v2`、`p4` 诊断字段。
- 不改剧情、路线、选项结果、目标日、排期或结局；保留 1.35、1.34、1.33 修复。本热修基于已发布 1.34/1.35 的干净 worktree，不包含并行开发中的剧情/CG 章节改动。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，`route map version: 1.36 (manifest 1.36)`。
- 原生扫描：1,573 个方法，0 capabilities / 0 findings / 0 pending / 0 hard flags。证据：`dist/workshop-1.36/native-scan.log`。
- 发布包：116 个文件，18,987,085 bytes；ZIP SHA-256 `B162E9E8A2B7FDB8A9971D0B9D3EA0D830DA8646EE4A6F5C9F7B0BFF16FF70DF`；DLL SHA-256 `85D46E582DA4EC2DA06EA7571472CC31958567C8C315799C5A11DFDF1D5CE922`；包内 `manifest.xml` ModVersion = 1.36。
- 本包从干净 worktree 打出，只包含 1.34/1.35 基线与本次存档兼容修复；并行开发中的剧情/CG 改动未进入。

## 发布状态

- 2026-11-24 09:01:10（UTC）以 **content-only** 模式更新原条目，`-KeepDescription`，Steam 返回 `k_EResultOK`，耗时 5.16 秒。
- Steam Web API 复核：标题与简介未改动；公开（Public），所有者 `76561198109201343`，文件大小 18,987,085 bytes，与本地包一致。
- 证据：`dist/workshop-1.36/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.txt`。
- 未执行本地 `Mods/` 部署；未提交 git、未推送 GitHub。
