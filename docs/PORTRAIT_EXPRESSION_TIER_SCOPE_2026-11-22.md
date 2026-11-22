# Portrait expression scope by asset tier (2026-11-22)

Source of truth: `Art/character-assets-2026-11-20/character_asset_registry.json` → `tier_definitions`, `workload`, and per-family `tier_label`.

## Tier definitions
| Tier | Expression variants | Notes |
|---|---:|---|
| L1_full_expression | 10 | Full expression set |
| L2_reduced_expression | 6 | Reduced expression set |
| L3_portrait_only | 0 | Portrait only |
| L4_design_only | 0 | Design only |
| L5_design_pending_integration | 0 | Integration deferred |

## Current base portraits
### L1 — 7 sets, full expressions
- `eleanor1`
- `eleanor_scav`
- `eleanor_maid`
- `eleanor_gala`
- `eleanor_love_intimate`
- `eleanor_rev`
- `eleanor_revolution_undersuit`

### L2 — 4 sets, reduced expressions
- `lydia_daily`
- `lydia_date`
- `sophia`
- `sophia_home`

### L3 — 4 sets, no expressions
- `eleanor_revolution_date`
- `eleanor_black_mesh_halter`
- `eleanor_black_sports_hoodie`
- `sophia_white_halter_knit_variant`

### L4 — 5 known sets, no expressions
- `eleanor_bunny`
- `eleanor_white_sleepwear`
- `lydia_rose_swimsuit`
- `lydia_sleepwear_harem`
- `eleanor_sleepwear_harem`

### L5 — 1 set, no expressions
- `eleanor_nun`

## Correction to the expression plan
The earlier plan treated all 11 base portraits as needing a full 9–11 expression set. That is wrong.

- All 11 current bases are L1/L2, so all still need some expressions.
- L1 must stay full-set (target 10 variants).
- L2 must be capped at 6 variants; do not carry over the existing 9–11 Lydia/Sophia sets.
- L3/L4/L5 must not receive expression work.

Current runtime counts are inconsistent with the tier targets:
- L1: most bases have 9 expressions, maid has 10; target is 10.
- L2: Lydia daily/date have 11 each, Sophia has 9 (plus `home_*` duplicates), Sophia home has 9; target is 6.

## Next step
Before batch generation, derive the actual L2 six-expression list from story usage / `ExpressionContent`, rather than reusing all current files. For L1, confirm whether the target tenth variant is a shared expression or the maid-specific `tsun`.
