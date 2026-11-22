# L1 `tsun` Decision — 2026-11-22

## Final decision
`tsun` is kept only for `eleanor_maid`.

Usage audit:

- Total: 2 occurrences
- `Content/maid_v05.json:1889` (`m19_b001`)
- `Content/maid_v05.json:1968` (`m19_b006`)
- Speaker: Eleanor
- `src/MaidStory.cs:359` calls `Eleanor("m19", true)`, so both resolve to `eleanor_maid`
- `eleanor1`, `eleanor_scav`, `eleanor_gala`, `eleanor_rev`, `lydia_daily`: 0 uses

## What changed
- The five newly generated `tsun` candidates for `eleanor1`, `eleanor_scav`, `eleanor_gala`, `eleanor_rev`, `lydia_daily` were removed from the L1 set and archived under:
  `Art/character-assets-2026-11-20/portrait_expression_sets_2026-11-22/archive/removed_tsun/`
- Only `Art/character-assets-2026-11-20/portrait_expression_sets_2026-11-22/L1/eleanor_maid/er_expr_eleanor_maid_tsun.png` is retained.
- Special annotation:
  - `.../L1/eleanor_maid/er_expr_eleanor_maid_tsun.SPECIAL.md`
  - `.../SPECIAL_CASES.md`
  - red-boxed row in `L1_expression_set_contact.jpg`

## Historical donor work
Full-frame and feature-composite donor experiments remain under:
`Art/character-assets-2026-11-20/portrait_expression_l1_tsun_2026-11-22/`
They are not part of the active L1 candidate set.

## Status
Candidates only; no `Textures/Items` changes.
