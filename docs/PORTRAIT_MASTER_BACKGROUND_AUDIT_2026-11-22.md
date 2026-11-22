# Pixel Outfit Master Background Audit — 2026-11-22

标准：纯洋红 `#FF00FF` 背景 + 本地色键；来源 `docs/ART_ASSET_WORKQUEUE_2026-11-19.md`。

## 结论

- 13 张生成母图中，0 张走纯洋红底。
- 9 张走软 alpha 阈值：`alpha_threshold`。
- 4 张走白底键控：`white_key`。
- 只有 `cp_portrait_eleanor1` 是 workshop 目标拷贝，不需要处理。

## 明细

| 输出 key | 背景类型 | alpha_method | alpha=0 比例 | 边界洋红 | 边界白色 | 源图 |
|---|---|---|---:|---:|---:|---|
| `cp_portrait_eleanor1` | `workshop_target_copy` | `copy` | None | None | None | `Textures\Items\cp_portrait_eleanor1.png` |
| `cp_portrait_eleanor_scav` | `soft_alpha` | `alpha_threshold` | 0.5604 | 0.0 | 0.0 | `dsh-image-gen\image-3fc5e468.png` |
| `er_portrait_eleanor_maid_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5479 | 0.0 | 0.0 | `dsh-image-gen\image-e13c6d72.png` |
| `er_portrait_eleanor_gala_female_beauty` | `white_background` | `white_key` | 0.0 | 0.0 | 0.8198 | `dsh-image-gen\image-46044cc5.png` |
| `er_portrait_eleanor_love_intimate_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5388 | 0.0 | 0.0006 | `dsh-image-gen\image-167a71e6.png` |
| `cp_portrait_eleanor_rev` | `soft_alpha` | `alpha_threshold` | 0.5008 | 0.0 | 0.0 | `dsh-image-gen\image-63ea2ed2.png` |
| `er_portrait_eleanor_revolution_undersuit_female_beauty` | `white_background` | `white_key` | 0.0 | 0.0 | 0.8248 | `dsh-image-gen\image-7264e3f7.png` |
| `er_portrait_lydia_female_beauty` | `white_background` | `white_key` | 0.0 | 0.0 | 0.8088 | `dsh-image-gen\image-08cbb283.png` |
| `er_portrait_lydia_date_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5301 | 0.0 | 0.0 | `dsh-image-gen\image-46687350.png` |
| `er_portrait_sophia_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5812 | 0.0 | 0.0002 | `dsh-image-gen\image-d826e1dd.png` |
| `er_portrait_sophia_home_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5443 | 0.0 | 0.0 | `dsh-image-gen\image-a2b53efc.png` |
| `er_portrait_sophia_home_v2_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5729 | 0.0 | 0.0 | `dsh-image-gen\image-3a2c4f05.png` |
| `er_portrait_sophia_green_v1_female_beauty` | `soft_alpha` | `alpha_threshold` | 0.5361 | 0.0 | 0.0 | `dsh-image-gen\image-5725ff8f.png` |
| `er_portrait_sophia_green_v2_female_beauty` | `white_background` | `white_key` | 0.0 | 0.0 | 0.7863 | `dsh-image-gen\image-ab47921e.png` |

## 判定

- `white_background`：`er_portrait_eleanor_gala_female_beauty`、`er_portrait_eleanor_revolution_undersuit_female_beauty`、`er_portrait_lydia_female_beauty`、`er_portrait_sophia_green_v2_female_beauty`。白键只删边界连通白区，头发内部/底边连通的白块会残留。

- `soft_alpha`：其余 9 张生成母图。不是洋红标准，但比白底少一层残留风险；仍需按标准重做或逐张验证。

- 标准化文档存在，但 `tools/build_pixel_outfit_portraits.py` 为兼容旧批次自动回退到 alpha/white key，没有强制拒绝非洋红源。


## 建议

1. 除 `cp_portrait_eleanor1` 外，其余 13 张全部回到纯洋红底重生成。
2. 批处理脚本增加 intake gate：源图边界洋红比例 < 0.5 时直接拒绝，不再自动 white_key。
3. 禁止在 288 运行时图上修补白块。
