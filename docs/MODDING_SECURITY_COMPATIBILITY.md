# MOD API 安全更新与正式版/测试版兼容规范

更新日期：2026-11-10
适用：Eleanor Story Expand 0.77 之后的开发分支

## 结论摘要

- 0.82补充修复：0.81晚会两处在`StoreUIManager`上直接启动的协程触发`threads`弃用警告，现改为`ModHelper.StartCoroutine`托管。发布脚本必须同时检查`Pending`、`DeprecatedCapabilities`与`UsesDeprecatedCapability`；`Capabilities`、`Findings`、`HardFlags`全空仍可能有弃用警告。修正后的扫描对旧0.81包明确失败，不能沿用旧“零发现”结论。

- 官方 Modding API 文档已于 2026-11-08 更新（`dragonpan2/probably-stolen-modding` 提交 `ae16103`，100-Rev13 Future Modding Security Changes），对应游戏公告 1.0.0.13 中提到的 **2026-12-10** 安全更新。
- 2026-12-10 起，`reflection-external`、`native`、`dynamic-code`、`threads` 四种能力直接拒绝；声明 `files` 或 `network` 的创意工坊模组需要开发者验证。
- 本 MOD 保持 `<Capabilities />` 为空：不使用 `files`、`network`、反射、原生代码、动态代码或线程/任务，因此不需要开发者验证。
- 已修复：`src/EleanorStory.cs` 的假叔叔陷阱延迟回调原本在 `DialogUIManager.Instance` 上调用 `StartCoroutine`，会被扫描器判为 `threads`，12 月 10 日后拒绝加载；现改为 `ModHelper.Delay`。
- Steam **正式版与 open_beta 当前是同一个构建**（buildid `26525552`，depot manifest gid `7345874240764404166`），正式版玩家不需要切换到测试版；后续以正式版作为编译基线。
- 本次修复不新增能力、扫描应通过，已订阅玩家**不需要重新授权**。

## 依据与当前环境

- 官方文档仓库：https://github.com/dragonpan2/probably-stolen-modding
  - `ModdingGuide.zh-CN.md` / `ModdingGuide.md`
  - `APIChanges.zh-CN.md` / `APIChanges.md`
- 文档提交：`ae16103`（2026-11-08，"100-Rev13 Future Modding Security Changes"）
- 游戏公告：更新补丁 1.0.0.13（2026-11-09）
- Steam 分支（`steamcmd +login anonymous +app_info_print 4348910`，2026-11-09 核对）：

  | 分支 | buildid | depot manifest gid | 更新时间 |
  | --- | --- | --- | --- |
  | public（正式版） | 26525552 | 7345874240764404166 | 2026-11-09 03:52:27 +08 |
  | open_beta（测试版） | 26525552 | 7345874240764404166 | 同一次更新 |

- 本机程序集：`Probably Stolen_Data/Managed/Assembly-CSharp.dll`
  - SHA-256 `725cb999d019dff1ff31bed353c7f9bb8d81a7605a15de1f80f5a3a6cf7b1d05`
  - 文件大小 4,820,224 字节，内部版本 `0.46D`
- 当前构建中的扫描器开关：`ModScanner.REFUSE_UNSCANNABLE_CAPABILITIES = false`，即现在只警告；12 月 10 日更新改为拒绝。

## 12 月 10 日安全更新要点

### 1. 退出并被拒绝的能力

| 能力 | 触发示例 | 替代方案 |
| --- | --- | --- |
| `threads` | `new Thread`、`ThreadPool`、`System.Threading.Timer`、`System.Timers.Timer`、`Task.Run`、`Task.Delay`、`ContinueWith`、`Parallel.*`，以及在**不属于自己的 MonoBehaviour** 上调用 `StartCoroutine` / `Invoke` / `InvokeRepeating` | `ModHelper.StartCoroutine` / `Delay` / `RunInBackground` / `RunOnMainThread`；自己的组件可用 `this.StartCoroutine` / `this.Invoke` |
| `reflection-external` | 拼接出的类型或成员名、对 `System.*` / `UnityEngine.*`（UI 命名空间除外）/ `HarmonyLib.*` / `Steamworks.*` / `ES3` 的反射、`dynamic` 关键字 | `typeof(游戏类型)`、字面成员名、`ModReflect` |
| `native` | `DllImport`、`unsafe`、指针、`calli`、`Marshal`、`UnsafeUtility` | 不使用 |
| `dynamic-code` | `Reflection.Emit`、`Expression.Compile`、`Assembly.Load`、`TypeNameHandling` 非 `None` | `ModReflect.Method<TDelegate>` 等 |

