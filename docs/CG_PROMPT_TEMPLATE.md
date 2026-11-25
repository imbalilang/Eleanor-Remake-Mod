# CG Prompt Template v5

Status: active  
Contract: `docs/CG_STYLE_CONTRACT.md` / `docs/cg_style_contract.json`

## Fixed style block

Use this block unchanged for every CG conversion:

> Match the rendering of the sole CG style master, `Art/cg_style_master.png`. Match its contour weight, pixel steps, highlight clusters, and shading.
> Fine 1× pixel detail with hard pixel edges; do not force coarse 2×2/4×4 blocks.  
> 3–5 hard cel-shading steps per material; clean flat color fields.  
> Limited cohesive palette; no smooth gradients, no airbrush, no soft anti-aliasing, no photographic texture, no lens blur, no checkerboard dithering, no granular noise or stippling.  
> Closed lower-district space-station space; roof/ceiling/beams at the top; practical lights only.  
> Character rendering follows the project's pixel-character grammar: warm dark outline, broad hair locks, large layered eyes, readable accessories.  
> Preserve the source CG's composition, camera, character count, identity, pose, clothing silhouette, key props and scene layout.  
> Style conversion only; do not add, remove, crop or replace content.

## Content slots

- `{{CONTENT_MASTER}}`: source CG path for the event
- `{{SCENE_TYPE}}`: pawnshop_interior | commercial_alley | ration_store | scrapyard | hideout | other
- `{{SCENE_REFERENCE}}`: one location reference path
- `{{CHARACTER_IDENTITY_REFS}}`: one or more character portrait paths
- `{{CG_STYLE_MASTER}}`: `Art/cg_style_master.png`, the sole CG style master
- `{{PRESERVE_LIST}}`: explicit content-lock list for this CG
- `{{OUTPUT_SIZE}}`:
  - OpenAI: native master **1536×864**; request **only** `aspect_ratio=16:9`, no `size`; runtime export 960×540 at 62.5% Lanczos
  - Grok: native master **1280×720**; request 1K 16:9 / `size=1280x720`; runtime export 960×540 at 75% Lanczos
  - Hunyuan: native master **1024×576**; request 1K 16:9; runtime export 960×540 at 93.75% Lanczos
  - Optional display export: 1920×1080 by 2× nearest

## Reference order

1. source CG for the event
2. character identity portrait(s)
3. one location reference
4. `Art/cg_style_master.png`

## Model recipe (slot)

- provider/channel: `openai-compat` / `openrouter`
- model: `openai/gpt-image-2.6-flare`
- size: provider-specific (OpenAI: no `size`, only `aspect_ratio=16:9`; Grok: `size=1280x720` / 1K); record exact output
- quality: high
- acceptance canvas: OpenAI `1536×864` / Grok `1280×720` / Hunyuan `1024×576` native master; `960×540` runtime export at 62.5% / 75% / 93.75% Lanczos
- reference order: as above

Alternative models may be recorded per run, but cannot be silently substituted.

## Post-generation geometry

- Design and approve at the provider-specific native master: OpenAI `1536×864`, Grok `1280×720`, Hunyuan `1024×576`
- Runtime export `960×540` by deterministic Lanczos: OpenAI 62.5% (5/8), Grok 75% (3/4), Hunyuan 93.75% (15/16), lossless PNG
- Optional display export `1920×1080` by exact 2× nearest
- Runtime 960×540 must not be produced by generative re-grid
- Existing 1664×936 CGs are frozen; do not mix them into the new standard

The runtime export must be deterministic resampling, not a generative pass. The provider-specific native master is the approval artifact.

## QA output

Return exactly one:

- `PASS`
- `FAIL`
- `UNCERTAIN`

Content lock is checked before style. Numeric metrics are supporting evidence only.

## Stop rules

- One content master per run
- Use the sole CG style master for style calibration
- No batch until a 3-content repeatability test passes
- No install without explicit approval

## Fixed Hair Color Block (v6)

HAIR COLOR LOCK: both Eleanor and Sophia use the same canonical copper-orange hair family: base #CC6A47, shadow #83344D, highlight #EEA175. Lighting may shift value, but the base hue must stay copper-orange. Do not generate golden-orange or dark brown-orange hair.

This block belongs in the initial generation prompt. Do not recolor an already generated image afterward.
