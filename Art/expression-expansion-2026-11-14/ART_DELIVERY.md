# 通用表情第二批

索菲娅常服与私服的 18 张运行差分已随新底图尺寸重新导出。[尺寸调整与运行贴图校验](../sophia-scale-2026-11-14/ART_DELIVERY.md)

2026-11-14。本批导出 74 张 288×212 RGBA PNG，包含 72 张新合成差分和 2 张沿用莉迪亚 `flustered` 图像的通用 `shy` 贴图。两批合计新增 95 张贴图。加上已有通用键的 22 张贴图，以下 10 套和[第一批的 3 套](../expression-expansion-2026-11-13/ART_DELIVERY.md)各有 9 种通用差分；`neutral` 使用各自底图。

| 画像资源族 | 本批差分 | 原生尺寸对照 |
| --- | --- | --- |
| 埃莉诺基础装 `eleanor1` | 微笑、受伤、坚定、疲惫 | [预览](previews/eleanor1-native.png) |
| 埃莉诺女仆装 `eleanor_maid` | 微笑、受伤、坚定、疲惫 | [预览](previews/eleanor_maid-native.png) |
| 莉迪亚日常 `lydia_daily` | 温柔、失落、恼怒、羞怯、坚定、疲惫 | [预览](previews/lydia_daily-native.png) |
| 莉迪亚约会 `lydia_date` | 同日常 | [预览](previews/lydia_date-native.png) |
| 埃莉诺革命军 `eleanor_rev` | 全 9 种 | [预览](previews/eleanor_rev-native.png) |
| 埃莉诺晚会 `eleanor_gala` | 全 9 种 | [预览](previews/eleanor_gala-native.png) |
| 埃莉诺恋爱私服 `eleanor_love_intimate` | 全 9 种 | [预览](previews/eleanor_love_intimate-native.png) |
| 埃莉诺战术背心 `eleanor_revolution_undersuit` | 全 9 种 | [预览](previews/eleanor_revolution_undersuit-native.png) |
| 索菲娅常服 `sophia` | 全 9 种 | [预览](previews/sophia-native.png) |
| 索菲娅针织私服 `sophia_home` | 全 9 种 | [预览](previews/sophia_home-native.png) |

本批使用内置生图制作莉迪亚的 5 张新表情母图和索菲娅的 9 张表情母图。埃莉诺沿用第一批同角色的表情母图。每套衣装按各自底图估读眼角和嘴的位置，再分别合成眼眉和嘴部。原生尺寸和 4 倍最近邻对照均已检查。克制的认真、坚定与温柔在原生尺寸上的差异较细，仍需实机切换验收。

覆盖核对确认 117 个通用差分键均有对应 PNG，规格与各自底图的透明度一致。旧女仆装差分的合成边界与本批不同，y≥136 的区域各有 5 个不透明像素与底图不同。本批沿用这些旧图。

两批新贴图已写入项目 `Textures/Items/`，交付母本保留在 `portraits/`。运行时按当前顾客的画像键和对白表情查图。旧 `flustered`、`tease`、`tsun` 优先使用原贴图，缺图时尝试同套衣装的通用差分。独立的索菲娅居家服与埃莉诺恋爱私服已经配置画像键，对应场景仍待接入。

Release 编译、中英文本、路线图、13 套画像键和 117 张通用贴图检查通过。未部署到本机游戏，尚未实机确认。全路线离线回归在旧版与本次编译产物上均停在第 103 章的讲价用例。

[本批母图提示词](prompts/) · [第一批埃莉诺提示词](../expression-expansion-2026-11-13/prompts/) · [参考来源与哈希](references.json) · [人工标点](landmarks.json) · [导出校验](asset_validation.json) · [复现导出](export_faces.py)
