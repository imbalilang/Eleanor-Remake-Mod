# 埃莉诺常服微笑与不悦修复

- 微笑沿用原差分使用的高清来源 `expression-expansion-2026-11-13/masters/er_expr_eleanor2_smile.png`（1138×1382）。不悦沿用 `expressions-and-endings-2026-11-10/masters/er_expr_eleanor1_angry.png`（1462×1076）。两张原稿都是半身像。
- 局部重绘输入为高清原图裁切，裁切坐标与文件哈希见 `asset_validation.json`。内置生图的实际提示词见 `prompts/*-hd.txt`。`masters/*-half-body.png` 将眉眼、嘴部的局部重绘合回对应的完整母图。最初以运行贴图放大裁切所做的试稿留作记录。
- 游戏贴图使用 B/detail 采样和局部坐标装配。微笑直接沿用同一张中性立绘的双眉、双眼，嘴部使用高清重绘。另一侧眉毛保留刘海下的自然遮挡。不悦只合入眉形与抿唇，双眼沿用同一张中性立绘。导出独立核对双眼、鼻部和下颌下方区域的 RGB，以及全图 alpha。
- 检视了原生尺寸、2 倍半身与 4 倍脸部对照。工作区替换沿用现有 `smile` 和 `angry` 资源键。实机对白切换待验收。

作者后来选定 `masters/angry.png` 的不悦表情。当前 `angry` 以这张选定图的眉眼与嘴部为准，按中性立绘的原裁切坐标做面积采样并装配到完整半身运行贴图。`masters/eleanor1-angry-half-body.png` 为此前高清候选，当前运行来源以校验记录中的 `runtime_source` 为准。
