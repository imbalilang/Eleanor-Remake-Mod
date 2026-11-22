# Eleanor Raw Expression Normalization — 2026-11-22

## Scope

- 40 raw expression donors for `gala`, `scav`, `rev`, `maid`.
- 10 expressions each: smile, serious, angry, sad, hurt, determined, tired, tsun, shy, soft.
- Source raw directory: `Art/character-assets-2026-11-20/portrait_expression_raw_eleanor_2026-11-22/raw/`.
- Normalized directory: `Art/character-assets-2026-11-20/portrait_expression_raw_eleanor_2026-11-22/normalized_1536/`.

## Normalization

- Canvas: `1536×1152`.
- Background: pure `#FF00FF`.
- Character bbox: height `1139–1140 px`, top `12–13 px`, horizontally centered.
- Transparent/white-background inputs were keyed/cleaned; small isolated alpha specks below 5,000 px were removed.
- `scav/tsun`, `scav/shy`, and `rev/soft` were regenerated from the 1536 outfit masters before normalization.
- All filenames preserved.

## Artifacts

- `normalized_1536/normalized_manifest.json`
- `normalized_1536/normalization_report.json`
- `normalized_1536/normalized_{gala,scav,rev,maid}_contact.jpg`
- Raw diagnostic: `raw_batch_diagnostic.json`

## Status

`normalized_raw_candidates_not_installed`. No 288×212 runtime export and no `Textures/Items` changes yet.

## Caveat

`maid` raw sources were originally generated from 288-base input; they were upscaled to the common bbox for this normalization pass. They are acceptable as face-donor raws but should be regenerated from the 1536 maid master if maximum detail is required.
