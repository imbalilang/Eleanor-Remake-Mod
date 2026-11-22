# Eleanor Expression Benchmark (2026-11-22)

Canonical benchmark: `Art/character-assets-2026-11-20/eleanor_expression_benchmark_2026-11-22/Facial_benchmark.png`

This is the current canonical facial-expression reference for Eleanor. It is an expression-only benchmark; outfit/body must still come from each target portrait.

## Coverage vs L1

L1 requires ten differentials:
`angry, sad, serious, determined, hurt, shy, smile, soft, tired, tsun`.

Facial has nine cells:
- smile
- serious
- angry
- sad
- hurt
- determined
- tired
- tsun
- soft (the unchanged soft/neutral cell)

Missing for L1: **shy**.

Neutral base portrait is separate and is not counted as a differential.

## Source candidates for the missing shy cell

- `Art/expressions-and-endings-2026-11-10/masters/er_expr_eleanor_maid_shy.png` (1461x1076)
- `Art/expression-expansion-2026-11-13/masters/er_expr_eleanor2_shy.png` (1138x1382)
- `Textures/Items/er_expr_eleanor_maid_shy.png` (288x212 runtime)

The 1461x1076 maid shy master is the closest existing source, but it predates this benchmark and should be reviewed before being added.

## Status

Benchmark recorded; Facial is not yet a complete L1 set because shy is missing. Do not treat the old 288 runtime expression set as the benchmark.
