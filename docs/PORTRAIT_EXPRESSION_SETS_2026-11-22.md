# L1 / L2 Expression Sets — 2026-11-22

## Final scope
- L1 standard (9 each): `eleanor1`, `eleanor_scav`, `eleanor_gala`, `eleanor_rev`, `lydia_daily`
- L1 special (10): `eleanor_maid` — includes `tsun`
- L2 (6 each): `eleanor_revolution_undersuit`, `lydia_date`, `sophia`, `sophia_home`

Standard L1 expressions: `angry, sad, serious, determined, hurt, shy, smile, soft, tired`
Special L1 addition: `tsun` only on `eleanor_maid`
L2 expressions: `serious, smile, hurt, shy, soft, tired`

## Output
`Art/character-assets-2026-11-20/portrait_expression_sets_2026-11-22/`

- `L1/` — 55 files (5 bases × 9 + maid × 10)
- `L2/` — 24 files (4 bases × 6)
- `L1_expression_set_contact.jpg` — `tsun` row red-boxed as a special case
- `L2_expression_set_contact.jpg`
- `expression_set_report.json`
- `SPECIAL_CASES.md`
- `archive/removed_tsun/` — five discarded `tsun` candidates

## Special case
`eleanor_maid / tsun` is retained because it has 2 actual uses:

- `Content/maid_v05.json:1889` (`m19_b001`)
- `Content/maid_v05.json:1968` (`m19_b006`)

No other L1 base uses `tsun`. The runtime fallback is `tsun` → `shy` (`src/PortraitExpression.cs:62-64`), but the maid asset is kept for the exact authored expression.

## Validation
- 79 files total, all 288×212 RGBA, alpha values only 0/255.
- Alpha IoU against each neutral base is 1.0 for all 79 files.
- All five non-maid `tsun` candidates were removed from the active set.
- Existing legacy `tease`/`flustered` files for `lydia_daily` remain in runtime but are not part of the L1 candidate package.

## Status
`candidates_not_installed`. No files under `Textures/Items` were overwritten.
