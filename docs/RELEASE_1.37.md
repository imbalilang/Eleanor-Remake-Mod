# 1.37 发布记录（2026-11-24）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 不再把旧路线存档永久暂停：
  - `src/StoryDirector.cs` 在读档时把旧侍从 `MaidState.FeatureVersion`、革命军 `RevVersion`、后宫 `DualState.FeatureVersion` 补为当前版本。
  - 章节、物品、王尔德 P1–P4、已购项目和现有进度保留，不调用重置；已购但被隐藏的王尔德卡片重新可见。
  - 共同线/恋爱线不再因为缺少侍从版本字段被暂停。
- `src/RomanceState.cs` 的 `PredatesStoryRewrite` 只在旧后宫版本仍未迁移时成立；迁移后存档继续走当前章节排期。
- 这是尽力兼容：旧路线中已被删除的场景不保证重播，但当前章节、主线排期和已购内容继续。
- 暂停/迁移日志记录补上的版本、`v2` 和 `p4` 标记；不改剧情、路线、选项结果、目标日、排期或结局。
- 保留 1.36、1.35、1.34、1.33 修复；基于干净 1.34 基线，不包含并行开发中的剧情/CG 章节改动。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，`route map version: 1.37 (manifest 1.37)`。
- 原生扫描：1,573 个方法，0 capabilities / 0 findings / 0 pending / 0 hard flags。证据：`dist/workshop-1.37/native-scan.log`。
- 发布包：116 个文件，18,987,702 bytes；ZIP SHA-256 `3236F5FB5942CEC0B1A8380D62EDD898BAC03CBEACA564E248F2007882AF7681`；DLL SHA-256 `F6FBB4CDC83364B4C0DBB9A24F433E671BF210E6F446C376C0673760B80C206A`；包内 `manifest.xml` ModVersion = 1.37。
- 本包从干净 worktree 打出，只包含 1.34/1.35/1.36 基线与本次旧路线放行；并行开发中的剧情/CG 改动未进入。

## 发布状态

- 第一次尝试在 2026-11-24 09:07:33（UTC）返回 `k_EResultFail`，远端仍为 1.36；未改动条目内容。
- 重试于 2026-11-24 09:07:52（UTC）以 **content-only** 模式更新，`-KeepDescription`，Steam 返回 `k_EResultOK`，耗时 4.54 秒。
- Steam Web API 复核：标题与简介未改动；公开（Public），所有者 `76561198109201343`，文件大小 18,987,702 bytes，与本地包一致。
- 证据：`dist/workshop-1.37/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.txt`。
- 未执行本地 `Mods/` 部署；未提交 git、未推送 GitHub。
