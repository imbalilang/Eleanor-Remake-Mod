# 0.97：后宫线声望要求下调为 30／60／90

2026-11-12。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 问题

玩家反馈主线快通关时上层区声望只有约 80，第三阶段 150 过高，后宫线在通关前走不完。

## 修改

- `DualProgression.Targets` 从 `{50,100,150}` 下调为 `{30,60,90}`。
- 双恋入口选项说明、路线图三个检查点节点、README、剧本总览和阅读稿同步改为 30／60／90。
- 检查逻辑不变：只在对应前置完成后检查当时声望；达标一次即锁存，后续下降不撤销；不消耗声望，也没有付费替代。
- 奖励、等待日常、目标日、路线、选项与 E1—E14 不变。
- 已有存档：尚未通过的检查点会按新阈值重新判定；已通过或已领取的奖励不回退。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（dual 81 块／13 选项；zh 2170 键、en 2228 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 0.97，manifest 0.97；18 个时间线节点，E1～E14；检查点参数与图一致）。
- 双恋回归脚本 `tools/test_dual_routes.ps1` 的边界值同步改为 29／30、59／60、89／90（本轮未执行完整运行时回归）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：30／60／90 三阶段在实际存档中的激活、等待提示和奖励领取。

## 构建与包

- DLL SHA256：`FCD1C5051084110BECA190B71F695B31DA5B476038BCDA7FB5F7EB1FB77731F3`。
- 运行包 50 个文件，5,291,273 字节。
- ZIP：`dist/EleanorRomance-0.97.zip`；ZIP SHA256：`C50505421437E5692BA3DEA79B4689412B9067A77DFDA4E0E23DECC665CEF4CE`。
- 工坊发布：首次尝试即 `k_EResultOK`，内容与说明一并提交，50 个文件，5,291,273 字节；完成时间 2026-11-12 00:56:55（北京时间），耗时 4.57 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,291,273`，`time_updated=1794416215`（2026-11-12 00:56:55 北京时间），`visibility=0`（公开），`banned=0`，`hcontent_file=4399798529739229733`；远程正文与 `docs/WORKSHOP_DESCRIPTION_0.97.txt` 逐字一致（7,525 字节）。
- 发布回执：`dist/workshop-0.97/publish-result.json`；远程快照：`dist/workshop-0.97/remote-after.json`；暂存目录：`dist/workshop-0.97/EleanorRomance`。
