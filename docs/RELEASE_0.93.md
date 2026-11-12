# 0.93：修复朋友线结局后的空来访循环

2026-11-11。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 问题

玩家反馈：拯救母亲并完成朋友线核心结局后，埃莉诺反复出现

- “这回的货我分好了。不好的放在一起，别说我没提醒你。”
- “我现在也算老手了。第一次拾荒那种事，你可别再提。”

随后直接离店，没有任何货物可买。

根因：`StoryDirector.TryQueueEpilogue` 对朋友线按 `EpilogueIndex` 最多排队 5 次；`EleanorStory.CreateEpilogueClient` 对朋友线始终使用 `ep.b005`，并且客户端是纯 `DIALOGUE`，对白结束时调用 `StoryEngine.Dismiss()` 主动离场。三件事叠加，形成“重复同一段对白、不卖货”的循环。

## 修复

- 朋友线后日谈改为真实 `SELL` 来访；进入场景时调用 `StoreClientList.AddScavGeneralStock()` 生成一批搜获货物，按正常开价、议价和收购流程结算，不白送钱或好感。
- 首次来访仍播放原 `ep.b005` 朋友段；后续来访改用新增的短交易问候 `er_ep_friend_trade`，不再原样重播整段对白。
- 新增 `RomanceState.EpilogueStockIndex`，同一次来访读档重进不会重复生成货物；重置新游戏时归位。
- 后日谈排期保持原设计：最多 5 次、至少间隔 8 天。恋爱线四段日常不变；路线、数值与 E1—E14 编号不变。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（zh 2154 键、en 2212 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 0.93，manifest 0.93；18 个时间线节点，E1～E14）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：朋友线第 1 次后日谈的货物生成、后续短交易问候、买货/议价/离店、读档防重复。
- 另：本机 0.92 曾因安装目录内残留 `EleanorRomance.dll.pre-sophia-20261111-214239.bak` 被扫描器拒绝（`payload-file -> executable content`）；该文件已移至 `dist/mother_portraits_local_20261111-214206/removed_from_mod_scan/`，0.92 远程包本身不含该文件。

## 构建与包

- DLL SHA256：`4188D8BED0C786AD466BE3AEAF7E54E4327F76CA8D9B7C32F1579942AF509BA9`。
- 运行包 50 个文件，5,281,515 字节。
- ZIP：`dist/EleanorRomance-0.93.zip`；ZIP SHA256：`E074A176608FD8788D7C9356A9F25CECA242DC0F5814ED8A5FADDBDD762BD8F1`。
- 工坊发布：首次尝试即 `k_EResultOK`，内容与说明一并提交，50 个文件，5,281,515 字节；完成时间 2026-11-11 22:27:59（北京时间），耗时 5.03 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,281,515`，`time_updated=1794407279`（2026-11-11 22:27:59 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=379049024324058370`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.93.txt` 逐字一致（7,365 字节）。
- 发布回执：`dist/workshop-0.93/publish-result.json`；远程快照：`dist/workshop-0.93/remote-after.json`；暂存目录：`dist/workshop-0.93/EleanorRomance`。
