# 1.38 发布记录（2026-11-24）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 范围

本次是从 1.37 发布提交 `c466242` 拉出的独立热修工作树，只包含：

- `src/PortraitCompat.cs`
- `src/Patches.cs`
- `src/StoryPresentation.cs`
- `src/DialogueSpeakers.cs`
- `manifest.xml` / `README.md` 的 1.38 版本更新

不包含：韩语校对修改、另一 Agent 正在处理的 CG 贴图/CG 文档和后续剧情。

## 变更

- 修复与少女当铺/立绘管理器一起启用时，角色入场后完全停下时已选立绘被改回默认/原生立绘的问题。
- Auto 模式不再覆盖其他立绘来源；本 MOD 不再改写不属于自己角色的立绘键。
- 修复双角色到访时后场角色第一句固定显示“治安部警官”：在原生 SEC 名字牌初始化后再次写入同伴名；真正的保安双人场景保持警官名。
- 不改剧情、路线、选项结果、目标日、结局或排期。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- 原生能力扫描（PowerShell 7）：1679 个方法，0 capabilities、0 hard flags、0 findings、0 pending、0 deprecated。
- 发布包：116 个文件，17,614,101 bytes；ZIP SHA-256 `46C218C635788E588687A17DA90509A10D6523D437B9BB04403DC192DE003BFE`；DLL SHA-256 `28464F5B8377902E2E3CCE22AAD9E208714C5677D7882D62E74FF9097B2DF57E`。

## 发布状态

- 2026-11-24T14:49:19Z（UTC）以 **content-only** 模式更新原条目，`-KeepDescription`，未传 NotesFile；Steam 返回 `k_EResultOK`，耗时 13.58 秒。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 17,614,101 bytes，与本地包一致；公开、未封禁；简介 SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`，与 1.32/1.34 一致，未修改。
- 证据：`B:\Eleanor Story Expand\dist\workshop-1.38\`（`release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`）及 `dist/EleanorRomance-1.38.zip`。
- 未执行本地 `Mods/` 部署；GitHub 未推送；共享主工作区和另一 Agent 的未提交改动未被触碰。
