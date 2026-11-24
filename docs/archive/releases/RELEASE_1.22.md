# 1.22 发布记录（2026-11-20）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 王尔德筹备项目的可见性与可购买性分离：卡片按剧情节点出现（第二幕后 P1/P2，进度 24 后 P3，进度 26 后 P4）；购买严格按 P1 安顿 → P2 通行与正装 → P3 第一单 → P4 自己的事业逐级进行。前一项未购买时，后一项仍可见但不可购买。
  - 第二幕（`MaidV2State.Act2Done`）前不显示 P1/P2；P2 仍是第三幕门槛。
  - 旧侍从线（`MaidState.V2 == false`）同步补上 P2/P3 前置：P2 在 P1 结果展示后，P3/P4 需要前序全部购买。
  - `src/MaidProjects.cs`：新增 `StoryVisible`；`Visible` 只按剧情显隐，`Available` 管购买前置；未购买但剧情可见的卡片保持 `hidden=false, state=0`，由 `MaidProjectReadyPatch` 返回不可购买，因此可见但置灰。
- 假货/缺陷检查后的成交反应修复：
  - `src/StoryEngine.cs` `DealTracker.Record` 增加 `confrontSucessDiscount` 判定（原生 `InspectableHelper.OnConfrontSuccess` 添加的 -15% 临时买价特征）。卖家自行降价现在记为公平成交，不再落入折扣比例检查而被判成玩家压价。
  - 修复 c06.b005“压这么狠，我捡了一上午”在成功指出假货后误播的问题。
- 文档同步：`docs/route_map.html`（DATA.version = 1.22）、`docs/ROUTE_MAP_MAINTENANCE.md`、`docs/全局剧本.md`、`README.md`。E1–E14 结束编号与剧情节点不变。
- 不改变正文、路线、选项、主线目标日、排期或结局；保留 1.21 韩语语域/译名修正、1.20 日韩本地化、1.19 合作买家旁白修复、1.18 高清 CG 与侍从线排期、1.17 跨 MOD 兼容及更早修复。
- `Localization/ko.csv` 与 1.21 相同（6,221 键，与英文 0 缺失/多余，0 CJK 残留）；本次不涉及韩语文本修改。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过（zh 5,977 / en 6,221 键，0 缺失内容键、0 缺失代码键，块引用有效）。
- `python tools/check_route_map.py`：通过，`route map version: 1.22 (manifest 1.22)`，5 个当前结局、20 幕、5 份指南一致。
- 对 1.0.0.15 游戏程序集原生扫描：2,178 个方法，0 capabilities、0 findings、0 pending、0 hard flags、0 deprecated。证据：`dist/workshop-1.22/native-scan.log`。
- 发布包：141 个文件，20,611,481 bytes；ZIP `dist/EleanorRomance-1.22.zip`，SHA-256 `47E02D584FCCD840502F5AB4AEC5909C547A78FF4710C69A47E328CC50FA8841`；DLL SHA-256 `BA03162C4300E3BC65DDBC77FA7A8F32486D7EEA5BC2863341EB7BD65C5A952F`；包内 `manifest.xml` ModVersion = 1.22。
- `Localization/ko.csv`：6,221 键，与英文 0 缺失/多余，0 CJK 残留（与 1.21 相同，本次未改韩语文本）。

## 发布状态

- 2026-11-20 05:33:07（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 10.54 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题保持不变；公开（Public），所有者 `76561198109201343`，文件大小 20,611,481 bytes，与本地包一致。
- 证据：`dist/workshop-1.22/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.txt`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
- 本地 git 变更未提交、未推送 GitHub。
