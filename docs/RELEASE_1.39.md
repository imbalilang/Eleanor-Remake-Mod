# 1.39 发布记录（2026-11-27）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次从本地 `main`（`28d5746`）加当前工作区的 1.39 内容打包，包含：

- 恋爱、后宫、侍从、革命军四条路线当前启用场景的文本重写与旧版清理。
- 恋爱、侍从、革命军各 4 场结局后日常窗口事件；后宫保留既有 `dr_daily_2/3`。
- 事件/结局 CG、Rivals 服装与日常立绘更新；埃莉诺耳饰在 CG/立绘中统一。
- 简中/英文重新生成；日文/韩文与简中键集对齐（3,075 键）。
- 保留 1.38 立绘兼容、双角色首句名字修复及更早修复。

不包含：工具/文档改动、未跟踪参考素材；未推送 GitHub；未执行本地 `Mods/` 部署；未修改 Workshop 简介或更新说明。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过（zh/en 各 3,075 键；post-ending daily 12 场）。
- `python tools/build_cg_chapters.py --check-sheet`：36/36。
- ja/ko 键集与 zh 一致：3,075 / 3,075 / 3,075。
- Workshop 包：164 文件，32,832,419 bytes；ZIP SHA-256 `01A0BA5FF424D1918A9EC4389AD8CC317FD47119F8AF0FAD1DCB5F3C8FF953AD`。
- Nexus 包：165 文件，34,308,869 bytes；DLL SHA-256 `95A0741D2A3BDAD87888CCEE6CAF503F1642AF9F9404A3FDE5895A4232C849A4`；ZIP SHA-256 `82E0863AF96AC71035E3756B601885C75D2A49BB4931FF68FE794790052F1C4E`。

## 发布状态

- 2026-11-27T09:11:41Z 以 content-only 模式更新原条目，`-KeepDescription`，未传 NotesFile；Steam 返回 `k_EResultOK`，耗时 93.13 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；file_size 32,832,419 bytes；条目公开、未封禁；time_updated 1795770700（2026-11-27T09:11:40Z）。
- 本地 Steam Workshop 缓存仍显示 1.38，属于缓存延迟；Web API 文件大小与 1.39 包一致。
- 证据：`dist/workshop-1.39/`（`release-files.json`、`publish-result.json`）、`dist/EleanorRomance-1.39.zip`、`dist/nexus-1.39/package-result.json`、`dist/EleanorRomance-1.39-nexus.zip`。
- Nexus 包已生成，未上传；GitHub 未推送。
