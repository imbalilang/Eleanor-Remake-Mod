# 0.84：游戏 1.0.0.14 兼容

2026-11-10。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；游戏 1.0.0.14 实机验收仍待完成。

## 原因

游戏更新到 1.0.0.14 后，模组的 `VersionGate` 仍只接受 0.46 / 0.46D。`StartingPerkRegistration.Register()` 在 `if (!VersionGate.IsSupported) return;` 处提前返回，后面的 `ModHelper.AddStartingPerk(...)` 没有执行；同一门禁还会停用剧情、物品和晚会等功能。

实机日志（`Player.log`）：

```
Game version: 1.0.0.14, supported: False
```

特性实际名称是【旧日回想】（0 点中立），不是【旧日之影】。

## 修复

- `VersionGate` 接受 `1.0.0.13`、`1.0.0.14`，同时保留 `0.46D` / `0.46`。
- `manifest.xml` 的 `GameVersions` 补入 `1.0.0.13`、`1.0.0.14`。
- 0.83 的文本、表情立绘、E3/E4 结局页和 0.82 晚会修复不变。

## 验证

- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。
- `tools/check_project.py`：通过（中文/英文各 1954 键，0 缺失内容键，0 非法块引用）。
- `tools/check_route_map.py`：通过（0.84，18 个时间线节点，E1～E14）。
- 仍需在游戏 1.0.0.14 新建游戏，确认【旧日回想】出现在天赋列表并成功进入提前开局。
- 原生扫描与完整运行时回归仍受本机缺少 PowerShell 7 限制，未记为通过。

## 构建与包

- DLL SHA256：`9703D80F7528D3FEDE87C05A579B26615EFC3048B30692D3AE10C0A2B735421E`。
- 运行包 43 个文件，2,406,524 字节。
- ZIP SHA256：`E329D7CCFEF2513DA44FB63E771534B8F1FDAA88BC520FEB681ADF02736FB2D3`。
- 工坊发布：`k_EResultOK`，完成时间 2026-11-10 21:04:26（北京时间）；内容与说明一并提交，43 个文件，2,406,524 字节。
- 发布回执：`dist/workshop-0.84/publish-result.json`；构建与检查日志同目录。
