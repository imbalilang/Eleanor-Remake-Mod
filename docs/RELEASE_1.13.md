# 1.13 发布记录（2026-11-17）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 母亲的日用品来访改为同时接受 `HOUSEHOLD_GOOD` 和 `FOOD`，移除隐藏的单一 Nutrifruit 特例；界面现在显示两个兴趣标签，普通分支台词同步为“日用品，还有吃的”，c18 回应同步。
- 被捕保释/释放调度只复用原生救援客户资格与工厂，不再顺带触发无关的固定日访谈。
- 保留 1.12 后宫手镯事件计数修复、1.11 立绘选择、1.10 voice pass 与更早修复。
- manifest、README、路线图和站点同步 1.13。

## 验证

- `python tools/build_content.py`：通过。
- `python tools/check_project.py`：通过，中英无缺键、无无效块引用。
- `python tools/check_route_map.py`：通过，1.13 与 manifest 一致。
- `dotnet build EleanorRomance.csproj -c Release`：0 警告、0 错误。
- 发布包与 Steam 结果见下方发布状态；未执行本地 `Mods/` 部署。

## 发布状态

- 发布包：139 个文件，13,244,874 bytes；ZIP `dist/EleanorRomance-1.13.zip`，SHA-256 `7CE572E6F796758CAB768818D4635C8814330E608F0629CF5BD420C965E79F61`。
- 2026-11-17 01:36（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 5.60 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题 `Eleanor Story Expand`，文件大小 13,244,874 bytes，与本地包一致，公开、未封禁。
- 证据：`dist/workshop-1.13/publish-result.json`、`remote-after.json`；更新说明 `docs/WORKSHOP_CHANGELOG_1.13.txt`。

## 2026-11-17 兼容更新（同一 1.13 条目）

- 游戏更新到 1.0.0.15 后，精确版本白名单会停用剧情、物品和开局特性；现改为版本号只记录日志，不再作为运行门槛。旧存档会从原章节继续。
- `manifest.xml` 的 `<GameVersions />` 清空，避免模组菜单因版本不匹配显示红色提示。
- 未改正文、路线、结局或排期；另行审核中的正文批次不包含在本更新中。
- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`、`python tools/check_route_map.py`：通过。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.13-followup/native-scan.log`。
- 发布包：139 个文件，13,258,238 bytes；ZIP `dist/EleanorRomance-1.13-followup.zip`，SHA-256 `0C5E6857DD2FFB6359587C70D09252CA92BF24B6F9CA217B16948706FDF36599`；DLL SHA-256 `13FF16053C903024277E85046116F5CD75B63B05F60F3678993557348DBC3535`。
- 更新说明：`docs/WORKSHOP_CHANGELOG_1.13_followup.txt`。

### 兼容更新发布状态

- 2026-11-17 13:35:20（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 6.53 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题 `Eleanor Story Expand`；简介 4,246 bytes，SHA-256 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`（与之前一致）；文件大小 13,258,238 bytes，与本地包一致；公开（visibility 0），未封禁。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-17T13:35:20Z`，内容句柄 `3011692025661730627`。
- 证据：`dist/workshop-1.13-followup/release-files.json`、`publish-result-followup.json`、`publish.log`、`native-scan.log`、`remote-after-followup.json`。
- 该 1.13 follow-up 没有提升版本号，玩家端不会按版本自动更新；已由 1.14 取代，见 `docs/RELEASE_1.14.md`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。

