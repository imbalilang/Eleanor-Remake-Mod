# 全身换装管线

日期：2026-11-20

## 基准身体

- Eleanor：`Art/character-assets-2026-11-20/design_sheets/final_L1_outfits/eleanor_L1_casual_fullbody.png`
- Lydia：`Art/character-assets-2026-11-20/design_sheets/final_L1_outfits/lydia_L1_daily_fullbody.png`

这两张已包含通过的少女化身体、脸、发型、A-pose 和浅灰背景。

## 换装流程

1. 选一张对应角色的 L1 全套基准图作为 image 1。
2. 新服装图作为 image 2。
3. 只替换衣服：保留脸、发型、身体比例、A-pose、镜头和背景。
4. 新服装必须完整、非暴露、非私密，适合对应层级。
5. 先 OpenAI；同一套图一旦被明确拒绝，不再重试 OpenAI，直接走混元。
6. 输出保存到 `design_sheets/fullbody_outfits/<character>/<outfit>.png`。

## 锁定项

- Eleanor：158/47，90-81-58-89，32D，7.0 头，裆高 82。
- Lydia：160/50，97-82-59-91，32F，7.0 头，裆高 83。
- 不重新设计身体，不重新画头，不让模型自行改身高或腿长。
## 清晰度门槛：每张图必须通过 `tools/check_repaint_sharpness.py`；FAIL_BLUR 必须从源参考重画，不得继续局部修补。
