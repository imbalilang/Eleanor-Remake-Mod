# Scene Reference Standard 2026-11-23

Status: active

## Native canvas

- 场景参考1-4: 960x540 native, 16:9.
- 场景参考0: 1920x1080 2x display version.
- CG standard: provider-specific native master (OpenAI 1536x864 / Grok 1280x720 / Hunyuan 1024x576); 960x540 runtime export at 62.5% / 75% / 93.75% Lanczos, lossless PNG; optional 1920x1080 2x NEAREST display export.
- Existing 1664x936 CGs are frozen and must not be mixed into the new standard.

## Pixel rules

- Native 1x pixel grid, 1-2 px structural lines.
- Not forced 2x2 or 4x4 blocks; 2x is display magnification.
- Hard edges, no anti-aliasing, no smooth gradients, no photographic texture, no lens blur, no bloom.
- Sparse dithering only; no full-screen noise.

## Detail and grain

- Official references are fine-detail 1x pixel art, not coarse-block pixel art.
- 2x is display magnification only; do not draw in 2x2 or 4x4 blocks.
- No granular noise, stippling or per-pixel random color variation.
- Large color fields stay clean at the provider-specific native master; 960x540 is the runtime export. Dithering is sparse and intentional.
- Grainy or prematurely enlarged output is a FAIL.

## Palette

- 64-192 colors is typical guidance, not a hard cap.
- Actual native references: 74, 102, 601; 2x display reference: 886.
- Judge by hard edges and limited value steps, not a fixed color count.

## File facts

- 场景参考1.png and 场景参考2.png are byte-identical duplicates.
- SCENE_STYLE_ANALYSIS.md currently labels 场景参考1 as 1920x1080; the actual 1920 file is 场景参考0.png.
- CG参考1.png is 640x360 mood reference only; not part of the 1-4 size standard.

## Contract impact

- cg_pixel_contract_v5: OpenAI 1536x864, Grok 1280x720, Hunyuan 1024x576; runtime_export 960x540 at 62.5% / 75% / 93.75% Lanczos PNG; display 1920x1080 2x nearest.
- Prompt template and QA use provider-specific native masters; 960x540 is runtime export only.
- No migration of existing 1664x936 CGs until separately approved.
