# 1.18 发布记录（2026-11-18）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 五张现有剧情/结局 CG 更新为 1664×936 高清导出：`er_ending_love_wedding`、`er_event_harem_lydia_date_cg`、`er_event_love_water_cut_cg`、`er_event_maid_gala_cg`、`er_event_revolution_embrace_cg`；`EndingPageUI.GetCgSprite` 对这五张图启用双线性过滤。侍从、后宫和革命军的像素风结局 CG 保持最近邻过滤。
- 侍从线 v2 后日谈 25–28 场改为按上一次来访日期相对排期：主线结束后第 25 场次日出现，之后每场间隔 2 天；不再因为绝对目标日而在 gala 结束语“明天照常来上工”之后长时间没有剧情。
- 保留 1.17 跨 MOD 兼容、1.16 晚会立绘修复、1.15 双人演出名字与后宫约会日修复、1.14 游戏版本策略及更早修复。
- 未改主线正文、路线、选项、主线目标日或结局；另行审核中的正文批次不包含在本版本中。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`、`python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.18，站点副本一致。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.18/native-scan.log`。
- 发布包：139 个文件，19,424,246 bytes；ZIP `dist/EleanorRomance-1.18.zip`，SHA-256 `AA097A4AEE98BE6EF8C549628DB6B062F914EDCD3D1FC46DB290B79DA8BF3A3F`；DLL SHA-256 `9D2C5BA46C47585033D25A577939E1800D8B4C7F9560C31116B896161E1CA545`。
- 高清 CG 的母图、导出尺寸、过滤方式和哈希见 `Art/cg-display-2026-11-18/asset_validation.json`。

## 发布状态

- 2026-11-18 13:48:39（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.53 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题 `Eleanor Story Expand`；简介 4,246 bytes，SHA-256 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`（与之前一致）；文件大小 19,424,246 bytes，与本地包一致；公开（visibility 0），未封禁。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-18T13:48:39Z`，内容句柄 `5787008847347446872`。
- 证据：`dist/workshop-1.18/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。

## 待确认

- 玩家反馈的“侍从线在 gala 结束语后剧情暂停”仍需要 `Player.log`/存档确认。本版本解决的是侍从线后日谈的绝对目标天空窗；如果日志显示 `v2_a3c3` 的完成事件未提交或客户端队列卡住，需要另做完成回退修复，不能把本版本当作该玩家问题的最终定位。
