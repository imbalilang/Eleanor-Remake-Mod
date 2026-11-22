# CG Repeatability Test Plan v1

Status: planned, not executed  
Contract: `docs/CG_STYLE_CONTRACT.md` / `docs/cg_style_contract.json`

## Goal

Prove that the fixed prompt/model recipe can convert three different content masters into the same target pixel style without changing content.

## Test contents

1. `Textures/Items/er_event_harem_lydia_date_cg.png` — complex glass-hall date scene
2. `Textures/Items/er_event_love_water_cut_cg.png` — water/close scene
3. `Textures/Items/er_event_maid_gala_cg.png` — maid gala interior

## Fixed inputs

- scene mother: one per scene type (see `docs/CG_REFERENCE_AUDIT_2026-11-22.md`)
- character identity: the relevant character portrait master
- character style mother: `Art/原图参考/QQ图片20261119194822.png`
- prompt: the fixed block in `docs/CG_PROMPT_TEMPLATE.md`
- provider/channel: `openai-compat` / `openrouter`
- model: `openai/gpt-image-2.6-flare`
- generation size: `2048x1152`
- quality: `high`
- reference order: content → identity → scene mother → character style mother

## Allowed run size

- exactly one generation per content
- no reroll, no alternates, no batch until all three are reviewed
- no `Textures/Items` changes

## QA per image

Content first:
- composition/camera preserved
- character count and identity preserved
- pose, clothing silhouette, key props preserved
- scene layout preserved

Style second:
- hard pixel edges and consistent pixel grid
- 3–5 shading steps
- no AA/gradients/photographic texture/checkerboard
- belongs to the scene + character reference families

Verdicts: `PASS` / `FAIL` / `UNCERTAIN`.

## Stop conditions

- any content-lock failure → `FAIL`
- style clearly outside target family → `FAIL`
- cannot decide → `UNCERTAIN`
- only `PASS` on all three unlocks batch production
