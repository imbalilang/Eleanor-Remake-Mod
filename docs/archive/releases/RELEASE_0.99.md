# 0.99：修复后宫线误弹“完整恋爱线已达成”

2026-11-12。状态：**已构建、已打包、已通过 Steam 预检；尚未上传创意工坊**。

## 问题

后宫线在 dr11 完成核心时调用共享的 `StoryDirector.CompleteCore()`。该方法检测到母亲安全、普通恋爱已确认且路线为甲／乙，就会弹出普通线的 `er_notice_true_unlock`：“完整恋爱线已达成。”

但后宫线此时还有 dr13—dr20 未播放，真正的结局页在 dr20 才出现，所以这条提示会让玩家误以为剧情已经结束。

## 修改

- `CompleteCore()` 增加 `dualOwnsEnding` 判断：当双恋扩展仍有历史、仍处于 Active 且持有结局时，不再显示 `er_notice_true_unlock`。
- 后宫线原有的 dr20 结局页不变；meta 解锁、声望、奖励、路线、选项与 E1—E14 均不变。
- 普通恋爱线、朋友线、侍从线等其他路径仍按原逻辑显示该提示。

## 验证

- Release 构建：0 警告、0 错误。
- `tools/check_project.py`：通过（zh 2170 键、en 2228 键；无缺键、无非法块引用）。
- `tools/check_route_map.py`：通过（route map 0.99，manifest 0.99；18 个时间线节点，E1～E14）。
- Steam 发布预检（`publish_workshop.ps1 -CheckOnly`）：通过。
- 实机仍需验证：后宫线 dr11 核心完成时不再弹该提示，dr20 结局页仍正常出现；普通恋爱线仍正常解锁。

## 构建与包

- DLL SHA256：`A7C8C0837CACC8B27A4F60E83A8A40281A12E73D2B968B86C3128AF20E3D5563`。
- 运行包 50 个文件，5,292,201 字节。
- ZIP：`dist/EleanorRomance-0.99.zip`；ZIP SHA256：`A6FE0B31B0B2212BEE7B2FBC8E253AED6FEDE81C966DA3CF185E8B7964C82EB3`。
- 暂存目录：`dist/workshop-0.99/EleanorRomance`；待发布。
