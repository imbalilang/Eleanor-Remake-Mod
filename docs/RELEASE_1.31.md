# 1.31 发布记录（2026-11-22）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 在 1.30 基础上完成韩语校对合并：共更新 279 个 key，包括斜体动作后的空格、埃莉诺/莉迪亚语气与标点修正，以及统一的“영양 열매”术语。
- 保留当前全部 key；校对表缺失的 34 个当前 key 继续使用现有译文，多出的 32 个废弃 key 不导入。
- 继续排除会回退 1.24–1.30 新内容的 97 条旧译、30 条斜体结构冲突，以及玩家敬语、革命军结局 POV 等 8 条不适用条目。
- 保留 1.30 后宫 v2 香水覆盖、1.29 闭店约会/奖励/警告礼修复、1.28 治安部渠道与侍从线本地化、1.27 Serpico 顺延及更早修复。
- 不改路线、选项结果、目标日或结局。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.31。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.31/native-scan.log`。
- 发布包：141 个文件，20,389,152 bytes；ZIP `dist/EleanorRomance-1.31.zip`，SHA-256 `74E051C6484012FE52CE8FACC375918A6EFFECCE006AC9F1FAB02710A7C18BC2`；DLL SHA-256 `CA9362AD954FAC215F26BCEA678CFFA5124AADA095B0B8597B7FE970909F97F8`。
- 韩语合并前备份：`dist/ko-before-full-correction-20261122.csv`。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-22 11:37:54（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.03 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,389,152 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-22T11:37:54Z`，内容句柄 `2333507226971900105`。
- 证据：`dist/workshop-1.31/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.31.txt`。
- GitHub 同步：源码、Content、Localization、文档与脚本推送 `origin/main`；大型本地 Art 工作目录、dsh-image-gen 与 reference 缓存不纳入仓库。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
