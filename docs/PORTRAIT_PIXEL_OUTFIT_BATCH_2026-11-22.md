# Pixel Outfit Portrait Batch — 2026-11-22

状态：`candidates_pending_review`，未安装到 `Textures/Items`。

规格：288×212 RGBA，B/detail `cp` profile，人物可见高度 150，alpha 仅为 0/255。

## 审查图

- 灰底 3×：`Art/character-assets-2026-11-20/portrait_pixel_outfit_2026-11-22/review_contact_sheet_gray_3x.jpg`
- 白底 1×：`.../review_contact_sheet_white_1x.jpg`
- 黑底 1×：`.../review_contact_sheet_black_1x.jpg`
- 与 workshop 对比：`.../review_workshop_compare.jpg`

## 文件对应

| 运行时文件 | 源图 | 状态/备注 |
|---|---|---|
| `cp_portrait_eleanor1.png` | `Textures\Items\cp_portrait_eleanor1.png` | 与 workshop 同名文件完全一致，可直接替换。 |
| `cp_portrait_eleanor_scav.png` | `dsh-image-gen\image-3fc5e468.png` | 构图与 workshop cp_scav 有差异：当前稿件没有十字镐，IoU 0.8422 / MAE 43.139，先作为候选。 |
| `er_portrait_eleanor_maid_female_beauty.png` | `dsh-image-gen\image-e13c6d72.png` | 像素女仆装，风格与 workshop 接近，待审核。 |
| `er_portrait_eleanor_gala_female_beauty.png` | `dsh-image-gen\image-46044cc5.png` | 像素礼服，待审核。 |
| `er_portrait_eleanor_love_intimate_female_beauty.png` | `dsh-image-gen\image-167a71e6.png` | 像素亲密私服，待审核。 |
| `cp_portrait_eleanor_rev.png` | `dsh-image-gen\image-63ea2ed2.png` | 服装与 workshop cp_rev 不一致（军装夹克 vs 绿色衬衫），IoU 0.9085 / MAE 32.897，不建议直接覆盖 cp_rev。 |
| `er_portrait_eleanor_revolution_undersuit_female_beauty.png` | `dsh-image-gen\image-7264e3f7.png` | 像素战术服，待审核。 |
| `er_portrait_lydia_female_beauty.png` | `dsh-image-gen\image-08cbb283.png` | Lydia 日常，待审核。 |
| `er_portrait_lydia_date_female_beauty.png` | `dsh-image-gen\image-46687350.png` | Lydia 约会，待审核。 |
| `er_portrait_sophia_female_beauty.png` | `dsh-image-gen\image-d826e1dd.png` | Sophia 学者日常，待审核。 |
| `er_portrait_sophia_home_female_beauty.png` | `dsh-image-gen\image-a2b53efc.png` | Sophia 居家 v1，待审核；与 v2 二选一。 |
| `er_portrait_sophia_home_v2_female_beauty.png` | `dsh-image-gen\image-3a2c4f05.png` | Sophia 居家 v2，待审核；比 v1 略小。 |
| `er_portrait_sophia_green_v1_female_beauty.png` | `dsh-image-gen\image-5725ff8f.png` | 额外 Sophia 绿装候选，不属于 11 张主表。 |
| `er_portrait_sophia_green_v2_female_beauty.png` | `dsh-image-gen\image-ab47921e.png` | 额外 Sophia 绿装候选，不属于 11 张主表。 |

## 重点差异

- `cp_portrait_eleanor1` 与 workshop 完全一致。
- `cp_portrait_eleanor_scav` 缺少 workshop 版的十字镐；若该道具是角色识别点，需要补。
- `cp_portrait_eleanor_rev` 服装与 workshop 版不一致；不建议直接覆盖 `cp_portrait_eleanor_rev`。
- `Sophia` 居家有两版：`er_portrait_sophia_home_female_beauty` 与 `..._v2...`，需要二选一。
- `sophia_green_v1/v2` 是额外候选，不进入 11 张主表。

## 未执行

- 没有覆盖 `Textures/Items`。
- 没有生成表情差分；表情应从最终母图派生。