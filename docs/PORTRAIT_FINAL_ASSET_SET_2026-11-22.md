# Final Portrait Master + Runtime Candidate Set — 2026-11-22

## 状态

- 母图已整合：4 张作者 QQ 原图 + 8 张新的洋红底生成母图。
- 已生成 12 个 288×212 RGBA 游戏内候选（11 个基础立绘 + `cp_portrait_eleanor3` 表情差分）。
- 尚未覆盖 `Textures/Items`；等待审查后安装。

## 母图来源

### 作者 QQ 原图（直接作为母图）

| 键 | 来源 | 说明 |
|---|---|---|
| `cp_portrait_eleanor1` | `Art/原图参考/QQ图片20261119194822.png` | 默认紫披肩 |
| `cp_portrait_eleanor_scav` | `Art/原图参考/QQ图片20261119200041.png` | 拾荒装 + 十字镐 |
| `cp_portrait_eleanor_rev` | `Art/原图参考/QQ图片20261119200057.png` | 绿色衬衫革命装 |
| `cp_portrait_eleanor3` | `Art/原图参考/QQ图片20261119200052.png` | 默认紫披肩·低落表情差分 |

### 新生成洋红母图

- maid
- gala
- love intimate
- revolution undersuit
- Lydia daily
- Lydia date
- Sophia daily
- Sophia home

母图目录：

`Art/character-assets-2026-11-20/portrait_pixel_magenta_2026-11-22/final_masters/`

## 游戏内候选

目录：

`Art/character-assets-2026-11-20/portrait_pixel_magenta_2026-11-22/runtime_candidates/`

全部：

- 288×212
- RGBA
- alpha 仅 0/255
- bbox 顶边 62，可见高度 150
- 颜色数 6,422–9,908

## 与 workshop 旧图对比

| 键 | IoU | MAE | 结论 |
|---|---:|---:|---|
| `cp_portrait_eleanor1` | 1.0000 | 0.000 | 逐像素一致 |
| `cp_portrait_eleanor_scav` | 1.0000 | 0.000 | 平移 -7px 后逐像素一致 |
| `cp_portrait_eleanor3` | 1.0000 | 0.000 | 逐像素一致 |
| `cp_portrait_eleanor_rev` | 0.9850 | 8.891 | 接近，非逐像素一致 |

其余 8 个 beauty 状态是新的像素母图，不再与旧平滑图逐像素比较。

## 审查图

- `.../final_review_gray.jpg`
- `.../final_review_white.jpg`
- `.../final_review_black.jpg`
- `.../reports/final_runtime_report.json`

## 头部位置

面部落点相对基准母图的偏移：

- Eleanor 各服装：0–1 px
- Lydia daily/date：0 px
- Sophia daily/home：0 px

## 安装规则

审查通过后，按同名复制到 `Textures/Items/`。未通过前不覆盖任何现有资源。
