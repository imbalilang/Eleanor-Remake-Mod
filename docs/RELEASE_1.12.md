# 1.12 发布记录（2026-11-16）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 后宫线手镯资格改为按买入／卖出／回购／赠送事件计数，不再依赖库存格或实例 ID。
- 修复免费赠送后手镯未取走、后宫资格未写入、最终进入单人恋爱线的问题。
- 旧档首次载入后扫描一次：`DPreludeState=1` 按实际持有数恢复；`DPreludeState=7` 且同一手镯仍在、尚未进入普通恋爱/告白时修复为已赠送；已进入普通恋爱或结婚的旧档不回改。
- 1.11 立绘选择、1.10 voice pass 与更早的入线、点击区、立绘兼容修复保留。
- 不改正文、路线结构、结局编号或排期；manifest 升至 1.12，路线图与站点同步 1.12。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：1.12 与 manifest 一致、5 个现行结局、20 个分幕、站点副本一致。
- 发布包 139 个文件、13,257,940 bytes；ZIP `dist/EleanorRomance-1.12.zip`。DLL SHA-256：`BD7F61E221E0E4196A5C80DFF32A41AA8EAE70F7F4C3386AC84F34C7D80F75C5`；ZIP SHA-256：`A8EAC28EB0B2738FFFFEFE4E2C1529D93DBFEE28E085E9451566B1DAAB395D2F`。
- 本机缺少 PowerShell 7，运行时回归和原生能力扫描无法执行；扫描脚本在 PowerShell 5.1 下因 `MissingMethodException` 退出，这是环境错误，不是能力发现。manifest 未新增 capabilities。

## 发布状态

2026-11-16 09:37（UTC），以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`。标题、简介和内容分级字段未改动。

Steam Web API 复核：

- 标题 `Eleanor Story Expand`；
- 远程说明 4,246 bytes，SHA-256 `94DAFD60363B34D2BD988D1D7CEBD8E86398E831BCB88C4CAEDD202D189086F7`（与 1.11 一致）；
- 文件大小 13,257,940 bytes，与本地包一致；
- 公开（visibility 0），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-16T09:37:25Z，内容句柄 `4615341275213657467`。

发布证据位于 `dist/workshop-1.12/`：`release-files.json`、`publish-result.json`、`publish.log`、`remote-after.json`、`final-verification.json`。更新说明为 `docs/WORKSHOP_CHANGELOG_1.12.txt`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
