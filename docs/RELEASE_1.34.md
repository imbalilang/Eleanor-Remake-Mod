# 1.34 发布记录（2026-11-24）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 修复第 5 章假叔叔警告场景重复让同一顾客离场：原版 `OnDismiss` 回调里再次调用 `DismissCurrentClient`，导致下一次叫号动画锁多计一次；柜台无人却一直显示“现在无法呼叫下一位顾客”。所有警告分支改为只提交一次章节进度，残留锁在读档或次晨开店时清除。
- 合并创意工坊 1.33 的源码与内容：CG/路线结局页按游戏当前语言取字体；立绘清理保留其他模组提供的 `cp_portrait_*` 键；旧侍从 m12–m28、旧革命军 R01–R14、旧互动/关系选择及死文本从构建和本地化中移除，只保留 v2 复用的 6 个侍从共享块。
- 本地化与 1.33 发布包逐 key 一致：zh 4577、en 4821；ja/ko 采用发布文件，共享 key 值未变，仅移除退役内容。
- 不改当前路线、剧情、选项结果、目标日、排期或结局；保留 1.32 卢式晨间闭店约会、1.31 韩语校对、1.30 后宫 v2 香水覆盖及更早修复。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- `python tools/check_route_map.py`：通过，manifest 与 `DATA.version` 均为 1.34。
- 生成内容与 1.33 DLL 对齐：Story 861、Maid 16、MaidV2 706、Dual 1347、RevV2 478、Love 471、Expression 658 个 key，无缺失、无多余。
- 本机完整运行时回归仍受 Unity `ECall` 环境限制，未执行；本次改动集中在第 5 章警告离场路径、载入恢复和 1.33 已发布内容的合并。
- 原生能力扫描（PowerShell 7）：1573 个方法，0 capabilities、0 hard flags、0 findings、0 pending、0 deprecated。
- 发布包：116 个文件，18,965,993 bytes；ZIP `dist/EleanorRomance-1.34.zip`，SHA-256 `FC05B4B403BC2547A0D2C318F1600C81F88C88A5B4FA18C9637BB0BDD507A6EF`；DLL SHA-256 `03699BFD4992C39EB71D4219D6845CFEB30E25FD2F422FE99C51E777442E2655`。
- 贴图使用 1.33 发布包中的 107 张既有素材；本地 6 张未发布的新表情/立绘未带入本版。

## 发布状态

- 2026-11-23T17:51:47Z（UTC）以 **content-only** 模式更新原条目，`-KeepDescription`，Steam 返回 `k_EResultOK`，耗时 4.18 秒；标题与创意工坊简介未修改。
- Steam Web API 复核：标题 `Eleanor Story Expand`；文件大小 18,965,993 bytes，与本地包一致；公开（visibility 0），未封禁；简介 SHA-256 `88FFA20908973A0AE7BF9CFB3BC5D17D1658E27B54168C41C462FF4550C6A27C`，与 1.32 一致。
- 证据：`dist/workshop-1.34/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
- GitHub 同步：`release/1.34` 合并提交与本文档推送 `origin/main`；源码、Content、Localization、README、manifest 与路线图进入仓库，`dist/` 发布证据和未发布素材不纳入。
