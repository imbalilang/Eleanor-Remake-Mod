# 1.19 发布记录（2026-11-19）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 修复第 21 章合作买家 `er_story_coop_buyer` 的离开旁白：原来 dismiss 包装使用 `er_name_upper_buyer + er_line_coop_buyer`，与成交后的 Narrator 旁白重复，且把括号旁白署成上层区客人。
- 现在 dismiss 包装使用 Narrator + 新键 `er_line_coop_buyer_close`（中文“（上层区的客人离开了窗口。）”，英文 “(The upper-district customer leaves the window.)”）。
- 原版每次成交后的 `dialog_util_after_sell`（“Was there anything else?”）仍按原版流程显示；若玩家卖出多件，仍会每次成交出现一次。这是原版行为，不在本次修改范围内。
- 不改正文、路线、选项、排期或结局；1.18 高清 CG/侍从线排期、1.17 跨 MOD 兼容、1.16 晚会立绘及更早修复保留。

## 验证

- `python tools/build_content.py`：通过，新增中英键 `er_line_coop_buyer_close`。
- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。
- `python tools/check_project.py`：通过，中英无缺键、无无效块引用。
- `python tools/check_route_map.py`：通过，1.19 与 manifest、站点一致。
- 实机建议：触发第 21 章合作买家，成交后离开；确认离开时显示 “Narration: (The upper-district customer leaves the window.)”，不再出现 “Upper-District Customer: (The goods Eleanor picked sell cleanly...)”。日志/历史中成交旁白只出现一次。

## 发布状态

- 发布包：139 个文件，19,404,005 bytes；ZIP `dist/EleanorRomance-1.19.zip`，SHA-256 `8F5A52EDE1638AADC9B3F293AF55A26EE87E0A7FE1F8DCD96A2222CF526D5F9F`；`EleanorRomance.dll` SHA-256 `E87DB555F7FD1154F881D8EDBD353E7321FD6C6EE1A844BA0EF44E353A677761`。
- 2026-11-19 05:25:02 UTC 以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.05 秒；标题、简介和内容描述符未改动。
- 发布前线上为 1.18（19,424,246 bytes）；1.19 正常递增。远程复核：Public，所有者 76561198109201343，文件大小 19,404,005 bytes，与本地包一致。
- 证据：`dist/workshop-1.19/publish-result.json`、`remote-before.txt`、`remote-after.txt`。
- 未推送 GitHub；本轮仅更新创意工坊。
