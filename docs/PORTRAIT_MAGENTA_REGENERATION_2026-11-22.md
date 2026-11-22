# Magenta Portrait Master Regeneration — 2026-11-22

状态：`masters_pending_review`，尚未 chroma key、尚未导出 288、未安装。

背景：纯洋红底逐张生成；除 `cp_portrait_eleanor1` 为 workshop 288×212 拷贝外，新母图均为 1536×1152。

## 文件

- 母图：`Art/character-assets-2026-11-20/portrait_pixel_magenta_2026-11-22/masters/`
- 洋红总览：`.../magenta_masters_contact_sheet.jpg`
- 头部位置对照：`.../head_position_contact_sheet.jpg`
- 报告：`.../reports/magenta_master_report.json`

## 头部位置校验

| key | 头部偏移 px | 头部 patch MAE |
|---|---:|---:|
| `cp_portrait_eleanor_scav` | [0, -1] | 5.2 |
| `er_portrait_eleanor_maid_female_beauty` | [1, -1] | 6.766 |
| `er_portrait_eleanor_gala_female_beauty` | [0, -1] | 5.565 |
| `er_portrait_eleanor_love_intimate_female_beauty` | [0, -1] | 7.347 |
| `cp_portrait_eleanor_rev` | [0, -1] | 5.825 |
| `er_portrait_eleanor_revolution_undersuit_female_beauty` | [0, -1] | 4.952 |
| `er_portrait_lydia_female_beauty` | [0, 0] | 8.603 |
| `er_portrait_lydia_date_female_beauty` | [0, 0] | 7.554 |
| `er_portrait_sophia_female_beauty` | [0, 0] | 9.16 |
| `er_portrait_sophia_home_female_beauty` | [0, 0] | 6.764 |

结论：Eleanor 的 6 张新母图头部相对基准偏移为 0–1 px；Lydia 与 Sophia 为 0 px。

## 本轮修正

- `cp_portrait_eleanor_scav`：新母图包含十字镐。

- `cp_portrait_eleanor_rev`：改为 workshop 对应的绿色衬衫，不再用军装夹克版本。

- 所有新母图均为洋红底，不再使用白底键控。

- 白底源图导致的头发内部白块，在这批洋红母图中不再存在。

## 下一步

1. 对 10 张新洋红母图做本地 chroma key。

2. 走作者 B/detail `cp` profile，Ht=150，导出 288×212 RGBA。

3. 生成白/黑/灰审查图后再决定是否覆盖 `Textures/Items`。
