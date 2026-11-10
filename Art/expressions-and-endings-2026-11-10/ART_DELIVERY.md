# Eleanor 表情与侍从结局 CG

后续修复：17张Eleanor表情已加入基础像下颌/颈部保护，解决拾荒温柔表情的下巴突起。当前资源与重导出规则见[修复记录](../expression-chin-fix-2026-11-10/ART_DELIVERY.md)。以下保留首次交付范围与来源。

2026-11-10。按用户指令生成并替换本轮范围内的占位资源；生成使用内置 `image_gen.imagegen`。

## 已交付

- 基础上层装表情 5 张：angry、sad、serious、shy、soft。
- 拾荒装表情 6 张：angry、determined、sad、shy、soft、tired。
- 女仆装表情 6 张：angry、sad、serious、shy、soft、tsun。
- 侍从结局 CG 1 张：她在关店后的熟悉当铺主动陪伴玩家，延续现有结局事实；960×540 完整不透明场景。

17 张表情全部为 288×212 RGBA，实际文件名沿用 `er_expr_...`，已替换 `Textures/Items` 对应 PNG。CG 沿用 `er_ending_maid_cg.png`，不改剧情、选项、资源键、运行时切换逻辑或结局判定。

## 制作与验证

母图位于 `masters/`，实际提示词位于 `prompts/` 和 `generation_jobs.json`。正常高分辨率生图后，使用原作者已验证 B/detail 精确采样；面部图层按虹膜位置等比对齐，装配到同族中性像，固定身体、头发外轮廓与透明度。一个暖色虹膜的自动检测结果已人工校正，缩放范围另有检查。

17 张 alpha 与对应中性像逐像素相同，面部区域外可见 RGB 保持相同。已检查原生尺寸总览，愤怒、难过、认真、害羞、温柔、疲惫、坚定与傲娇按实际绘制区分。

离线导出和运行替换通过 `python tools/export_expression_assets.py --install` 复现。`asset_validation.json` 记录每张母图、底图、输出 SHA256、实际排布及面部定位。已核对 18 个安装后文件哈希；项目内容/中英资源键检查通过。

替换后审计在 `audit-after/`：36 个运行 PNG，9 张基础画像、22 张表情、2 张 CG、3 张物品；此前 Eleanor 11 张错误风格及 211 高度的差分已替换，17 张 Eleanor 均与当前美化底图轮廓一致。原先占位文件保存在 `before/` 便于回退。

## 用户明确暂缓

Lydia 5 张表情与后宫结局 CG 均未重新生成、未替换；6 个原文件哈希保持不变。待用户确认 Lydia 新形象后再制作。后宫 CG 继续是占位，不能写成已完成。

## 预览

- `previews/expression-contact-sheet.png`：三套中性像与全部差分的原生尺寸对照。
- `previews/eleanor1-2x.png`、`previews/eleanor_scav-2x.png`、`previews/eleanor_maid-2x.png`：整数放大。
- `endings/er_ending_maid_cg.png`：侍从 CG 游戏尺寸。

离线检查通过；实际对话切换与不同画幅的结局 UI 显示仍需实机验收。美术规范和复用模板见上级目录。

## 发布

已随0.85发布至原创意工坊条目3834588749，Steam返回k_EResultOK。构建0警告0错误，原生能力扫描通过。详见[0.85发布记录](../../docs/RELEASE_0.85.md)。
