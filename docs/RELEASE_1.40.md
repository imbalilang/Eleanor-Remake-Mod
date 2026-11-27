# 1.40 发布记录（2026-11-27）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次以 1.40 打包当前工作区，包含 1.39 已发布但尚未提交的四路线文本、本地化、结局后日常与 CG/立绘内容，以及 1.40 的 CG 窗口表情、交易排期和侍从新来访：

- CG 章节与交易窗口对白接入角色表情与场景立绘；新增 `Content/cg_expressions_v1.json` 映射。
- 修复可选交易 CG 与主线/日常来访同日排队冲突；调整交易窗口，并补上后宫怀表交易后的店内收尾。
- 侍从线新增第三幕后交易来访《楼上的风声》（`v2_salon_buzz`，进度 24），并调整结局后来访排期。
- 读档时清理重写文本的未完成对话游标，避免中段存档续读落到截断句。
- 简中/英文重新生成；日文/韩文与简中键集对齐（3,157 键）。
- 保留 1.39 四路线文本重写、结局后日常、CG/立绘更新及 1.38 立绘兼容修复。

不包含：Nexus 发布包、未跟踪参考素材；未执行本地 `Mods/` 部署；未修改 Workshop 简介或更新说明。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过（zh/en 各 3,157 键；post-ending daily 12 场）。
- `python tools/build_cg_chapters.py --check-sheet`：36/36。
- `python tools/check_route_map.py`：路线图 1.40 与 manifest 一致，5 结局 / 20 幕 / 场景覆盖通过。
- ja/ko 键集与 zh 一致：3,157 / 3,157 / 3,157。
- Workshop 包：164 文件，32,895,081 bytes；ZIP SHA-256 `C6CFFCE6285A33DBDE4C847C3F7785603B30F5ED7EE772A30F7E4FB1E14A571D`。

## 发布状态

- 2026-11-27T10:58:14Z 以 content-only 模式更新原条目，`-KeepDescription`，未传 NotesFile；Steam 返回 `k_EResultOK`，耗时 5.54 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；file_size 32,895,081 bytes；条目公开、未封禁；time_updated 1795777093（2026-11-27T10:58:13Z）。
- 本地 Steam Workshop 缓存仍显示 1.39，属于缓存延迟；Web API 文件大小与 1.40 包一致。
- 证据：`dist/workshop-1.40/`（`release-files.json`、`publish-result.json`）、`dist/EleanorRomance-1.40.zip`。
- GitHub `main` 随本次 1.40 提交同步；Nexus 未发布。
