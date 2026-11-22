# 1.25 发布记录（2026-11-21）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 应用全局文案修订：共同线、恋爱线、后宫线、侍从线、革命军线及其 UI 对白/文案更新；中英文同步，日文/韩文本地化一并更新。
- 保留 1.24 原生操作压价判定、1.23 王尔德筹备提示、1.22 假货检查反应、1.21 韩语语域/译名修正、1.20 日韩本地化及更早修复。
- 不改路线、选项结果、目标日、排期或结局编号。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.25。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.25/native-scan.log`。
- 发布包：141 个文件，20,403,050 bytes；ZIP `dist/EleanorRomance-1.25.zip`，SHA-256 `12CFC97D65454AF8D0EF7481DE6962B0CC07F019117F8FCB51FAF857303841BB`；DLL SHA-256 `A3436DF740EFE16D09A3386E6456683FCBFA1693E104CA04D9B9ECD5BE44899E`。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行；构建、内容键和原生扫描通过。

## 发布状态

- 2026-11-21 05:48:04（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.07 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,403,050 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-21T05:48:04Z`，内容句柄 `8563882838245530516`。
- 证据：`dist/workshop-1.25/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.25.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
