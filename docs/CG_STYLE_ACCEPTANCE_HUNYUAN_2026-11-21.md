# Hunyuan Large-CG Style Acceptance — 2026-11-21

## User decision

Use `Art/character-assets-2026-11-20/cg/harem_two_bunny_bed/approved/harem_two_bunny_bed_mirror_camera_cg_master_approved.png` as the final two-bunny CG for this scene.

For future large-scale CGs generated with Tencent Hunyuan (`tencent/hy-image-v3.5-preview`), exact reproduction of the v1 pixel-style profile is not required. A native generation may be accepted at **4/5 metrics**, provided the single failing metric is not extreme.

## 4/5 acceptance rule

Required:

- at least 4 of the 5 `check_pixel_style.py` metrics are inside the v1 band
- exactly one metric is outside the band
- the failing metric is within **40% relative deviation** from its nearest band boundary
- the image is visually acceptable as a CG, with no T0 anatomy failure

Not eligible:

- 3/5 or lower
- two or more failed metrics
- a single metric more than 40% outside its nearest boundary
- postprocessed metric passes produced by downscale/blur

## Applied case

`harem_two_bunny_bed_mirror_camera_cg_master_approved.png`:

- `hf_ratio 0.0586` PASS
- `flat_ratio 0.129` FAIL, minimum 0.205, relative deviation 37.1%
- `palette_5bit 1559` PASS
- `edge_density 0.1383` PASS
- `smooth_fraction 0.657` PASS
- Verdict: 4/5, accepted under `HUNYUAN_LARGE_CG_4OF5_ACCEPTED`

## Rejected comparison cases

- `image-552cf34f.png`: 3/5 (`hf_ratio`, `palette_5bit` fail); not eligible.
- `image-3622c1bc.png`: 2/5 (`flat_ratio`, `palette_5bit`, `edge_density` fail); not eligible.

## Important distinction

This is an acceptance policy for Hunyuan large CGs, not a redefinition of the v1 pixel-style standard. Small assets and native style-standard candidates still require 5/5.