- 目前这些发现只警告；2026-12-10 后拒绝加载，已批准的模组也会在下次启动重新扫描并停止。
- 对游戏代码之外的补丁目标（如 `System.IO.File`、`UnityEngine.Application`）按 `reflection-external` 计算；对游戏代码、本模组类型、前置模组类型以及 `UnityEngine.UI` / `TMPro` / `UnityEngine.EventSystems` 的补丁免费。
- `[ModRestricted]` 助手方法按它指明的规则计算：`SaveFiles.*` 算 `files`；`PaperEditorFiles.OpenFolder` / `OpenFile`、`ModUIManager.OpenModFolder` 这类系统外壳操作按 `process` 式拒绝。
- 一律拒绝：补丁加载器/信任/扫描器类型、`Harmony.UnpatchAll(null)`、自带 `0Harmony.dll`、未在清单列出的 DLL、嵌入可执行文件或加密载荷等。

### 2. 创意工坊验证与玩家授权

- 声明 `files` 或 `network` 的创意工坊模组，自 2026-12-10 起必须进入开发者签名验证名单才会加载；本地 `Mods/` 文件夹永远豁免。
- 验证过的模组会跳过玩家的能力确认框。
- 普通更新规则：文件变化但**没有新增能力**且扫描通过 → 自动加载，模组菜单显示“已更新”；新增能力或新版本扫描失败 → 不加载，需要玩家点“审核并启用”。
- 批准现在覆盖模组文件夹中的每一个文件；只替换数据或贴图也会重新做文件夹扫描，但不会要求重新授权。
- 本 MOD 空能力、无 `files`/`network`，不需要开发者验证，也不会因本次修复要求玩家重新授权。

## 本项目强制规则

### 1. 协程与后台工作

- 禁止在游戏对象或游戏管理器上调用 `StartCoroutine` / `Invoke` / `InvokeRepeating`。
- 使用加载器管理的接口：
  - `ModHelper.StartCoroutine(ModConstants.ModId, routine)`
  - `ModHelper.Delay(ModConstants.ModId, seconds, action)`
  - `ModHelper.RunInBackground(ModConstants.ModId, token => ..., onFinished)`
  - `ModHelper.RunOnMainThread(ModConstants.ModId, action)`
- 只有模组自己通过 `AddComponent` 创建的 `MonoBehaviour` 才能使用 `this.StartCoroutine` / `this.Invoke`。
- 现有示例：`src/EleanorStory.cs` 的 `AddTrapWatcher` 使用 `ModHelper.Delay(ModConstants.ModId, 0.3f, ...)`。旧版 `MetaHelper.WaitAndDo` 使用受时间缩放影响的 `WaitForSeconds`，新接口使用 `WaitForSecondsRealtime`；游戏没有修改 `Time.timeScale`，因此当前行为等价，且协程会随模组停止而取消。

### 2. 能力与 API 选用

- `manifest.xml` 保持 `<Capabilities />` 为空。
- 文件读写只用 `ModHelper.ReadModFile*` / `ModDataFile*` / `LoadModAssetBundle`；不要使用 `System.IO`、`FileStream`、`Directory`、`AssetBundle.LoadFromFile` 或带路径的 Easy Save API。
- 不使用 `System.Net`、`UnityWebRequest`、`WWW`；不使用 `Process`、`Application.OpenURL`；不使用 `DllImport`、`unsafe`、`Reflection.Emit`、`Expression.Compile`、`TypeNameHandling`。
- 如果未来确实需要 `files` 或 `network`：先写设计说明，申请开发者验证，并接受玩家重新授权和验证流程；不要直接发布。

### 3. Harmony 补丁

