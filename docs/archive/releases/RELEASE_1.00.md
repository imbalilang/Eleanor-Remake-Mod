# 1.00：修复后宫线合作采购场景的拾荒者立绘

2026-11-12。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 问题

玩家反馈后宫线中埃莉诺绝大多数时候仍穿拾荒者衣服。核对后：

- `dr08` 是回上层工作的节点：甲线在办工作通行、先两头跑；乙线做上层交接记录并短住附近。母亲仍留在下层。
- dr11／dr15／dr17／dr19／dr20 与日常原本就使用 `eleanor1` 上层装；dr03 与救母段使用 `eleanor_scav` 拾荒装，符合尚未拿到上层工作的剧情。
- 例外是 `coop`（莉迪亚介绍的采购客，约第 72 天）：它复用了普通线第 21 章的 `CreateCooperationChapter()`，仍固定使用 `eleanor_scav`，与 dr08 之后的剧情冲突。

## 修改

- `CreateCooperationChapter()` 增加可选立绘参数，默认仍为 `eleanor_scav`。
- 后宫线 `DualStory` 的 `coop` 场景显式传入 `eleanor1`；普通线第 21 章不传参，保持原拾荒装。
- 仅改立绘选择，不改剧情、选项、数值、路线、E1—E14 或 0.99 的提示修复。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（zh 2170 键、en 2228 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 1.00，manifest 1.00；18 个时间线节点，E1～E14）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：后宫线 coop 来访显示 `eleanor1`；普通线第 21 章仍显示 `eleanor_scav`。
- 旧存档若已通过 coop，不会重播该场景；新开或未到该节点的存档才能看到新立绘。

## 构建与包

- DLL SHA256：`8EF431D10E2DDE76009718946457788CA6735400C0D15F3ADAD7B528154F1346`。
- 运行包 50 个文件，5,293,892 字节。
- ZIP：`dist/EleanorRomance-1.00.zip`；ZIP SHA256：`540614933425F25D1B6EA302156BA583AAB86C0F4D0DDC1326B4D15C737726CA`。
- 工坊发布：首次尝试即 `k_EResultOK`，内容与说明一并提交，50 个文件，5,293,892 字节；完成时间 2026-11-12 01:40:30（北京时间），耗时 4.54 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,293,892`，`time_updated=1794418830`（2026-11-12 01:40:30 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=8760900775924138144`；远程正文与 `docs/WORKSHOP_DESCRIPTION_1.00.txt` 逐字一致（7,512 字节）。
- 发布回执：`dist/workshop-1.00/publish-result.json`；远程快照：`dist/workshop-1.00/remote-after.json`；暂存目录：`dist/workshop-1.00/EleanorRomance`。
