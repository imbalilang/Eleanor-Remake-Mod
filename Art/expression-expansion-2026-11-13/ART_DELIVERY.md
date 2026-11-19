# 埃莉诺通用表情第一批

2026-11-14。围绕早期主线及常用拾荒装完成 21 张差分候选，文件名与游戏资源键一致。

| 立绘 | 新增差分 | 预览 |
| --- | --- | --- |
| `eleanor2` | serious、smile、soft、sad、hurt、angry、shy、determined、tired | [原生尺寸](previews/eleanor2-native.png) · [面部 4 倍](previews/eleanor2-faces-4x.png) |
| `eleanor3` | serious、smile、soft、sad、hurt、angry、shy、determined、tired | [原生尺寸](previews/eleanor3-native.png) · [面部 4 倍](previews/eleanor3-faces-4x.png) |
| `eleanor_scav` | serious、smile、hurt | [原生尺寸](previews/eleanor_scav-native.png) · [面部 4 倍](previews/eleanor_scav-faces-4x.png) |

每张母图由内置生图从该服装的实际基础像制作。以人工估读的可见内外眼角为锚点逐眼校正，嘴部独立定位。局部采样使用项目 B/detail 的面积采样和细节处理，合成限定在眼眉与嘴周的内部区域。保留基础像的透明度、下颌、颈部、头发外轮廓、身体和衣服。已查看原生尺寸与最近邻 4 倍对照。部分克制表情在原生画幅上的区别主要在嘴角与眼睑，仍需实机切换验收。

交付 PNG 在 `portraits/`，分辨率 288×212，RGBA。母图和实际提示词分别在 `masters/`、`prompts/`。本批贴图已和第二批一起写入项目 `Textures/Items/`，见[接入记录](../expression-expansion-2026-11-14/ART_DELIVERY.md)。

[参考来源与哈希](references.json) · [人工标点](landmarks.json) · [导出与哈希校验](asset_validation.json) · [复现导出](export_faces.py)
