# Grok Large-CG Style Acceptance — 2026-11-21

## User decision

`dsh-image-gen/image-f0c62f02.jpg` is approved for the two-bunny CG as a Grok large-CG 4/5 variant.

For future large-scale CGs generated with Grok Imagine Image 2.0 (`x-ai/grok-imagine-image-2.0`) through content-master restyle, exact reproduction of the v1 pixel-style profile is not required. A native generation may be accepted at **4/5 metrics**, provided the single failing metric is not extreme.

## 4/5 acceptance rule

Required:

- at least 4 of the 5 `check_pixel_style.py` metrics are inside the v1 band
- exactly one metric is outside the band
- the failing metric is within **40% relative deviation** from its nearest band boundary
- the image is visually acceptable as a CG, with no T0 anatomy failure
- the generation is a content-master restyle, not a postprocessed metric pass

Not eligible:

- 3/5 or lower
- two or more failed metrics
- a single metric more than 40% outside its nearest boundary
- postprocessed metric passes produced by downscale/blur

## Applied case

`image-f0c62f02.jpg`:

- `hf_ratio 0.0499` PASS
- `flat_ratio 0.238` PASS
- `palette_5bit 2066` FAIL, maximum 2009, relative deviation 2.84%
- `edge_density 0.1499` PASS
- `smooth_fraction 0.575` PASS
- Verdict: 4/5, accepted under `GROK_LARGE_CG_4OF5_ACCEPTED`

## Not eligible comparison

- `image-def7f9b4.jpg`: 3/5 (`flat_ratio`, `palette_5bit` fail); not eligible.
- `image-4b96b9f0.jpg`: 4/5 but `palette_5bit 2128` and visible rose; visually not accepted.
- `image-6fb98a7c.jpg`: 3/5; not eligible.

## Important distinction

This is an acceptance policy for Grok large CGs, not a redefinition of the v1 pixel-style standard. Small assets and native style-standard candidates still require 5/5.


## Single style reference requirement

Grok large-CG restyles must use exactly one pixel-style reference image. Multiple style references are not allowed because they pollute CG content (accessories, clothing, composition). Content/pose references are separate from style references.
