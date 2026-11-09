# 0.79 发布记录

发布时间：2026-11-09 14:32（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 内容

- 修正工具补给与第25章样品来访的第二件工具：0.78 使用的 `wrench_orange` 属于 `UnusedDirectory`，名称硬编码、没有正常本地化条目，正常游戏也无法获得扳手。现改用原版剪线钳 `wire_cutter`（名称/描述/风味文本使用 `item_wire_cutter_*`，图标 `Items/items_tool` / `wirecutter`，价值 40，类型 TOOL），与螺丝刀一起提供。
- 旧存档中的 `wrench_old` 占位物品在读取后补正为当前语言的 Wire Cutter 名称、图标、描述和价值；内部标识仍为 `wrench_old`，新生成的物品是真正的 `wire_cutter`。
- 保留 0.78 的 Modding 安全更新修复：假叔叔箱子的延迟回调使用 `ModHelper.Delay`，没有 `threads` 发现；能力清单为空，不需要开发者验证，已订阅玩家无需重新授权。
- 正式版与 open_beta 当前同为 1.0.0.13 / Steam build 26525552（depot manifest gid 7345874240764404166）。中英各 2294 键不变。

## 正式包验证

- Release 编译：0 警告 0 错误。
- 本机 `EleanorRomance.dll` SHA256：`F8E1D83E15A40DEE20320106996D3692351C07A9E9198A0BBA5794D5E18D3EBD`。
- 运行包 16 个文件，1,702,337 字节；ZIP SHA256：`D3810F8BA13AA5B13C7333402A858040FACD39E6A78A63C53492BEA681916D94`。
- 反编译确认补给/样品库存为 `screwdriver` + `wire_cutter`，发布包中无 `wrench_orange` 生成。
- 使用游戏自带 `ModScanner.Scan` 扫描发布包（1069 个方法）：`Capabilities`/`Findings`/`Pending`/`HardFlags` 均为空，`UsesDeprecatedCapability=false`。
- 本轮未运行游戏内补给场景实测；编译、包校验与扫描通过，实机物品显示待玩家反馈。

## 工坊确认

- 内容上传：Steam 返回 `k_EResultOK`，原条目 `3834588749`，无需接受工坊法律协议，上传耗时 5.53 秒。
- 远程查询：标题为中英双语，visibility `0`（Public，本次更新未修改可见性），文件大小 1,702,337 字节，内容 `time_updated` 1794205924（2026-11-09 14:32:04 +08）。
- 2026-11-09 15:08（+08）仅更新页面描述：`tools/publish_workshop.ps1 -DescriptionOnly` 返回 `k_EResultOK`，耗时 1.03 秒，未重传文件，远程文件大小仍为 1,702,337 字节。
- 新版远程描述 5,244 字节，与重写后的 `docs/WORKSHOP_DESCRIPTION_0.79.txt` 规范化换行后逐字一致；正文改为玩家向文案，技术细节保留在本记录。
- 本地证据目录 `dist/workshop-0.79/`：`release-files.json`、`publish-result.json`、`remote-verified.json`、`description-update-result.json`、`description-update-verified.json`、`description-before-rewrite.txt`；归档 `dist/EleanorRomance-0.79.zip`。
- 为测试工坊版本，本机本地 `Mods/EleanorRomance` 副本已停用并备份在 `dist/local_override_backup_20261109` / `dist/local_override_disabled_20261109`；`Mods/` 当前为空，因此本次没有本地部署覆盖工坊内容。
- 未推送 GitHub；源码同步仍按手动推送规则处理。
