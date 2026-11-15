# 1.09 发布记录（2026-11-15）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。本版本只调整立绘兼容策略；不改正文、结局编号、路线条件或排期。

## 变更

- 立绘改为合作式：原版埃莉诺调用和本 MOD 自己的剧情使用本 MOD 立绘；其他 MOD 的客户、立绘键和贴图完全不动。
- 本 MOD 自己的剧情客户改用私有 `cp_portrait_eleanor*` 键，旧存档中的本 MOD 客户在原版键上加载时会迁移；只替换原版 `eleanor1/2/3/scav/rev` 的美化 MOD 不再影响我们的剧情客户。
- 若其他 MOD 覆盖了当前可见立绘，本 MOD 让位，不强制换回；同时在对白层暂停表情切换，避免“先显示对方立绘、再切到我们的表情、再切回”的闪烁。
- 移除 1.08 的强制接管和 2 秒逐帧守卫；1.07/1.08 的入线卡住、物品点击区、手镯重查等修复保留。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：1.09 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 发布包 153 个文件、10,706,617 bytes；ZIP `dist/EleanorRomance-1.09.zip`。DLL SHA-256：`13D13987461A853ACEE77506D0967FEE8DDEC4ADE74C31AAEEDA126D58CAE2A1`；ZIP SHA-256：`6ADFB76C592A90BF91DD7E610F45B498D5A3CC626506EDB85EA1E29E1A41DF7A`。
- 原生能力扫描通过：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.09/native-scan.log`。
- 跨 MOD 立绘让位、私有键与表情暂停属于 Unity 实机显示行为，离线扫描不能替代画面验收；完整离线运行回归仍被实际安装 Unity 程序集的 `ECall methods must be packaged into a system module` 环境错误阻塞。

## 发布状态

2026-11-15 19:36（北京时间），Steam 对原条目 1.09 内容与英文说明返回 `k_EResultOK`；随后分别更新英文与简体中文说明页，均返回 `k_EResultOK`。Steam Web API 复核：

- 文件大小 10,706,617 bytes，与本地包一致；
- 公开（visibility 0），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-15T11:35:57Z，内容句柄 `501254791571860462`；
- 远程说明 5,369 bytes，与 `WORKSHOP_DESCRIPTION_1.09.txt` 一致。

发布证据位于 `dist/workshop-1.09/`：`release-files.json`、`publish-result.json`、`publish-result-schinese.json`、`publish-result-english-description.json`、`publish.log`、`publish-schinese.log`、`publish-english-description.log`、`native-scan.log`、`remote-after.json`、`final-verification.json`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
