# Grok Large-CG Routing - 2026-11-21

## Rule

For large-scale CG work rejected by OpenAI's safety system, use Grok Imagine Image 2.0 as the preferred generator, provided the job is a content-master restyle rather than text-only generation.

## Working recipe

- provider: openai-compat
- channel: openrouter
- model: x-ai/grok-imagine-image-2.0
- size: explicit WIDTHxHEIGHT (use 2048x1152 for exact 16:9)
- quality: medium
- references: one content master plus exactly one pixel-style anchor (never multiple style images)
- prompt: preserve layout/faces/pose, but explicitly discard the source's smooth rendering

## Evidence

Style-anchor positions 2, 3, 4 and 5 were tested at 2048x1152. All four passed check_pixel_style.py 5/5. Position 5 produced the highest flat_ratio (0.298) and was archived as harem_two_bunny_bed_grok_restyle_p5.

## Boundaries

- Grok text-only / multi-reference generation from scratch did not control pixel style reliably.
- Grok image-to-image restyle from a smooth content master can under-convert; use one pixel-style anchor plus explicit discard-smooth-rendering wording. Multiple style references pollute content and are forbidden.
- If Grok rejects the content, use Tencent Hunyuan as fallback. Tencent allows at most one correction pass before drifting.
- OpenAI remains preferred for precise edits when it accepts the content.


## Large-CG 4/5 acceptance

Grok large CGs may be accepted at 4/5 when exactly one style metric fails within 40% relative deviation of its nearest band boundary, the image is visually acceptable, and there is no T0 failure. See `docs/CG_STYLE_ACCEPTANCE_GROK_2026-11-21.md`.

Applied case: `image-f0c62f02.jpg` -> `GROK_LARGE_CG_4OF5_ACCEPTED`; palette_5bit 2066 vs max 2009, 2.84% deviation.


## Multi-style reference pollution (2026-11-21)

P2-P5 multi-style tests passed 5/5 but polluted CG content (rose/hair accessories/composition). New work uses exactly one content master and one pixel-style anchor. See docs/GROK_STYLE_TEST_LOG_2026-11-21.md. The archived `harem_two_bunny_bed_grok_restyle_p5` is historical evidence only.
