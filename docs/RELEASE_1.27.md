# 1.27 发布记录（2026-11-21）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 修复恋爱线第二幕停水场景与原生 Serpico 剧情来访日的冲突：停水场景会强制收店并解散客户队列，若当天同时排入 `love_a2_01`，Serpico 的数据选择或最终奖励客户会被一并解散。
- `LoveDirector.SerpicoStoryVisitDue` 在 Serpico 来访到期时阻止首次第二幕来访排期（`NextVisit` 返回 0，`IsA2FirstDue` 尊重待处理来访 4），恋爱场景顺延到次日。
- 保护 Serpico1/3/4/5/6、信件日、警官日与文件选择日；收尾奖励日仅在已做出真实文件选择（`serpicoDocumentChoice` 为 1/2/3）时保护。
- 包含已发布的 1.26 全局文案收尾与本地化批次；保留 1.25 全局文案修订、1.24 原生操作压价判定、1.23 王尔德筹备提示、1.22 假货检查反应、1.21 韩语修正及更早修复。
- 不改路线、选项结果、目标日、排期或结局。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.27。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.27/native-scan.log`。
- 发布包：141 个文件，20,398,440 bytes；ZIP `dist/EleanorRomance-1.27.zip`，SHA-256 `30E1D659C611DFC702B8D4FD220CA2AEF9CDA72D76AFDB9ED4F84D2DE3EA7221`；DLL SHA-256 `2E083F874002E90DB65AB7CDF1CDCD9CD5EC802370EA9C5DB69278D3975E6246`。
- 本次含运行时代码修复，DLL 哈希与 1.26 不同。
- Serpico 判定依赖引擎字段 `StoreClientData.serpicoDocumentChoice` 与 `SerpicoStoryline.*Day`，已针对 1.0.0.15 程序集编译通过；本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-21 06:47:15（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.53 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,398,440 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-21T06:47:15Z`，内容句柄 `8803130555523910446`。
- 证据：`dist/workshop-1.27/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
