# CG Reference Audit 2026-11-22

Status: draft binding for `cg_pixel_contract_v1`

## Scene references

| File | Type | Use |
|---|---|---|
| `Art/场景参考/场景参考1.png` | pawnshop / sales-booth interior | primary scene mother for interior booth scenes |
| `Art/场景参考/场景参考2.png` | roofed lower-district commercial alley | primary scene mother for dense commercial/exterior scenes |
| `Art/场景参考/场景参考3.png` | ration store / grocery interior | primary scene mother for shop aisle scenes |
| `Art/场景参考/场景参考4.png` | scrapyard / industrial lower deck | primary scene mother for industrial/scrap scenes |
| `Art/场景参考/CG参考1.png` | underground hideout ambience | mood/lighting reference only; not a general architecture template |
| `Art/场景参考/SCENE_STYLE_ANALYSIS.md` | analysis | authoritative scene grammar notes |

Key scene facts: 960×540 native plates, 64–192 colors, hard 1–2 px lines, closed sky/roof, practical lights, no AA or smooth gradients.

Glass-hall date scene has no exact scene mother. Use `场景参考1` for interior structure and `场景参考2` for commercial density/color; do not copy either layout. Keep a roofed/glass-enclosed upper structure with no open sky.

## Character references

| File | Role |
|---|---|
| `Art/原图参考/QQ图片20261119194822.png` | default Eleanor; global pixel-character style mother |
| `Art/原图参考/QQ图片20261119200041.png` | scav Eleanor; outfit/style reference |
| `Art/原图参考/QQ图片20261119200052.png` | default Eleanor sad differential; style reference |
| `Art/原图参考/QQ图片20261119200057.png` | green-shirt revolution Eleanor; outfit/style reference |
| `Art/原图参考/其他角色1.png` | other character; render grammar only, no identity transfer |
| `Art/原图参考/其他角色2.png` | other character; render grammar only, no identity transfer |
| `Art/原图参考/STYLE_ANALYSIS.md` | authoritative character grammar notes |

Character grammar: 245×178 logical, 6× nearest, 3–5 shading steps, warm dark outline, broad hair locks, large layered eyes, readable accessories.

Identity must come from the specific character portrait/master, not from a style reference. `其他角色1/2` must not dictate Lydia/Sophia/Eleanor identity.

## Binding

- One primary scene mother per scene type.
- One global character style mother: `QQ图片20261119194822.png`.
- Identity portrait per character/CG.
- No multiple scene mothers or multiple character style mothers in one production run.
- No generation until the 3-content repeatability test is defined and approved.
