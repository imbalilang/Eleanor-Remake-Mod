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

