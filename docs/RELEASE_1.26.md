# 1.26 发布记录（2026-11-21）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 在 1.25 全局修订基础上，对共同线、恋爱线、后宫线、侍从线与革命军线的部分对白/文案做最后收尾；中英文同步，日文/韩文本地化更新。
- 保留 1.25 全局文案修订、1.24 原生操作压价判定、1.23 王尔德筹备提示、1.22 假货检查反应、1.21 韩语语域/译名修正及更早修复。
- 不改路线、选项结果、目标日、排期或结局编号。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.26。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.26/native-scan.log`。
- 发布包：141 个文件，20,397,090 bytes；ZIP `dist/EleanorRomance-1.26.zip`，SHA-256 `94BF5B0CE32111A01318876674CFEBDC579577FC1066C1FC6B050510FE6F1630`；DLL SHA-256 `A3436DF740EFE16D09A3386E6456683FCBFA1693E104CA04D9B9ECD5BE44899E`。
- 本次改动为本地化/文案与文档，DLL 哈希与 1.25 相同；版本号按规则递增，确保订阅者收到更新。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-21 06:24:25（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.03 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,397,090 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-21T06:24:25Z`，内容句柄 `8404595693638734793`。
- 证据：`dist/workshop-1.26/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.26.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
