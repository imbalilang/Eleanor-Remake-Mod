# 1.24 发布记录（2026-11-21）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- `DealTracker` 删除“成交价低于市场价 15% → 压价”的价格兜底；压价只由原生砍价/黑名单/威胁操作判定。
- 批发、市场波动、检查/对峙成功后的降价等未砍价成交按公道价反应，不再误播“压这么狠，我捡了一上午”。
- 保留 1.23 王尔德筹备提示、1.22 假货检查反应、1.21 韩语语域/译名修正、1.20 日韩本地化及更早修复。
- 不改正文、路线、选项、目标日、排期或结局。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.24。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.24/native-scan.log`。
- 发布包：141 个文件，20,624,715 bytes；ZIP `dist/EleanorRomance-1.24.zip`，SHA-256 `3C98B776FB5ABB0AB29565A4958542A7DB0B14C352A52D643E58F5E4ED6DB529`；DLL SHA-256 `38FEB379AE82F425A3AC9F99AAB39BC5C090B8C847617E90C372A76DC91B0E6E`。
- `tools/test_runtime_flows.ps1` 已加入回归断言：没有原生砍价操作的低成交价必须为公道价。本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行。

## 发布状态

- 2026-11-21 03:02:26（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.53 秒；本次没有设置标题/简介/内容描述符。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 20,624,715 bytes，与本地包一致；公开（visibility 0），未封禁。远程简介为 4,485 bytes，SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`（由之前版本保留，本次未修改）。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-21T03:02:26Z`，内容句柄 `6399499760478346104`。
- 证据：`dist/workshop-1.24/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.24.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
