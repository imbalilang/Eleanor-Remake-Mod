# 1.03：修复第6章求食援助分支

2026-11-12。状态：**已构建、已打包、已发布到原创意工坊条目 3834588749**；实机验收待完成。

## 问题

玩家反馈：第6章（`c05`）求食来访中，只给一小块肉（或以为是免费赠送）时，埃莉诺播放“我懂了。你开的是店，不是救济站。”；给 160～200+ 价值的食物才道谢离开。同时她身上显示约 68 信用点，而她此时应该身无分文。

## 根因

- 本章客户使用 `SetBudget(40)`；原生 `StoreClient.SetBudget` 会调用 `ApplyBudgetModifier()`，按店铺吸引力、特性与背书追加预算——玩家看到的 68 由此而来。
- 因为她有预算，赠食被结算成正常购买：她按协商价付款，玩家反而收到信用点；`onItemBought` 的 `paid` 超过 `market/3` 的援助阈值，`HardshipAidGiven` 未置位，于是走了拒绝分支。
- 原版 Eleanor4（同一困境来访）使用 `SetBudget(0)`，任何被收下的食物都计为援助。

## 修改

- `CreateHardshipChapter()` 的预算改为 `SetBudget(0)`，与原版 Eleanor4 一致：她不能用钱购买，食物只能被赠送。
- 移除按支付比例判定援助的逻辑；本章任何一次成交都直接置 `HardshipAidGiven`（她的购买标签仍只接受 FOOD／TREAT／BREVAGE）。
- 只有完全没有成交时才播放拒绝分支；给现金的原生选项与现金对白保持不变。
- `c05.b002` 的标签由“接受零价或合理援助交易后”改为“接受食物援助后”；正文不变。
- 不改章节顺序、选项结果、声望、奖励、路线或 E1—E14。包含此前未发布的 1.01、1.02 全部修改。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（zh 2215 键、en 2275 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 1.03，manifest 1.03；18 个时间线节点，E1～E14）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：`c05` 给任意食物进入道谢分支；什么都不给进入拒绝分支；客户预算显示 0，不再出现 68。
- 本地原生 `scan_package.ps1` 在 Windows PowerShell 5.1 下因扫描器运行库缺少 `String.Split(Char, StringSplitOptions)` 无法执行；非包内容问题，发布脚本的文件哈希与未跟踪文件校验均已通过。

## 构建与包

- DLL SHA256：`F32F8BC532DA7C8852BF49BC23A558AB52395BD9D546A7F298DEBBAFEA58DE41`。
- 运行包 50 个文件，5,307,492 字节。
- ZIP：`dist/EleanorRomance-1.03.zip`；ZIP SHA256：`7A840E885560D169E667A1D769C2ABB5634830537B581B606DA5C9A74D64990A`。
- 工坊发布：首次尝试即 `k_EResultOK`，内容与说明一并提交，50 个文件，5,307,492 字节；完成时间 2026-11-12 03:40:38 UTC（北京时间 11:40:38），耗时 5.07 秒。
- 远程复核（Steam GetPublishedFileDetails）：`result=1`，`file_size=5,307,492`，`time_updated=1794454837`（2026-11-12 03:40:37 UTC，北京时间 11:40:37），`visibility=0`（公开），`banned=0`，`hcontent_file=5794227354784153761`；远程说明以 1.03 段开头。
- 发布回执：`dist/workshop-1.03/publish-result.json`；暂存目录：`dist/workshop-1.03/EleanorRomance`。

## 版本关系

- 上一次工坊版本为 1.00；1.01、1.02 未单独上传，已全部并入 1.03。
- 1.03 同时包含 1.02 的后期回应选择与路线修复、1.01 的上层顾客饰品判定修复，以及一轮对白与留白整理。
