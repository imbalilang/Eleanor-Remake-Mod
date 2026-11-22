# Portrait Expression Pilot — Full-frame method (2026-11-22)

## Why the face-layer composite was abandoned
The first 9-expression `cp_portrait_eleanor1` pilot used a local face-layer composite. Even after:
- tight eye/mouth masks,
- jaw guard,
- donor alignment search (3–4 px),
- skin-tone mean matching,
- feathering,

a visible vertical seam remained on the cheek/chin. Poisson blending was tried and produced black artifacts. Local compositing is not a viable production path for these 288×212 portraits.

## Current method
Generate the **whole portrait** from the author QQ pixel master, changing only the expression, then export through the canonical B/detail pipeline:

`Art/原图参考/QQ图片20261119194822.png` (exact cp1 author master)
→ OpenAI whole-image expression edit at 1536×1152, pure magenta background
→ `tools/build_bust_portrait_asset.py --profile cp`
→ 288×212 RGBA.

No face-layer mask, no skin blending, no seam.

## Pilot outputs
`Art/character-assets-2026-11-20/portrait_expression_pilot_2026-11-22/eleanor1/fullportrait/`

9 expressions: angry, sad, serious, determined, hurt, shy, smile, soft, tired.

All are 288×212 RGBA with alpha only 0/255. Alpha IoU vs `cp_portrait_eleanor1` is 0.936–0.960; RGB MAE 24.7–28.7. The expression changes are coherent, but the whole-frame redraw does slightly alter hair outline and body silhouette.

Contact sheet: `eleanor1_fullportrait_contact_2x.jpg`
Report: `fullportrait_report.json`

## Status
Candidates only. Not installed into `Textures/Items`.
Need user visual review before scaling to the remaining bases.

## Trade-off
- No seams, because there is no local composite.
- Whole-frame redraw cannot guarantee pixel-identical body/hair; alpha IoU is ~0.95, not 1.0.
- If exact body lock is mandatory, the only robust alternative is hand-authored feature patches per outfit/expression, which is much more labor-intensive.
