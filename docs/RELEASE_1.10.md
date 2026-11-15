# 1.10 发布记录（2026-11-15）

原创意工坊条目：[3834588749](https://steamcommunity.com/sharedfiles/filedetails/?id=3834588749)。本版本为文本语气与人物卡更新；不改结局编号、路线条件、交易、选择或排期。

## 变更

- 角色卡补强：莉迪亚加入公开/私下两套语域、钱与时间边界、照顾型与“自己的时间”；索菲娅加入长期超负荷、玩心、面对权力的具体反应。
- 路线 ActCard 加入【声音参考】：后宫线（莉迪亚/埃莉诺）、恋爱线（埃莉诺/索菲娅）、革命军线（疲惫指挥官/战术短句/责任者）。
- 女仆线：晏若初式 非露骨 语料与女仆期人设；act 2–4 已按 1:1 voice pass 重写。
- 后宫线 `dv01`–`dv13`：13 章全量 voice pass，P0/P1=0；莉迪亚的表演性同情、保护事件善后、2000/黑幕/停下道歉、三人各自同意等节点保留。
- 恋爱线 `love_a1_01`–`love_a3_04`：10 章全量 voice pass，P0/P1=0；索菲娅不撮合、不替女儿决定；婚礼保留 E1。
- 革命军线 `v2_r1c1`–`v2_r4c4`：11 章全量 voice pass，P0=0；疲惫/战术/责任者语气贯穿；最后一句“我好想你”保留；源文已有的 CI-010 14–16 仍为已知结构限制。
- 术语/空间修正：秤→柜台角、硬币→信用点语境、通行证/通行→许可证、`我妈`→`我母亲`；r4c4 原有 P0 已清。
- 未改路线图结构、结局编号、交易/选择/存档逻辑；未执行本地 `Mods/` 部署。

## 验证

- Release 编译 0 警告、0 错误。
- `python tools/build_content.py`、`python tools/check_project.py` 通过：中英无缺键、无无效块引用。
- `python tools/check_route_map.py` 通过：manifest 1.10 与路线图一致、5 个现行结局、站点副本一致。
- 发布包 153 个文件、10,709,538 bytes；ZIP `dist/EleanorRomance-1.10.zip`。DLL SHA-256：`D0517FD819150F161DDCA7B68E83E728CBE43BBCC751EB056520410109EC62CA`；ZIP SHA-256：`1B7A990BE41475599B6829207ACB5D5D37003E592D6B6ABA34AF359352DEB01F`。
- 原生能力扫描：0 capabilities、0 findings、0 pending、0 hard flags；证据 `dist/workshop-1.10/native-scan.log`。
- 实机运行回归仍受离线 Unity 程序集环境限制；立绘/界面显示需在游戏内验收。

## 发布状态

2026-11-15 20:20（北京时间），Steam 对原条目 1.10 内容与英文说明返回 `k_EResultOK`；随后分别更新简体中文与英文说明页，均返回 `k_EResultOK`。Steam Web API 复核：

- 文件大小 10,709,538 bytes，与本地包一致；
- 公开（Public），未封禁；
- 条目 ID 3834588749，所有者 76561198109201343；
- 更新时间 2026-11-15T12:20:14Z；
- 描述 5,744 bytes，SHA-256 `A7C2B598396374BB96E75F1A1BAD4AB713D3AF2B9E0759027E3DD3F1DE787A24`；

发布证据位于 `dist/workshop-1.10/`：`release-files.json`、`publish-result.json`、`publish-result-schinese.json`、`publish-result-english-description.json`、`publish.log`、`publish-schinese.log`、`publish-english-description.log`、`native-scan.log`、`remote-after.json`、`final-verification.json`。

未执行本地 `Mods/` 部署；本地安装目录与存档未被修改。
