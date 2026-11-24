# 1.23 发布记录（2026-11-20）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 新增王尔德筹备提示：任一新项目首次可见时，提示“王尔德现在可以为埃莉诺提供更多的帮助”。一轮可见只提示一次，不再按项目逐个弹窗。
- 修复侍从 v2 主线章节结束不刷新/不提示王尔德项目的缺陷：
  - `src/MaidV2Story.cs` `CompleteScene` 结束时调用 `MaidProjects.RefreshStates()`，王尔德列表立即更新。
  - `src/StoryDirector.cs` 每日开店刷新改为 `MaidProjects.RefreshStates(true)`，提示在下一次开店时发出，避免被对白收尾遮住；也覆盖已看见 P2 却从未收到提示的旧存档。
  - `src/MaidProjects.cs` 把逐项目通知合并为一轮一次的提示，并记录 `notified.p1..p4` 防止重复。
- 修复玩家反馈的侍从 v2 第20章等待问题：在第二幕告白后，P2“通行与正装”成为第三幕门槛；此前王尔德列表只静默更新，玩家不知道要去购买，导致她不再来上班。现在旧存档下一次开店会收到提示。
- 新文案 `er_maid_notice_wilde_help` 已加入简体中文、英文、日文、韩文；旧项目名提示键保留但不再由代码使用。
- 不改正文、路线、选项、剧情顺序、门槛、目标日、排期或结局；1.22 王尔德可见性/购买性规则与假货检查反应修复、1.21→1.17 修复全部保留。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过（zh 5,978 / en 6,222 / ja 5,978 / ko 6,222 键；0 缺失内容键、0 缺失代码键）。
- `python tools/check_route_map.py`：通过，`route map version: 1.23 (manifest 1.23)`。
- `python tools/check_maid_v2.py`：通过（16 场景 / 54 块 / 10 选项）。
- 四语言均含 `er_maid_notice_wilde_help`，无重复键；无占位符。
- `tools/test_runtime_flows.ps1 -Maid` 在原生 `CheckStoryLineEleanor` 处报 NullReference（phase=scenarios）；用已发布的 1.22 DLL 复跑得到同一失败，属本机测试夹具/环境问题，与 1.23 改动无关。
- 发布包：141 个文件，20,605,311 bytes；ZIP `dist/EleanorRomance-1.23.zip`，SHA-256 `C754E855188126ED1FFF05D700E338B940C84068E2BEC668F7179A0509054017`；DLL SHA-256 `A24CA9E146B2A0786F2A4693D1565DE9707C8ABC10328F5909A9E3E1C64D4B0B`；包内 `manifest.xml` ModVersion = 1.23。
- 原生扫描：2,178 个方法，0 capabilities / 0 findings / 0 pending / 0 hard flags。证据：`dist/workshop-1.23/native-scan.log`。
- Nexus 包：`dist/EleanorRomance-1.23-nexus.zip`，142 个文件，22,081,761 bytes，SHA-256 `98AFACF9A94C979FF5E303A9B3EC53023BB39C25D35D02AB8E7C35045FB1085B`；DLL SHA-256 与 Workshop 包相同，原生扫描 0 findings。证据：`dist/nexus-1.23/package-result.json`、`native-scan.log`。

## 发布状态

- 2026-11-20 06:01:26（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 9.04 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题保持不变；公开（Public），所有者 `76561198109201343`，文件大小 20,605,311 bytes，与本地包一致。
- 证据：`dist/workshop-1.23/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
- 未提交 git、未推送 GitHub。
