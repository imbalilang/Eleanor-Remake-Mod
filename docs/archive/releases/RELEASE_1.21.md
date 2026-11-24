# 1.21 发布记录（2026-11-19）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。

## 变更

- 韩语版埃莉诺与莉迪亚改为自然的半语（반말）：去掉通篇 `-요` / `-습니다` / `-세요` 等敬语结尾；对母亲、索菲娅、军警以及引号内他人台词保留必要敬语。
- 韩语版埃莉诺的译名统一为 **엘리너**（替换旧的 `엘레노어`）。
- 日语版检查后未发现同样的敬语过量问题：埃莉诺/莉迪亚主要以普通体口语为主，礼貌语集中在索菲娅、母亲、假叔叔和军警等符合角色定位的说话人；未修改 `Localization/ja.csv`。
- 不改变正文、路线、选项、主线目标日、排期或结局；1.20 日韩本地化及更早修复保留。

## 验证

- Release 编译：0 警告、0 错误。
- `python tools/check_project.py`：通过。
- 韩语占位符与英文键比对：0 处不一致。
- 对 1.0.0.15 游戏程序集原生扫描：0 capabilities、0 findings、0 pending、0 hard flags。证据：`dist/workshop-1.21/native-scan.log`。
- `python tools/check_route_map.py`：**本次未更新 `docs/route_map.html`，manifest 为 1.21、DATA.version 仍为 1.20，版本一致性检查预期失败**；路线图内容未改，记录为待处理。
- **2026-11-20 补记：GitHub Pages 已停用，公开攻略迁移到 Steam 指南 3842734960；上面的外放站点记录仅保留为 1.21 发布当时的状态。**
- 发布包：141 个文件，20,621,730 bytes；ZIP `dist/EleanorRomance-1.21.zip`，SHA-256 `E568AB92D2DD1CB36EC7781134F8FC2C72D07B174131BA60DD641AC09BD3C6EB`；DLL SHA-256 `C209B39E22A6A385CACE9270B2D7E50410C670E361519DD5CB0CE47B37006AA4`。

## 发布状态

- 2026-11-19 12:09:01（UTC）以 **content-only** 模式更新原条目，Steam 返回 `k_EResultOK`，耗时 4.53 秒；标题、简介和内容描述符未改动。
- Steam Web API 复核：标题 `Eleanor Story Expand`；英文默认简介 4,359 bytes，SHA-256 `95054250763CB3B4ECBF949811DB21D6C71C3ABA91B1430AA86859E2EF371C5A`（与 1.20 一致）；文件大小 20,621,730 bytes，与本地包一致；公开（visibility 0），未封禁。
- 条目 ID `3834588749`，所有者 `76561198109201343`，更新时间 `2026-11-19T12:09:01Z`，内容句柄 `5680078699224766953`。
- 证据：`dist/workshop-1.21/release-files.json`、`publish-result.json`、`publish.log`、`native-scan.log`、`remote-after.json`。
- 未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
