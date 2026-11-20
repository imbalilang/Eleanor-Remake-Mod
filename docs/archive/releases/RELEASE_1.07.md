# 1.07 热修复发布记录（2026-11-15）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。本版本只修两处旧侍从/侦探款入口的卡住问题，不改正文、结局编号、路线条件或排期。

## 修复

- 旧侍从入线场景（资源 c11 / 运行第 12 章）的第一个选项“你都安排好了。去找侦探吧。”原来依赖挂在选择回复行上的收尾动作；原生选择流程从选项直接跳到 Next，空 Next 导致该收尾动作不执行，第 12 章不提交，顾客也可能不离场。现在该选项有实际显示的玩家回复行，完成动作挂在该行上，选择后正常提交第 12 章并推进。
- v2 侦探费选择（“差多少我补 / 按货收，不多管”）在不垫付或交易未结算时，第 12 章会等待 `Savings>=800` 或第 42 天；同时 `MaidV2Story.CreateChapter(12)` 无条件返回付费共事场景，绕过共同线 C11 收尾。现在只要玩家已经回答侦探费选择，第 12 章立即进入 `MaidV2Story.CreateEntry`：付清则进入共事场景，未付则播放共同线 C11 侦探款收尾并推进，不再空等。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：1.07 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 发布包 153 个文件、10,711,324 bytes；ZIP `dist/EleanorRomance-1.07.zip`。DLL SHA-256：`7656EEDAA5CE140D1D43E21A5003D1E53B415AF33C0F5FD3A6A4D60D0945ECB5`；ZIP SHA-256：`3B0EC744C9E8F183C87F1A00C0F4222B0E3C41A94F5E916AB4CEA77891932929`。
- 原生能力扫描通过：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.07/native-scan.log`。
- 新增回归断言：旧侍从第一个选项必须有可显示的回复行且能推进；v2 不垫付选择在 day 31 不得再被第 12 章门槛挡住，并应播放共同 C11 收尾。测试位于 `tools/test_rewritten_routes.ps1` 与 `tools/test_release_qa.ps1`。本机完整离线运行回归仍被实际安装 Unity 程序集的 `ECall methods must be packaged into a system module` 环境错误阻塞，因此这些新断言未在本机执行；静态检查和原生扫描通过，实机仍需补测这条旧档路径。

## 发布状态

2026-11-15 18:56（北京时间），Steam 对原条目 1.07 内容与英文说明返回 `k_EResultOK`；随后分别更新英文与简体中文说明页，均返回 `k_EResultOK`。Steam Web API 复核：

- 文件大小 10,711,324 bytes，与本地包一致；
- 公开（visibility 0），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-15T10:56:16Z，内容句柄 `633574824219725358`；
- 远程说明 5,123 bytes，与 `WORKSHOP_DESCRIPTION_1.07.txt` 一致。

发布证据位于 `dist/workshop-1.07/`：`release-files.json`、`publish-result.json`、`publish-result-schinese.json`、`publish-result-english-description.json`、`publish.log`、`publish-schinese.log`、`publish-english-description.log`、`native-scan.log`、`remote-after.json`、`final-verification.json`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
