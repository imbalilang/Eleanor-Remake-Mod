# 1.20 发布记录（2026-11-19）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 新增日语 `Localization/ja.csv` 与韩语 `Localization/ko.csv`，覆盖 5,977 个当前本地化键：共同线、恋爱线、后宫线、侍从线、革命军线、结局、UI、道具、新闻与短访。
- 游戏语言选择日本語／한국어时由 ModLoader 读取对应 locale 文件；缺失键回退英文。
- 不改变正文、路线、选项、目标日、排期或结局；1.19 合作买家离开旁白修复、1.18 高清 CG/侍从线排期、1.17 跨 MOD 兼容与更早修复保留。

## 验证

- `python tools/apply_ja_translations.py`：5,977/5,977 键。
- 标签与占位符检查：`<i>`、`<b>`、`*...*`、`♥`、`{0}` 等结构与中文源一致。
- `python tools/check_project.py`：通过。
- `dotnet build EleanorRomance.csproj -c Release`：通过。
- `python tools/check_route_map.py`：1.20 同步。
- 实机建议：分别切换日本語／한국어，抽查 C01、各路线一章及一个结局的显示与换行。

## 发布状态

- 发布包：141 个文件，20,604,388 bytes；ZIP `dist/EleanorRomance-1.20.zip`，SHA-256 `552E54F8A3FBCF68D97EA373268C2B84E9006EF0F3F93791598B0918FAF3A818`；`EleanorRomance.dll` SHA-256 `E87DB555F7FD1154F881D8EDBD353E7321FD6C6EE1A844BA0EF44E353A677761`。
- 2026-11-19 11:14:43 UTC 以 **content-and-description** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 7.55 秒。
- 发布前线上为 1.19（19,404,005 bytes）；1.20 正常递增至 20,604,388 bytes。英文简介增加 Languages 段；中文简介未改动。
- 线上复核：Public，所有者 76561198109201343，文件大小 20,604,388 bytes，与本地包一致；英文简介 4,359 bytes。
- 证据：`dist/workshop-1.20/publish-result.json`、`remote-before.txt`、`remote-after.txt`、`remote-after-api.json`、`release-files.json`。
- 未进行本地游戏内显示测试；正文、路线与结局未改。
