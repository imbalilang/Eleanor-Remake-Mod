# L2 expression set audit (2026-11-22)

## Actual packaged assets

Source: `dist/workshop-1.28/release-files.json` (latest release manifest).

- `er_expr_lydia_daily_*`: 11
- `er_expr_lydia_date_*`: 11
- `er_expr_sophia_*`: 9
- `er_expr_sophia_home_*`: 9
- Total `er_expr_*`: 104

There is no six-file L2 set in the latest package. The latest package is overbuilt relative to the L2 target.

The only historical six-variant L2 package is the old Lydia set (`workshop-1.01` / `nexus-0.86`): five expression files plus the neutral base portrait:
`flustered`, `hurt`, `serious`, `smile`, `tease`.

## Actual story usage

Counted from `Content/*.json` `expr` fields, by speaker:

| Expression | Lydia | Sophia | Combined |
|---|---:|---:|---:|
| serious | 44 | 14 | 58 |
| smile | 36 | 7 | 43 |
| hurt | 26 | 0 | 26 |
| shy | 12 | 0 | 12 |
| soft | 0 | 7 | 7 |
| tired | 2 | 6 | 8 |
| tease | 3 | 0 | 3 |
| flustered | 1 | 0 | 1 |
| determined | 2 | 0 | 2 |
| angry | 1 | 0 | 1 |

Neutral is the base portrait and is not counted as an expression differential.

## Proposed L2 six-expression set

Use the same six differentials for all four L2 bases (`lydia_daily`, `lydia_date`, `sophia`, `sophia_home`):

1. `serious`
2. `smile`
3. `hurt`
4. `shy`
5. `soft`
6. `tired`

Reason: these are the six most-used non-neutral expressions across the two L2 characters. `tease` and `flustered` are excluded because they are nearly unused in current content. `tired` is retained because Sophia uses it six times.

## L1 tenth slot

The current L1 sets mostly have 9 differentials; maid has 10 including `tsun`. The natural unified tenth slot is `tsun`, added to the other six L1 sets so all seven L1 bases share the same 10 differentials.

## Status

Proposal only. Do not trim or install runtime expression assets until the user confirms the six names.
