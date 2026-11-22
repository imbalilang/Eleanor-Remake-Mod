# CG Prompt Template v5

Status: active  
Contract: `docs/CG_STYLE_CONTRACT.md` / `docs/cg_style_contract.json`

## Fixed style block

Use this block unchanged for every CG conversion:

> Render as authentic project pixel art, not a filtered anime image.  
> Fine 1× pixel detail with hard pixel edges; do not force coarse 2×2/4×4 blocks.  
> 3–5 hard cel-shading steps per material; clean flat color fields.  
> Limited cohesive palette; no smooth gradients, no airbrush, no soft anti-aliasing, no photographic texture, no lens blur, no checkerboard dithering, no granular noise or stippling.  
> Closed lower-district space-station space; roof/ceiling/beams at the top; practical lights only.  
> Character rendering follows the project's pixel-character grammar: warm dark outline, broad hair locks, large layered eyes, readable accessories.  
> Preserve the source CG's composition, camera, character count, identity, pose, clothing silhouette, key props and scene layout.  
> Style conversion only; do not add, remove, crop or replace content.

## Content slots

- `{{CONTENT_MASTER}}`: source CG path
- `{{SCENE_TYPE}}`: pawnshop_interior | commercial_alley | ration_store | scrapyard | hideout | other
- `{{SCENE_MOTHER}}`: one scene reference path
- `{{CHARACTER_IDENTITY_REFS}}`: one or more character portrait paths
- `{{CHARACTER_STYLE_MOTHER}}`: one global pixel-character style reference
- `{{PRESERVE_LIST}}`: explicit content-lock list for this CG
- `{{OUTPUT_SIZE}}`:
  - OpenAI: native master **1536×864**; request **only** `aspect_ratio=16:9`, no `size`; runtime export 960×540 at 62.5% Lanczos
  - Grok: native master **1280×720**; request 1K 16:9 / `size=1280x720`; runtime export 960×540 at 75% Lanczos
  - Hunyuan: native master **1024×576**; request 1K 16:9; runtime export 960×540 at 93.75% Lanczos
  - Optional display export: 1920×1080 by 2× nearest

## Reference order

1. content master
2. character identity portrait(s)
3. one scene mother
4. one character style mother

Never mix multiple scene mothers or multiple character style mothers in one run unless the user explicitly requests a comparison test.

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
- No extra style mothers
- No batch until a 3-content repeatability test passes
- No install without explicit approval
