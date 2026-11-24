# 1.33 发布记录（2026-11-23）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- CG/路线结局页字体修复：
  - `src/EndingPageUI.cs` 删除 `FindFont()` 的“首个含‘埃’的字体”启发式，改为 `FontLoader.GetFontByRole(FontRole.Noto_Sans, LocalizationSettings.SelectedLocale)`。
  - 标题、正文、结局页备注和按钮统一使用当前语言字体：简中 `NotoSansSC`、日文 `NotoSansJP`、韩文 `NotoSansKR`、英文 `NotoSans`。
  - 字体查询放在独立 `TryLocaleFont()` 中并加 `NoInlining`；缺少 `FontLoader`/`GetFontByRole` 的旧构建在 `ResolvePageFont()` 捕获并回退 `TMP_Settings.defaultFontAsset`。
  - `EleanorRomance.csproj` 增加 `Unity.Localization` 编译引用（`Private=false`）。
- 立绘管理器/少女当铺兼容修复：
  - `src/PortraitCompat.cs` 的旧档 `cp_portrait_*` 清理不再无条件回退基础键；新增 `HasAnyLoadedModSprite()`，只要任一已启用 MOD 在其 `Sprites`/`PaperSprites`/`Textures` 中提供该键就保留。
  - 修复玩家在少女当铺/Flaming 立绘管理器中选择立绘后被改回原版立绘的问题。
- 旧档兼容章节与死内容清理：
  - 删除 `Content/maid_v05.json`、`maid_v05_en_part1–4.json`、`revolution_v1.json`、`interaction_v1.json`、`relationship_choices_v1.json`、`maid_id_map_v07.json`。
  - 仍在使用的 6 个侍从共享块迁入 `Content/maid_shared.json`：`m11_b001–b004`、`m23_b006`、`world_b003`；`MaidContent.g.cs` 只剩这 6 块。
  - 删除 `src/RevolutionContent.g.cs` 及 `StoryEngine` 的兜底引用；删除旧后宫 `v05_legacy_entry`、`dr_legacy_*`、8 个 `er_maid_legacy_*` UI 键；`revolution_ui.json` 只保留当前 v2 结局页 5 个键。
  - 重新生成 zh/en/ja/ko CSV：四语言各删除 954 条死键；zh 4,577 / en 4,821 / ja 4,577 / ko 4,813。
  - 删除旧生成器、旧日译批次脚本、旧阅读生成器和 `tools/legacy/import_maid_v05.py`；`check_project.py` 增加旧文件/旧类不得回归的检查。
  - 工作区清理中已删除的 32 张不再使用的表情变体也随 1.33 移除；8 张 CG 与现行立绘未删除。
- 不改当前路线、剧情、选项结果、目标日、排期或结局；旧档继续按 `PredatesStoryRewrite` 暂停/重置逻辑处理。保留 1.32 卢式晨间闭店约会、1.31 韩语校对、1.30 后宫 v2 香水覆盖及更早修复。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过（zh 4,577 / en 4,821 / ja 4,577 / ko 4,813 键；0 缺失内容键、0 缺失代码键；maid 6 块 / 16 行；dual 181 块）。
- `python tools/check_route_map.py`：通过，`route map version: 1.33 (manifest 1.33)`，5 个当前结局、20 幕、5 份指南一致。
- 原生扫描：1,571 个方法，0 capabilities / 0 findings / 0 pending / 0 hard flags。证据：`dist/workshop-1.33/native-scan.log`。
- 发布包：116 个文件，18,964,614 bytes；ZIP `dist/EleanorRomance-1.33.zip`，SHA-256 `AC42036D77398C3F25C7624AF1A1E667B453BB10668C3B1BFD1C51797B631B9E`；DLL SHA-256 `90F2A0047086714B5B9EC1FBD3ACB41640674553823E59369EF1CB7B4C36949A`；包内 `manifest.xml` ModVersion = 1.33。
- 本地化：四语言各删除 954 条死键；`ko.csv` 有 8 条 `er_v06_c09_n102_13–20` 在清理前就缺失，本次未补译。
- 实机验证：未由 Agent 执行；字体与立绘兼容修复需玩家/作者实机确认。

## 发布状态

- 2026-11-23 07:11:55（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.56 秒；标题、简介和内容描述符未改动（`-KeepDescription`）。
- Steam Web API 复核：标题保持不变；公开（Public），所有者 `76561198109201343`，文件大小 18,964,614 bytes，与本地包一致。
- 证据：`dist/workshop-1.33/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-before.txt`、`remote-after.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
- 未提交 git、未推送 GitHub。
