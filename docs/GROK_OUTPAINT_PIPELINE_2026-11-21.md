# Grok Outpaint Then Style Pipeline - 2026-11-21

## Problem

`image-a1d27d60.jpg` is 1024x1024. Directly restyling it to 2048x1152 caused the model to fit the square content into a wide canvas, compressing the figures vertically.

## Tested pipeline

1. Build `grok_outpaint_test/outpaint_base_16x9.png`: 2048x1152 canvas, original square centered at 1152x1152, side panels extended from bed edges.
2. Outpaint only the left/right panels with Grok, preserving the central figures: `dsh-image-gen/image-df743170.jpg`.
3. Restyle the outpainted image with the P5 pixel pipeline: `dsh-image-gen/image-e27684d0.jpg`.

## Result

- Outpaint step preserved body height and added horizontal bed area.
- Style step passed `check_pixel_style.py` 5/5:
  - hf_ratio 0.0558
  - flat_ratio 0.294
  - palette_5bit 1593
  - edge_density 0.1621
  - smooth_fraction 0.626
- Direct restyle control `image-6fb98a7c.jpg` failed 3/5: flat 0.175, smooth 0.519.

## Conclusion

Prompt wording alone is not reliable for preventing geometric squash. Build the 16:9 canvas explicitly, outpaint only the sides, then restyle. This separates geometry from style.

## Files

- `Art/character-assets-2026-11-20/cg/harem_two_bunny_bed/grok_outpaint_test/outpaint_base_16x9.png`
- `dsh-image-gen/image-df743170.jpg`
- `dsh-image-gen/image-e27684d0.jpg`
- `Art/character-assets-2026-11-20/cg/harem_two_bunny_bed/grok_outpaint_test/pipeline_report_2026-11-21.json`


## Style reference rule

Step 3 uses exactly one pixel-style anchor. Do not add multiple style references: they pollute content and accessories. See `docs/GROK_STYLE_TEST_LOG_2026-11-21.md`.
