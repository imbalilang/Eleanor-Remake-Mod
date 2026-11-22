# 1.30 发布记录（2026-11-22）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 补齐后宫 v2 终局香水：`dv13` 结局完成时直接发放自我补充香水；已完成旧存档会在加载或开店时自动补发。
- 韩语本地化以 1.29 的 `ko.csv` 为基线选择性合并：应用 200 行斜体动作后空格/排版修正，以及埃莉诺、莉迪亚的半语语气与标点修正。
- 未整表替换；排除校对表中的旧版本内容、错位行、错误译文和不匹配的斜体标签，保留当前新增 key。
- 保留 1.29 的莉迪亚闭店约会修复、原版警告礼 barter 流程、终局香水发放、文案修订、1.28 治安部渠道与侍从线本地化、1.27 Serpico 顺延及更早修复。
- 不改路线、选项结果、目标日或结局。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.30。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.30/native-scan.log`。
- 发布包：141 个文件，20,388,748 bytes；ZIP `dist/EleanorRomance-1.30.zip`，SHA-256 `E1F484C3700F6D46FE6A9E5C8BAE0BE4DD30349A048779AC0AA9EEC24012124D`；DLL SHA-256 `85FA1C2E38463A0D916AFBC9768E35EA9C1AD05010F42CCA7C74430684609D76`。
- 韩语文件以本地 `dist/ko-before-1.30-correction-20261122.csv` 为合并前备份。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-22 11:26:21（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.53 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,388,748 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-22T11:26:21Z`，内容句柄 `2391637432823669017`。
- 证据：`dist/workshop-1.30/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.30.txt`。
- GitHub 同步：源码、Content、Localization、文档与脚本已提交并推送 `origin/main`（发布 commit `cf31614`）；大型本地 Art 工作目录、dsh-image-gen 与 reference 缓存未纳入仓库。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
