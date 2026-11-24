# 1.14 发布记录（2026-11-17）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 游戏更新到 1.0.0.15 后，移除 `VersionGate` 的精确版本白名单；`Application.version` 只记录日志，不再停用剧情、物品或开局特性。已有存档会从原章节继续。
- `manifest.xml` 的 `<GameVersions />` 清空，避免模组菜单因版本不匹配显示红色提示。
- 保留 1.13 母亲日用品交易与保释调度修复、1.12 手镯修复、1.11 立绘选择及更早修复。
- 未改正文、路线、结局或排期；另行审核中的正文批次不包含在本版本中。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`、`python tools/check_route_map.py`：通过，路线图与 manifest 均为 1.14，站点副本一致。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.14/native-scan.log`。
- 发布包：139 个文件，13,258,058 bytes；ZIP `dist/EleanorRomance-1.14.zip`，SHA-256 `F223A9C574C0C22CB6BFBB4C7888A5F19F05E6E830FB6416E37A9A1A454200EB`；DLL SHA-256 `13FF16053C903024277E85046116F5CD75B63B05F60F3678993557348DBC3535`。

## 发布状态

- 2026-11-17 13:40:21（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.60 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题 `Eleanor Story Expand`；简介 4,246 bytes，SHA-256 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`（与之前一致）；文件大小 13,258,058 bytes，与本地包一致；公开（visibility 0），未封禁。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-17T13:40:21Z`，内容句柄 `2350203858003135759`。
- 证据：`dist/workshop-1.14/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。

## Nexus 离线包

- 目录：`dist/nexus-1.14/EleanorRomance`（140 个文件，14,734,363 bytes）；ZIP `dist/EleanorRomance-1.14-nexus.zip`。
- ZIP SHA-256：`D0DF1FE2C12BF6D1ABCFC010A5B928399A1748FA08E88736BE9A66F6135F64A9`；DLL SHA-256：`13FF16053C903024277E85046116F5CD75B63B05F60F3678993557348DBC3535`。
- 内置 `cp_portrait_eleanor*` 立绘 5 张；manifest 版本 1.14、名称带 `[Nexus]`、描述带 Nexus 内置立绘说明。
- 清单 SHA-256 逐一复核一致；原生扫描 0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/nexus-1.14/release-files.json`、`package-result.json`、`native-scan.log`。
- 本 Nexus 包不包含另行审核中的正文批次；未上传 Nexus，未做本地部署。
