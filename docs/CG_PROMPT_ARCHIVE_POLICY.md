# CG 提示词留档规则

日期：2026-11-20

## 规则

每一张通过 CG 必须同时具备：

1. 完整正向提示词文本
2. 使用的模型 ID、channel、size、aspect_ratio、quality 等参数
3. 参考图清单
4. 通过母图与游戏用导出图哈希
5. 提示词文件路径写入 APPROVED.json

没有提示词档案的图不得标记为 approved。

## 目录

- `docs/cg_prompt_archive/2026-11-20/`
- 中央清单：`docs/cg_prompt_archive/2026-11-20/manifest.json`

## 当前补档

- 白花睡服 B
- 侍从兔女郎
- Lydia 白兔床姿
- Lydia Grok 最终版
- 已撤销的双人兔女郎 v7 也保留提示词供追溯

## 说明

现有提示词为从会话记录重建，标记为 `reconstructed_from_transcript`。后续新增通过图必须在审批当时保存原始提示词，不再做事后重建。
