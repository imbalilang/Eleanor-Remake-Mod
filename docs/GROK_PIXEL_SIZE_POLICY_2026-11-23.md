# Grok Pixel-Art Resolution Policy — 2026-11-23

**Decision:** for Grok pixel-art CGs, generate at **1280×720** as the native master, approve the pixel feel, then export **960×540 at 75% Lanczos** for runtime and optionally **2560×1440 at 2× NEAREST** for display.

## Why

- Native 2K Grok output adds detail at the larger canvas, reducing the apparent pixel-block size.
- The 1280×720 Grok output has the stronger pixel-art feel.
- Integer 2× NEAREST doubles dimensions without changing a single logical pixel or colour.

## Approved recipe

1. Content + one style mother + prompt.
2. Grok `x-ai/grok-imagine-image-2.0`, request **1280×720** (1K tier), quality `medium`.
3. Judge the 1280 image for pixel feel, line continuity, identity and content.
4. Runtime export: **960×540 at 75% Lanczos**, lossless PNG (`tools/export_cg_runtime_960.py`).
5. Optional display export: exact **2× NEAREST** → 2560×1440.
6. No other resize, quantize, BOX or dithering step.

## Do not

- Do not use native 2K Grok output as the pixel-style master.
- Do not use any non-integer scaling except the approved 75% master→runtime export.
- Do not treat postprocess as a fix for broken lines or changed content.

## Selected asset

`Art/character-assets-2026-11-20/cg/harem_two_bunny_bed/grok_small_then_2x_2026-11-23/`

- `harem_two_bunny_bed_grok_source_1280x720.jpg`
- `harem_two_bunny_bed_grok_small_then_2x_master_2560x1440.png`
- `report.json`

Status: `selected_pending_install`.
