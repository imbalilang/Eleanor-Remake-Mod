# 1.17 发布记录（跨 MOD 兼容，候选）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 事件编号隔离：逮捕救援的保释官与释放来访改用本 MOD 私有 `er_story_bail_officer` / `er_story_freed`，不再使用原版 `eleanorBailOfficer` / `eleanorFreed`。其他 MOD（如农业扩展/牧场物语）按 `eleanor*` 过滤 `AddClient` 或清理客户时，不会误关我们的救援事件。
- 触发独立：救援由 `StoryDirector.OnGenerateCustomerVeryEarly` 的每日钩子调用 `VanillaEleanorBridge.ScheduleRescue` 排队，不再依赖原版 `CheckStoryLineEleanor` 的 Harmony 前缀执行顺序；重复检查基于当前客户与队列中的救援客户。
- 立绘让位：`PortraitCompat.TryNativeEleanorOverride` 在检测到另一个 MOD 的埃莉诺客户（当前客户或 `clientStack[0]`，`sourceModId` 非本 MOD）时不再把原生键替换成本 MOD 默认立绘，保留对方的 `eleanor_scav` / `eleanor1` 等原生立绘；本 MOD 自己的 `er_portrait_*` / `cp_portrait_*` 剧情立绘不受影响。
- 价格隔离：`Patch_PreludeBraceletPrice` 增加 `Priority.Last` 后置补丁，在其他 MOD（如农业扩展制服交易加成）修改价格后重新写回前章手镯固定价，保证 `PreludeFixedPrice` 的精确价状态判定。
- 共享世界参数（`nutrifruitSeedsFreed`、原版保释字段、经济价格等）仍允许两边共同修改；本次不尝试合并两边剧情或母亲结局。
- 保留 1.16 晚会立绘修复、1.15 双人演出名字与后宫约会日修复、1.14 游戏版本策略、1.13 母亲交易与保释修复及更早修复。
- 不改正文、路线、选项、排期或结局。

## 兼容边界

- 使用农业扩展“真正的水培农场主”开局时，两套埃莉诺主线仍会并行；本版本保证双方事件都能触发、互不按 ID 关闭，但不合并两套母亲/数据盘结局。
- 农业扩展共用 `nutrifruitSeedsFreed` 等世界参数；按作者要求允许共同修改，可能出现两边结局组合产生的世界状态差异。
- 其他 MOD 的客户若没有设置 `sourceModId`，本 MOD 无法识别其归属，原生键立绘仍可能被替换；农业扩展已设置 `sourceModId = agri_expansion`，可被识别。

## 验证

- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，1.17 与 manifest、站点一致。
- 发布包：139 个文件，13,262,353 bytes；ZIP `dist/EleanorRomance-1.17.zip`，SHA-256 `650931C8810CEF0FE1FD0371D8E86A0641F57B1AE69B0CE6176F41A0290B88EF`。
- 原生能力扫描：本机 PowerShell 5.1 仍出现已知环境错误 `MissingMethodException: System.String[] System.String.Split(Char, System.StringSplitOptions)`，报告 `capabilities`/`hardFlags`/`findings`/`pending` 为空；这是扫描器运行环境问题，不是能力发现；日志见 `dist/workshop-1.17/native-scan.log`。
- 实机待验证（发布后仍需回归）：
  1. 农业扩展开局 + 本 MOD，触发第 5 章逮捕；确认保释官/释放客户为 `er_story_*`，农业扩展的 `eleanor*` 过滤不会清掉它们，救援能完成。
  2. 农业扩展的周常埃莉诺客户与我们的章节同日出现；两边都能接待，均不互相吞事件。
  3. 农业扩展使用 `eleanor_scav`/`eleanor1` 的客户保留原生立绘；我们的 maid/gala/恋爱立绘不变。
  4. 穿农业扩展商人制服卖前章手镯，报价仍为固定值，前章状态能正确推进。
  5. 存读档后救援客户不被农业扩展 `Tick` 清理，我们的剧情不被停用。

## 发布状态

- 2026-11-18 10:18:21 UTC 以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.53 秒；标题、简介和内容描述符未改动。
- 发布包：139 个文件，13,262,353 bytes；ZIP `dist/EleanorRomance-1.17.zip`，SHA-256 `650931C8810CEF0FE1FD0371D8E86A0641F57B1AE69B0CE6176F41A0290B88EF`。
- 远程查询复核：Public，所有者 76561198109201343，文件大小 13,262,353 bytes（与本地包一致），标题/简介未改。证据：`dist/workshop-1.17/publish-result.json`、`remote-before.txt`、`remote-after.txt`。
- 发布前线上为 1.16（13,260,027 bytes）；本次为正常递增，不是降级。
- 实机回归仍建议按上面的五项执行；本版本先按作者要求发布，后续如有问题以新版本修正。
