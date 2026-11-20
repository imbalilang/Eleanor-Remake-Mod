# 1.06 发布记录（2026-11-15）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 内容

- 统一埃莉诺立绘：原版、其他 MOD 和本 MOD 场景中，识别为埃莉诺的访客一律使用本 MOD 立绘；本 MOD 的作者服装与表情仍保留。`GetSprite` / `ShowClientSprite` / `GetNextClient` / `DisplayClientText` 多层锁定，外部立绘键或补丁不再造成同一存档内立绘混用。
- 吊坠、手镯与推荐/赞助/谢礼饰品在运行时生成约 1% 不透明度的白色命中底，整件物品矩形可点击；不透明像素、裁切、缩放和外观不变。唯一物品贴图来源仍是原来的三张 PNG。
- 任一现行结局（E1/E2/E3/E4/E9）后，下一次开店空闲时“下层区档案员”来访，可重置本轮路线并从共同线重开另一条路线。选项为立即重置、8 天后再问、本存档不再询问；重置保留已获得物品、真结局元进度和已发生的新闻/声望等世界效果。旧档档案员的原条件不变。
- 不新增或重排结局编号，不改变路线条件、目标日或正文。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用；`python tools/check_route_map.py` 通过：1.06 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 发布包 153 个文件、10,709,230 bytes；正式归档 `dist/EleanorRomance-1.06.zip`。DLL SHA-256：`A0CE2453F5CC857C9C0DD718208886612E7A47FDD6344345F1324BA7D8CA60EC`；ZIP SHA-256：`ED92E3B4E089495CFEBCDFE313E9282AFBB6FB328764C9D54E38068527736627`。
- 原生能力扫描通过：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.06/native-scan.log`。
- 离线运行回归本次未完成：现有 `test_runtime_flows.ps1` 在本机实际安装的 Unity 程序集上停在 `ECall methods must be packaged into a system module`；没有可用的 0.46D 兼容库。该项作为环境阻塞保留在 `dist/workshop-1.06/runtime-release-qa.log`，不冒充通过。静态检查与原生扫描通过，但 Unity 实机画面/点击验收仍需玩家或作者补测。

## 发布状态

2026-11-15 18:10（北京时间），Steam 对原条目 1.06 内容与英文说明返回 `k_EResultOK`；随后分别更新英文与简体中文说明页，均返回 `k_EResultOK`。Steam Web API 复核：

- 文件大小 10,709,230 bytes，与本地包一致；
- 公开（visibility 0），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-15T10:10:50Z，内容句柄 `8419596321227980892`；
- 远程标题与 manifest 标题一致；默认说明 5,123 bytes，与 `WORKSHOP_DESCRIPTION_1.06.txt` 一致。

工坊说明在作者指定的 1.04 原文基础上，仅按项目规范修正正式路线名称、CutePortraits 已内置的事实，并加入通关后路线重置说明；正文与剧情内容未改。若作者希望恢复其他措辞，可在工坊后台或再次发布时替换 `docs/WORKSHOP_DESCRIPTION_1.06.txt`。

发布证据位于 `dist/workshop-1.06/`：`release-files.json`、`publish-result.json`、`publish-result-schinese.json`、`publish-result-english-description.json`、`publish.log`、`publish-schinese.log`、`publish-english-description.log`、`native-scan.log`、`runtime-release-qa.log`、`remote-after.json`、`final-verification.json`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
