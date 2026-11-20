# 0.70 发布记录：v0.7 选项与筹备修订

发布时间：2026-11-08 02:51（Asia/Shanghai；Steam 完成时间 2026-11-07T18:51:46Z）。原条目：[Steam Workshop 3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

本轮把 v0.7 计划中的问题 D（王尔德项目提前可见/可购买）、C（暂缓存档迁移）、B（旧约序章重写）、A（第12～24章选项与文本修订）合并为 0.70 发布。问题 D 的独立 0.69 热修复未单独发布。

## 发布内容

- 王尔德七种项目定义默认隐藏；按剧情步骤逐项显示，未来项目不显示、不预告。
- 读档撤销异常提前解锁并按原生报价退款；增加 `NetworkUpgrade.Unlock` 兜底，阻止未经事务的 `er_maid_` 解锁。
- `Confession==3` 暂缓存档读档迁移为主从恋爱确认；第22章按 `migrated.lydia_deferred` 跳过已播的莉迪亚追问。
- 旧约序章改为卖货→侦探消息→收回旧话两选→条件→最终确认；不扣款，最终确认才写入方向。
- 第12、13、14、16、20、22、24章选项与台词修订；删除第12章提前退出、第16章三百补偿菜单与第20章暂缓。
- 内容管线从 v0.7 重导：184 块／936 行；`Content/maid_id_map_v07.json` 记录旧→新映射；中英各 2067 键。

## 构建

本机游戏安装于 `D:/Program Files (x86)/Steam/steamapps/common/Probably Stolen`。本机没有 .NET SDK，使用现有 VS2019 BuildTools 的 Roslyn `csc.exe`，引用官方 `NETStandard.Library.Ref 2.1.0` 参考程序集及游戏 `Managed` 下的 `Assembly-CSharp.dll`、`UnityEngine.CoreModule.dll`、`0Harmony.dll` 编译。产物引用集合与 0.68 一致：`netstandard 2.1`、`Assembly-CSharp`、`UnityEngine.CoreModule`、`0Harmony`。

- DLL：278,528 字节，846 个方法（0.68 为 838），SHA256 `7B81882E565BC7F5A6271F412B07CBFF56ED13F7124B72B9571F0CC2C76DA267`。
- 发布包：15 个文件，共 1,560,031 字节。
- ZIP：`dist/EleanorRomance-0.70.zip`，SHA256 `92BF2E920E3048921CA9CF3D32D0A8AD6AA060DA851BD61693089D482F9AFD8D`。

## 验证

- `tools/check_project.py`：无缺失中英键、无未知块引用、占位符一致；中英各 2067 键，UI 87 键。
- `tools/import_maid_v05.py` 重跑验证：184 块／936 行，映射保留 171 条、改动 3 条、新增 13 条、删除 16 条。
- 使用 Unity.Burst.Cecil 对本包做只读审计：仅引用上述四个程序集，846 个方法，0 条 System.IO／Net／Diagnostics／Reflection.Emit／Security／Management 敏感引用。
- **未能运行** `tools/scan_package.ps1` 的原生 ModScanner 和 `tools/test_runtime_flows.ps1 -Maid` 的完整回归：两者依赖 PowerShell 7（`.NET`/`AssemblyLoadContext`），本机只有 Windows PowerShell 5.1。扫描器在 PS 5.1 下因缺少 `String.Split(Char, StringSplitOptions)` 重载而中止，属于运行环境缺少，不是包解析失败。发布基于上述静态检查、Cecil 审计和既有 0.68 回归基线完成；未把这两项标记为通过。
- 未启动游戏做 Unity 界面/存档实机验收；未操作玩家存档。

## 上传结果

`tools/publish_workshop.ps1` 使用游戏自带 Steamworks 客户端更新原条目，Steam 返回 `k_EResultOK`，无需接受新协议，耗时 7.53 秒。

- item：3834588749
- localFiles：15
- localBytes：1,560,031
- completedUtc：2026-11-07T18:51:46.9459880Z

公开 API 复核（`dist/workshop-0.70/remote-after.json`）：

- `file_size`：1,560,031（与本地包一致）
- `visibility`：0（公开）
- `banned`：0
- `time_updated`：2026-11-07T18:51:47Z
- 远程描述 5,740 字节，包含 `0.70` 与 `v0.7`

## 证据文件

- `dist/workshop-0.70/EleanorRomance/`：发布内容
- `dist/workshop-0.70/release-files.json`：文件清单与 SHA256
- `dist/workshop-0.70/publish-result.json`：Steam 返回
- `dist/workshop-0.70/publish.log`：上传过程
- `dist/workshop-0.70/remote-after.json`：上传后公开 API 状态
- `dist/workshop-0.70/manual-cecil-audit.json`：Cecil 只读引用/敏感 API 审计
- `dist/workshop-0.70/native-scan-ps5-error.json`：原生扫描在 PS 5.1 下的环境错误记录
- `dist/package-0.70.log`：打包输出