- 只对游戏程序集（`Assembly-CSharp`）中的类型或本模组类型打补丁。
- 不对 `System.*`、`UnityEngine.*`（三个 UI 命名空间除外）、第三方库类型打补丁。
- 不补 `ModLoader`、`ModTrust*`、`ModFingerprint`、`ModScanner`、`ModCapability`、`ModSecurityDebug` 等加载器/信任类型。
- 不创建自己的 `Harmony` 实例，不调用无 id 的 `UnpatchAll()`，不自带 `0Harmony.dll`。
- 需要反射时优先使用字面的游戏类型 + 字面成员名（这是免费且可被扫描器读取的）；运行期才确定的成员改用 `ModReflect`。

### 4. 正式版 / 测试版兼容

- **编译基线：正式版（public）。** 发布前检查 public 与 open_beta 的 buildid 和 depot manifest gid；一致时只发布一个 DLL。
- **分支不一致时：** 不要直接编译引用 beta 独有成员，否则正式版运行时会 `MissingMethodException` / `MissingFieldException`。用运行时探测：
  ```csharp
  var field = typeof(StoreClientData).GetField("isEleanorDead");
  if (field != null) { /* 使用 beta 独有功能 */ }
  ```
  字面游戏类型 + 字面成员名在新扫描规则下按直接调用计算，不产生 `reflection-external`；找不到时降级并记录日志。
- **不要用 `<GameVersions>` 区分分支。** public 与 open_beta 的内部版本都是 `0.46D`，该元素只是版本闸门；分支差异只能靠运行时探测或分开构建。
- 新 API 的采用顺序：优先正式版已有 API；beta 独有内容作为可选增强，不作为主流程必需条件。
- 如果未来两分支长期分叉，再评估双构建或把 beta 功能做成可选模块；当前不引入额外构建复杂度。

### 5. 发布前检查清单

1. 分支核对：
   ```bash
   steamcmd +login anonymous +app_info_print 4348910
   ```
   确认 public 与 open_beta 的 `buildid` / depot manifest gid；如果不同，按第 4 节处理。
2. 构建后进入游戏，在 F8 控制台执行：
   ```
   mod-scan eleanor_romance
   ```
   - `declared` 为空，`detected` 无阻塞项；
   - 没有 `DEPRECATED` 行；
   - 没有一律拒绝的标记。
3. 检查发布文件夹：只包含 `manifest.xml`、`README.md`、`preview.png`、`EleanorRomance.dll`、`EleanorRomance.deps.json`、`Localization/*.csv`、`Textures/Items/*.png`；没有未列出的 DLL、脚本、可执行文件、压缩包或高熵资源。
   - 本地部署同样适用：旧DLL备份必须放在整个模组目录之外，不能在根目录或子目录保留改名为 `.bak` 的程序文件。扫描器按文件内容识别，改扩展名仍会报 `payload-file -> executable content`。
   - 文件哈希和程序集测试通过后，还须对实际安装目录运行 `pwsh -File tools/scan_package.ps1 -Managed '<游戏Managed目录>' -Package '<实际安装模组目录>'`。只扫描打包暂存目录不能发现安装残留。
4. 递增 `<ModVersion>` 并更新发布说明；任何新增能力都会导致所有已订阅玩家重新授权，发布前先确认是否真的需要。
5. 本机安装用于开发时不受创意工坊验证限制；创意工坊发布包必须通过完整扫描。

## 变更记录

- 2026-11-11：修复本地索菲娅导入遗留的旧DLL备份导致的激活失败，将备份移至工作区 `dist/`，实际安装目录原生扫描通过（1515方法，0硬标记、0发现、0能力）。正式程序、图片及存档未改动。
- 2026-11-09：建立本规范；把 `AddTrapWatcher` 的延迟回调从 `DialogUIManager.Instance.StartCoroutine(MetaHelper.WaitAndDo(...))` 改为 `ModHelper.Delay(ModConstants.ModId, 0.3f, ...)`，消除 `threads` 发现。该修复尚未作为新版本发布，发布时应递增 `<ModVersion>`。

## 参考

- 官方 ModdingGuide：https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.zh-CN.md
- 官方 APIChanges：https://github.com/dragonpan2/probably-stolen-modding/blob/main/APIChanges.zh-CN.md
- 本仓库相关实现：`src/EleanorStory.cs`（`AddTrapWatcher`）、`manifest.xml`、`tools/package_release.ps1`
