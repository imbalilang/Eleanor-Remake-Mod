# 11 张基础立绘像素化批次

日期：2026-11-21

## 范围

处理现有 `portrait_regeneration_2026-11-20` 的 11 张基础候选：

- eleanor1
- eleanor_scav
- eleanor_maid
- eleanor_gala
- eleanor_love_intimate
- eleanor_rev
- eleanor_revolution_undersuit
- lydia_daily
- lydia_date
- sophia
- sophia_home

排除：

- 4 张 vanilla fallback：已废弃
- `cp_portrait_eleanor2`
- `cp_portrait_eleanor3`：原作表情差分，少女当铺已完成像素化

## 流程

1. 源图：`Art/character-assets-2026-11-20/portrait_regeneration_2026-11-20/transparent_masters/*_transparent_master.png`
2. `tools/build_bust_portrait_asset.py`：`--profile cp --no-magenta-key`，输出 288×212 基线立绘。
3. `tools/pixelize_portrait_batch.py`：在 288×212 基线上执行：
   - alpha 阈值 128，硬化为 0/255
   - MedianCut 量化到 128 色
   - 无 dither
   - 不改变构图、尺寸、bbox
4. 生成 4× 最近邻预览和对比图。

## 输出

- 基线：`Art/character-assets-2026-11-20/portrait_pixelization_2026-11-21/batch_11/baseline_288/`
- 像素母版：`.../batch_11/pixel_masters_288/`
- 预览：`.../batch_11/previews/`
- 总览：`.../batch_11/batch_11_pixel_contact_sheet.jpg`
- 前后对比：`.../batch_11/batch_11_before_after_2x.jpg`
- 报告：`.../batch_11/reports/batch_11_pixelization_report.json`

## 状态

`candidates_not_installed`

尚未覆盖 `Textures/Items`，也尚未派生表情差分。先审 11 张基础候选。

## 已知取舍

- 128 色会压平部分渐变，但脸、发型和服装结构保持不变。
- 如果某张脸部色阶不足，可单独提升到 256 色重跑。
- 当前只做基础立绘；表情差分必须从批准的像素母版派生。
