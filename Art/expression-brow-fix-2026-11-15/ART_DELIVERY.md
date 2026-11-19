# 表情画像重眉修复

检查范围为 `Textures/Items` 的 13 套立绘、117 张通用表情和 5 张旧标签贴图。逐张比较中性底图与差分的双眉，疑似重影再对照已配准的母图和导出遮罩。13 张画像确认有旧眉残留，均在画面左侧。

| 立绘 | 修复的表情键 |
| --- | --- |
| 埃莉诺拾荒装 `eleanor_scav` | `determined`、`angry` |
| 埃莉诺基础装 `eleanor1` | `angry`、`serious` |
| 埃莉诺女仆装 `eleanor_maid` | `angry`、`serious`、`tsun` |
| 莉迪亚日常装 `lydia_daily` | `angry`、`determined`、`sad` |
| 莉迪亚约会装 `lydia_date` | `angry`、`determined`、`sad` |

埃莉诺这批表情沿用 B/detail 采样的已配准母图及既有虹膜锚点。莉迪亚这批差分沿用第二批表情的高清母图和[人工眼角标点](../expression-expansion-2026-11-14/landmarks.json)，逐眼作相似变换。两种导出器共用[原生眉部遮罩](../../tools/expression_face_bounds.py)，按衣装和已核定的表情补足旧眉与新眉之间的可见皮肤。羽化限于遮罩内部。

对照了修复前后的原生像素和最近邻放大图，并在白、黑及 `#252C35` 背景检查拾荒装「坚定」。逐像素确认 13 张修正版仅在各自眉区发生改动，透明度与各自底图完全一致。眼睛、嘴、下颌和其他区域与修复前逐像素一致。其余 109 张差分的文件哈希一致。

[拾荒装「坚定」修复前后](before-after.png) · [母图、底图、贴图哈希和像素差异](validation.json)

修正版已写入项目贴图目录。本次为离线验收，实机切换有待验证。
