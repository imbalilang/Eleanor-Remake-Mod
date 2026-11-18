# 1.15 发布记录（2026-11-18）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 双人场景第二个说话人的首句名字：在原生 SEC 名字气泡激活前写入同伴名，修复首句短暂显示“治安官警长”、下一句才恢复正常的问题。
- 后宫线 dv09→dv10：约定约会后的第二天开门时直接进入约会事件并接管白天营业；约会 CG 结束后关闭当天营业，返回当天晚上/关店后自由活动，不再自动进入次日。
- 保留 1.14 游戏版本策略、1.13 母亲交易与保释修复、1.12 手镯修复、1.11 立绘选择及更早修复。
- 未改正文、路线、结局或排期；另行审核中的正文批次不包含在本版本中。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`、`python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.15，站点副本一致。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.15/native-scan.log`。
- 发布包：139 个文件，13,258,963 bytes；ZIP `dist/EleanorRomance-1.15.zip`，SHA-256 `8EF93E0FE667788D589CA93B1ECCFA75B23CD25D851F63C84B04046BFD618A2F`；DLL SHA-256 `A6FBC02F7035589C1875F212C51C22FADAC1CF4A60208DA95F1DB25830D18B6C`。

## 发布状态

- 2026-11-17 19:29:11（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.53 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题 `Eleanor Story Expand`；简介 4,246 bytes，SHA-256 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`（与之前一致）；文件大小 13,258,963 bytes，与本地包一致；公开（visibility 0），未封禁。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-17T19:29:11Z`，内容句柄 `6009641790254254107`。
- 证据：`dist/workshop-1.15/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.15.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
