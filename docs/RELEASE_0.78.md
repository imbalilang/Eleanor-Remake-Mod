# 0.78 发布记录

发布时间：2026-11-09 11:41（Asia/Shanghai）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 内容

- 修复工具补给与第25章样品来访：原先引用未注册的物品 id `wrench_old`，`DirectoryMaster.Item` 找不到定义时会生成 `unknown` 精灵的占位物品，名称回退为原始 id。现使用原版扳手 `wrench_orange`（`UnusedDirectory.Wrench`，TOOL，价值 30）。
- 旧存档迁移：`Items.RepairLoadedShapes` 在读取后扫描 `wrench_old` 占位物品，补正名称为 Wrench、`Items/items_construction` / `wrench_orange` 图标、价值 30 以及 TOOL/WEAPON/MELEE_WEAPON 类型，并记录修复数量。
- Modding API 安全更新适配：假叔叔箱子的延迟回调从游戏 MonoBehaviour 上的 `StartCoroutine(MetaHelper.WaitAndDo(...))` 改为 `ModHelper.Delay(ModConstants.ModId, 0.3f, ...)`，消除 100-Rev13 的 `threads` 发现，2026-12-10 安全更新后仍可加载。
- 能力清单保持为空；不使用 `files`/`network`/反射/原生/动态代码。本次更新不新增能力，已订阅玩家无需重新授权，也不需要创意工坊开发者验证。
- 正式版与 open_beta 当前同为 1.0.0.13 / Steam build 26525552（depot manifest gid 7345874240764404166）。中英各 2294 键不变。

## 正式包验证

- Release 编译：0 警告 0 错误。
- 本机 `EleanorRomance.dll` SHA256：`1D48AFE9C5CC24E4081362FA89B3EAE3372E24690AA4BD422C13D87F67808297`。
- 运行包 16 个文件，1,701,634 字节；ZIP SHA256：`E7FEF8556F37118EAF2F76D38DE9B9645C1EB4456F47979B9C4671F18433D51F`。
- 静态检查：无 `StartCoroutine`/`InvokeRepeating` 于非自有 MonoBehaviour；无 `files`/`network`/`process`/反射/原生/动态代码用法；Harmony 补丁目标均为 `Assembly-CSharp` 类型；发布包只有 `EleanorRomance.dll` 一个 DLL。
- 使用游戏自带 `ModScanner.Scan` 离线扫描发布包与 Steam 下载缓存（各 1069 个方法）：`Capabilities`/`Findings`/`Pending`/`HardFlags` 均为空，`UsesDeprecatedCapability=false`；下载缓存 DLL 与发布包 SHA256 一致。
- 2026-11-09 正式版（public）复测：Steam public build 26525552，depot manifest gid 7345874240764404166（与 open_beta 相同）；`Assembly-CSharp.dll` SHA256 `725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05`。本地 `Mods/EleanorRomance` 与 Steam 工坊缓存的 0.78 DLL 扫描均无发现项，说明分支切换不改变扫描结果。
- 本轮未运行完整实机周目；实机显示与旧档修复效果待玩家反馈。订阅端若仍显示 `threads` 弃用提示，通常是仍加载旧 0.77 DLL 或本地 `Mods/EleanorRomance` 覆盖副本，处理方式见下节。

## 订阅端旧版提示排查

- 0.78 发布包、`bin/Release` 与 Steam 下载缓存 DLL 的 SHA256 均为 `1D48AFE9C5CC24E4081362FA89B3EAE3372E24690AA4BD422C13D87F67808297`，扫描器均无 `threads` 发现。
- 仍看到 “Uses capabilities the next game update ... (Keep running code in the background ...)” 时，说明当前加载的仍是 0.77 内容（旧 DLL 在 `DialogUIManager` 上调用 `StartCoroutine`）：
  1. 完全退出游戏后重启；模组扫描只在启动时执行，旧会话不会自动刷新。
  2. 在模组菜单确认版本为 0.78、来源为 Workshop；如存在本地 `Mods\EleanorRomance` 旧副本，它会覆盖同 ID 的工坊内容，需要更新或移除。
  3. 如 Steam 缓存仍未更新，重启 Steam、取消/重新订阅或验证游戏文件以强制重新下载工坊内容。
- 2026-11-09 13:12 正式版实机日志验证：临时移除本地 `Mods/EleanorRomance` 覆盖副本后，Steam 工坊 v0.78 正常加载，日志为 `audit: eleanor_romance v0.78 source=Workshop workshop=3834588749 owner=76561198109201343 running=True trust=Updated caps=none`，无 `threads` 或其他能力警告。
- 存在本地副本时，同一位置日志为 `Changed: record source Workshop/3834588749, now Local/0` 与 `Not loaded: Changed (当前来源与已批准副本的来源不同)`；本地副本会阻止工坊版本加载，并可能让菜单继续显示旧版本信息。

## 工坊确认

- Steam 返回 `k_EResultOK`，原条目 `3834588749`，无需接受工坊法律协议，上传耗时 7.54 秒。
- 远程查询：标题为中英双语，visibility `0`（Public，本次更新未修改可见性），文件大小 1,701,634 字节，`time_updated` 1794195703（2026-11-09 11:41:43 +08）。
- 远程描述 7,645 字节，与 `docs/WORKSHOP_DESCRIPTION_0.78.txt` 规范化换行后逐字一致。
- 本地证据目录 `dist/workshop-0.78/`：`release-files.json`、`publish-result.json`、`remote-verified.json`；归档 `dist/EleanorRomance-0.78.zip`。
- 未推送 GitHub；源码同步仍按手动推送规则处理。
