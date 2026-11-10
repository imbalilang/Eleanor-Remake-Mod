# 0.80 发布记录

发布时间：2026-11-09 16:51（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 内容

- 全文文风修订：重写 765 条中文对白与界面文本，覆盖公共主线、侍从线、归来篇、经营互动和结局；同步调整 40 条英文文本。
- 目标是让 Eleanor、Lydia、母亲等角色更接近原作语气：更口语、更有脾气，减少说明腔、程式化措辞和生硬的总结句。
- 剧情分支、选项含义、数值、路线和存档结构不变；被回显的玩家台词与选项标签保持逐字一致。旧存档继续可用。
- 包含 0.79 的 Wire Cutter 物品修复与 Modding API 安全更新适配；`<Capabilities />` 仍为空。
- 中英各 2294 键。

## 文案检查

- `docs/archive/design/文风修订对照_2026-11-09.txt` 共 765 条，全部与当前 `Localization/zh.csv` 的新文本一致；没有残留旧文本条目。
- 相对已发布的 0.79 包：中文 765 条、英文 40 条差异，键集合与条目数不变。
- 在临时目录重新运行 `tools/build_content.py`：生成的 `zh.csv`/`en.csv` 与工作区语义完全一致；生成的 `StoryV06Content.g.cs`、`MaidContent.g.cs` 与工作区逐字一致。
- 选项回显检查：`er_maid_legacy_help` / `er_maid_legacy_retract` / `er_maid_route_difficulty` 与被回显的玩家台词保持相同文本。

## 正式包验证

- Release 编译：0 警告 0 错误。
- 本机 `EleanorRomance.dll` SHA256：`F8E1D83E15A40DEE20320106996D3692351C07A9E9198A0BBA5794D5E18D3EBD`（本次只改文本，DLL 与 0.79 相同）。
- 运行包 16 个文件，1,704,554 字节；ZIP SHA256：`A1776C4761D9DF1FFDB925B0E490EC47E430EE53B412F4749450EE61D3B3275D`。
- 已部署到本机 `Mods/EleanorRomance`（manifest 0.80，中英各 2294 键）。
- 游戏自带 `ModScanner.Scan` 扫描发布包（1069 个方法）：`Capabilities`/`Findings`/`Pending`/`HardFlags` 均为空，`UsesDeprecatedCapability=false`。
- 本轮未运行完整实机周目；文本显示与语气效果待玩家反馈。

## 工坊确认

- Steam 返回 `k_EResultOK`，原条目 `3834588749`，无需接受工坊法律协议，上传耗时 5.54 秒。
- 远程查询：标题为中英双语，visibility `0`（Public，本次更新未修改可见性），文件大小 1,704,554 字节，`time_updated` 1794214302（2026-11-09 16:51:42 +08）。
- 远程描述 5,244 字节，与 `docs/WORKSHOP_DESCRIPTION_0.80.txt` 规范化换行后逐字一致。
- 本地证据目录 `dist/workshop-0.80/`：`release-files.json`、`publish-result.json`、`remote-verified.json`；归档 `dist/EleanorRomance-0.80.zip`。
- 本机 `Mods/EleanorRomance` 为 0.80 本地副本；如同时订阅 Steam 工坊版本，本地副本会覆盖同 ID 的工坊内容，只保留一个来源。
- 未推送 GitHub；源码同步仍按手动推送规则处理。
