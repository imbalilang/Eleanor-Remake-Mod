# CG Style Contract v5

Status: active  
Scope: all in-game CG assets  
Source of truth for style: `Art/场景参考` and `Art/原图参考`  
Existing approved CGs: content references only; not style masters

## 1. Canonical geometry

Provider-specific master canvas (approval artifact):

| Provider | Native master | Export to 960×540 |
|---|---:|---:|
| OpenAI `openai/gpt-image-2.6-flare` | **1536 × 864** | **62.5%** (5/8) |
| Grok `x-ai/grok-imagine-image-2.0` | **1280 × 720** | **75%** (3/4) |
| Hunyuan `tencent/hy-image-v3.5-preview` | **1024 × 576** | **93.75%** (15/16) |

- OpenAI calls must use `aspect_ratio=16:9` only; do **not** send a `size` parameter. `1536×864` is the supported/default 16:9 output.
- Grok calls use 1K 16:9; explicit `size=1280x720` is supported.
- Hunyuan calls use 1K 16:9; current OpenRouter route returns 1024×576. Do not request 1280×720 or 4K for Hunyuan: they fall back to 1024×576 / 2048×1152.
- Runtime export: **960 × 540**, default **Lanczos**, output **lossless PNG**.
- Display reference: **1920 × 1080** (2× nearest display only).
- Native master pixel grid: **1× at the provider-specific master**, not forced 2×2 or 4×4 blocks.
- All CG layers (background, character, props) share the same provider-specific master grid.

Rationale: OpenAI 16:9 defaults to 1536×864 and Grok 1K 16:9 is 1280×720. All scale to 960×540 with exact uniform factors: OpenAI 5/8, Grok 3/4, Hunyuan 15/16; no axis distortion. 960×540 is export-only; 1664×936 legacy CGs are frozen.

## 1a. Detail and grain

- The official scene language is **fine 1× pixel detail**, with 1–2 px structural lines.
- 2× is display magnification only; do not force 2×2 or 4×4 blocks as a drawing style.
- No granular noise, stippling or per-pixel random color variation.
- Large color fields must stay clean and readable at the native provider-specific master; 960×540 is only the runtime export.
- Dithering is sparse and intentional only; no full-screen checkerboard or noise texture.
- A noisy, grainy or prematurely enlarged look is a FAIL, even if the palette is limited.

## 2. Scene grammar

Primary references: `Art/场景参考/*`.

- Closed lower-district space-station spaces; no unobstructed open horizon (enclosed glass atrium with structural mullions is allowed when the source content requires it)
- Roof, upper deck, beams, cable trays or low ceiling at the top of every exterior view
- Patched gray-green metal, oxidized mauve, steel blue, worn concrete, exposed conduits
- Practical light only: fluorescent strips, CRT screens, work lamps, neon signs, doorway light
- Foreground occlusion / open midground staging area / structured background
- 1–2 px structural lines; sparse dithering only
- 64–192 colors is a typical guidance range, not a hard cap; complex interiors may exceed it
- No anti-aliasing, no smooth gradients, no photographic texture, no bloom

Scene type references:
- `场景参考1.png`: pawnshop / sales-booth interior
- `场景参考2.png`: roofed lower-district commercial alley
- `场景参考3.png`: ration store / grocery interior
- `场景参考4.png`: scrapyard / industrial lower deck
- `CG参考1.png`: underground hideout atmosphere; not a general architecture template

## 3. Character grammar

Primary style references: `Art/原图参考/QQ图片*.png` and `其他角色1/2.png` (render style only).

- Build on a clean low-resolution logical grid, then integer nearest-neighbor upscale
- 3–5 hard cel-shading steps per material
- Warm dark-brown/near-black outline, 1–2 logical px
- Hair in broad ribbon-like locks with 1–2 wide highlight bands; no fine wispy strands
- Large clear eyes with dark upper-lash blocks, layered iris and square/round catchlights
- Readable accessories at 1× logical size
- Identity comes from the specific character portrait; style comes from the pixel rendering grammar
- Do not copy the reference character's hair color, face, outfit or background into another character

## 4. Content lock

Preserve from the source CG:

- composition and camera
- number and identity of characters
- pose and hand placement
- clothing silhouette and key props
- scene layout and key objects

Allowed to change:

- edge treatment
- palette behavior
- shading steps
- anti-aliasing / dithering / texture treatment

Not allowed:

- crop or recenter the scene
- replace or add characters
- change pose, outfit identity or key props
- use a different scene layout

## 5. Production recipe

One production run uses:

- one primary scene mother (or one scene-type grammar reference)
- one character style mother per character
- one content master
- one fixed prompt template
- one fixed provider/model/size/quality/reference order

The prompt template is in `docs/CG_PROMPT_TEMPLATE.md`.

## 6. QA

QA is independent from generation and checks content first, style second.

Output states:

- `PASS`: content preserved and style belongs to the target family
- `FAIL`: content altered or style clearly outside the target family
- `UNCERTAIN`: not enough evidence; do not install

Hard checks:

- no anti-aliasing or smooth gradients
- no checkerboard dithering
- no photographic texture or real lens blur
- no granular noise or stippling
- line width and pixel block consistent with the provider-specific native master (1–2 px structural lines); 960×540 is the runtime export
- content lock intact (composition, characters, pose, outfit, props)
- enclosed-sky / no-open-horizon rule intact

Soft checks:

- visual distance to the selected scene grammar
- visual distance to the selected character pixel grammar
- palette and shading-step consistency

`tools/check_pixel_style.py` is supporting evidence only. Its five metrics are not a global pass gate and must not be used as the generation target.

## 7. Acceptance order

1. audit reference set and bind scene/character mothers
2. fix prompt template and model recipe
3. run 3 content variations
4. QA content + style
5. only then produce the CG batch
6. install only after explicit approval

