# 0.94：莉迪亚在双恋主线中实际购买奢侈品

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 问题

莉迪亚在当铺的戏份几乎都是纯 `DIALOGUE`：进入双恋线后，dr 场景只推进剧情，没有任何买卖；原本第 22 章的 500 预算采购又会被 `SuppressMainChapter(19-24)` 跳过。玩家实际只在第 10 章买过一次手链，且不是必成交。

## 修改

- dr04、dr07、dr09、dr10 在原有对白后追加“顺路看货”短段，然后由莉迪亚以 `BUY` 来访购买奢侈品／艺术品／高档品，预算 500，走原生开价、议价与成交流程。
- 双恋结局后的日常第 2 段（莉迪亚）改为真实买入，预算 300；其余六段日常不变。
- 莉迪亚只买不卖；成交或离店后按实际是否成交播放不同收尾（`er_dual_trade_close_bought` / `er_dual_trade_close_none`）。
- 新增 `FinishAfterTrade()`：对白结束后不立即关闭客户端，而是让原生交易开始；场景在她离店时提交，避免读档或中途退出跳过交易部分。
- 对白、选项、关系与路线节点不变；E1—E14 与完美集合不变。

## 新增文本

- `dr04_shop`、`dr07_shop`、`dr09_shop`、`dr10_shop`、`dr_daily_2_shop` 五段短对白，中英同步。
- 收尾句按成交情况二选一；不引用具体金额或物品，避免和实际交易结果冲突。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（dual 80 块／13 选项；zh 2166 键、en 2224 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 0.94，manifest 0.94；18 个时间线节点，E1～E14）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：四个场景的对白→交易→离店收尾流程、无合适货物时的未成交收尾、双恋日常第 2 段、读档续接。
- 旧存档：已经完成这些场景的存档不会重播新增交易；新开或尚未走到这些节点的存档会使用新流程。

## 构建与包

- DLL SHA256：`CCC1A61C32F8A24593013CC7EBECC50129A6D05A4AA51D4A21A93BC0884BCB34`。
- 运行包 50 个文件，5,286,917 字节。
- ZIP：`dist/EleanorRomance-0.94.zip`；ZIP SHA256：`F7C152F4D7A13D9444C368B5AFBD38333DD3934C6A414D40689F078425D2631F`。
- 工坊发布：首次尝试即 `k_EResultOK`，内容与说明一并提交，50 个文件，5,286,917 字节；完成时间 2026-11-11 23:23:26（北京时间），耗时 5.07 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,286,917`，`time_updated=1794410606`（2026-11-11 23:23:26 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=5020927790723368562`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.94.txt` 逐字一致（7,573 字节）。
- 发布回执：`dist/workshop-0.94/publish-result.json`；远程快照：`dist/workshop-0.94/remote-after.json`；暂存目录：`dist/workshop-0.94/EleanorRomance`。
