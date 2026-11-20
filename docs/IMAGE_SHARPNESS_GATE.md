# 全局图像清晰度门槛

日期：2026-11-20
工具：`tools/check_repaint_sharpness.py`

## 指标

图像先统一缩放到最长边 1024 px，再转灰度，计算：

- Laplacian variance：`lap_var`
- Sobel gradient mean：`sobel_mean`

## 通过阈值

- `lap_var >= 270`
- `sobel_mean >= 3.4`

两者同时满足才算 `PASS`，否则为 `FAIL_BLUR`。

## 强制规则

1. 每张图生成后、进入审核或归档前，必须跑一次清晰度检测。
2. `FAIL_BLUR` 的图不得继续局部修补或反复重绘。
3. 失败后必须从原始参考图/干净身体基准重新生成。
4. 同一张图最多允许一次局部修正；修正后再失败，直接从头重画。
5. 报告里必须记录检测指标和最终 verdict。
