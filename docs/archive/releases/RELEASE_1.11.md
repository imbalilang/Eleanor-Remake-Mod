# 1.11 发布记录（2026-11-15）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。本版本新增游戏内埃莉诺立绘选择，并保留作者现有的工坊标题与简介。

## 变更

- 新增原生 `ModConfig` 设置「立绘 / Portraits → Eleanor portrait mode」，保存在 `ModConfigs/eleanor_romance.json`。
- Auto（默认）：原版和本 MOD 剧情使用本 MOD 立绘；其他 MOD 的埃莉诺立绘保留；被覆盖时让位并暂停表情切换。
- Force：所有 MOD 的埃莉诺都使用本 MOD 立绘，覆盖其他 MOD 的埃莉诺立绘。
- Other：完全使用原版/其他 MOD 立绘；本 MOD 不替换、不切换表情，剧情客户回退到原版键。
- 原版埃莉诺接管改为按原版调度方法的作用域，不再用宽泛 `eleanor*` 标识过滤；其他 MOD 自己的埃莉诺事件不会被误拦。
- 1.10 voice pass 与 1.07–1.09 的入线卡住、物品点击区、手镯重查和立绘兼容修复保留。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：1.11 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 发布包 155 个文件、12,394,102 bytes；ZIP `dist/EleanorRomance-1.11.zip`。DLL SHA-256：`3584F67C505F8F8A9BE7427D381E5229FA19ED6FFF8C269010C6C042398C8B07`；ZIP SHA-256：`91AA6BD83DCF179D96DEE0BF5A183957B3CBA857105C2484408506ECAFB5D06B`。
- 原生能力扫描通过：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.11/native-scan.log`。
- 新设置与各立绘模式属于 Unity 实机 UI/显示行为，离线检查不能替代画面验收；完整离线运行回归仍被实际安装 Unity 程序集的 `ECall methods must be packaged into a system module` 环境错误阻塞。

## 发布状态

2026-11-15 22:22（北京时间），以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`。发布时**没有**设置标题、没有设置描述、没有请求内容分级描述符；作者现有工坊标题与简介原样保留。

旧版本曾通过 `publish_workshop.ps1 -Mature` 写入两项 Steam 内容描述符，省略 `-Mature` 不会自动移除。2026-11-15 22:30（北京时间）追加 **metadata-only** 更新，对 `NudityOrSexualContent` 和 `AnyMatureContent` 调用 `RemoveContentDescriptor`，Steam 返回 `k_EResultOK`；Steamworks 查询确认描述符数量为 0，标题、简介、文件大小和内容句柄均未改变。

Steam Web API 复核：

- 标题发布前后均为 `Eleanor Story Expand`；
- 远程说明 4,246 bytes，发布前后 SHA-256 均为 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`；
- 文件大小 12,394,102 bytes，与本地包一致；
- 公开（visibility 0），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-15T14:22:51Z，内容句柄 `8685798187511779733`。

发布证据位于 `dist/workshop-1.11/`：`remote-before.json`、`release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`、`descriptor-removal-result.json`、`descriptor-removal.log`、`descriptors-after.json`、`remote-after-descriptor-removal.json`、`final-verification.json`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。

## 2026-11-16 热修：后宫手镯持有计数

- 保持 manifest 1.11，不升版本；以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`。
- 后宫线手镯资格改为买入／卖出／回购／赠送事件计数，不再依赖库存格或实例 ID；修复免费赠送后手镯未取走、后宫资格未写入、最终进入单人恋爱线的问题。
- 旧档首次载入后扫描一次：`DPreludeState=1` 按实际持有数恢复；`DPreludeState=7` 且同一手镯仍在、尚未进入普通恋爱/告白时修复为已赠送；已结婚或已进入普通恋爱的旧档不回改。
- 本地包：139 个文件、13,257,331 bytes；`EleanorRomance.dll` SHA-256 `BD7F61E221E0E4196A5C80DFF32A41AA8EAE70F7F4C3386AC84F34C7D80F75C5`；ZIP SHA-256 `60881132B5F64A3809740F446A08BB7FEDB75C64C2F6263085B79443CB45A17A`。
- 远程复核：标题 `Eleanor Story Expand`；visibility 0；未封禁；文件大小 13,257,331 bytes；说明 4,246 bytes、SHA-256 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`（未改动）；内容句柄 `6506262506094433032`；更新时间 2026-11-16T09:33:19Z。
- `dotnet build -c Release`、`check_project.py`、`check_route_map.py` 通过。本机缺少 PowerShell 7，运行时回归与原生能力扫描无法执行；`native-scan.log` 记录的是 PowerShell 5.1 的 `MissingMethodException` 环境错误，不是扫描能力发现。
- 证据位于 `dist/workshop-1.11/`：`release-files.json`、`publish-result.json`、`publish.log`、`remote-after.json`、`final-verification.json`、`native-scan.log`；更新说明为 `docs/WORKSHOP_CHANGELOG_1.11_bracelet_hotfix.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
- 该 1.11 热修随后由 **1.12** 版本更新覆盖，见 `docs/RELEASE_1.12.md`。
