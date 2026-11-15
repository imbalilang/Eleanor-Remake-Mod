# 1.08 热修复发布记录（2026-11-15）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。本版本只修埃莉诺站定后短暂闪过其他 MOD 立绘的问题，并收纳上一轮手镯赠礼重查修复；不改正文、结局编号、路线条件或排期。

## 修复

- 新增到达/站定立绘校正：在 `StoreClientMono.OnArrived`、`StoreUIManager.OnNextClientArrived`、`StoreClientMono.SetImmediatlyArrive` 和 `StoreClient.StartMainDialogue` 的末位后置/前置位置，只替换外来立绘，不覆盖本 MOD 的表情和专属服装。
- 到达后 2 秒内，`StoreClientMono.Update` 末位逐帧检查当前埃莉诺客户；发现其他 MOD 写入的立绘立即恢复本 MOD 立绘，覆盖原版走入结束、站定回调或延迟回调写入的情况。
- `DisplayClientText` 不再先应用挂在外部对白上的他 MOD `Dialogue.portrait`；埃莉诺客户只等待最终校正，避免对白开始时再闪一次。
- 手镯赠礼选项的手镯重查从挂在选择回复行上的 `endAction` 改到选项 Action。原生选择流程会跳过选择回复行及其收尾动作，之前重查不会执行；现在手镯缺失时会正确进入缺失提示。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：1.08 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 发布包 153 个文件、10,714,963 bytes；ZIP `dist/EleanorRomance-1.08.zip`。DLL SHA-256：`9574CB2D8A3D73D9AD62F8C488A518496509F514E4D2ADA67396CB548E70E0DA`；ZIP SHA-256：`16E8C19156066EBEEBA1A8C57EF6EB085C2E78CAD5D9E05BD113803B872EB77A`。
- 原生能力扫描通过：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.08/native-scan.log`。
- 站定闪立绘属于 Unity UI/SpriteRenderer 实机行为，离线扫描不能证明已消失；完整离线运行回归仍被实际安装 Unity 程序集的 `ECall methods must be packaged into a system module` 环境错误阻塞。请玩家重点复测走入站定和第一句对白前后两处。

## 发布状态

2026-11-15 19:08（北京时间），Steam 对原条目 1.08 内容与英文说明返回 `k_EResultOK`；随后分别更新英文与简体中文说明页，均返回 `k_EResultOK`。Steam Web API 复核：

- 文件大小 10,714,963 bytes，与本地包一致；
- 公开（visibility 0），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-15T11:08:17Z，内容句柄 `6214488986259397958`；
- 远程说明 5,123 bytes，与 `WORKSHOP_DESCRIPTION_1.08.txt` 一致。

发布证据位于 `dist/workshop-1.08/`：`release-files.json`、`publish-result.json`、`publish-result-schinese.json`、`publish-result-english-description.json`、`publish.log`、`publish-schinese.log`、`publish-english-description.log`、`native-scan.log`、`remote-after.json`、`final-verification.json`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
