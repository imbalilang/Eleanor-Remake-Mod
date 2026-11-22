# CG Runtime Export Standard — 2026-11-23

Status: active
Contract: `cg_pixel_contract_v5`

## Geometry

- Native generation / approval master:
  - OpenAI `gpt-image-2.6-flare`: **1536 × 864** (request `aspect_ratio=16:9` only, no `size`)
  - Grok `x-ai/grok-imagine-image-2.0`: **1280 × 720** (1K 16:9)
  - Hunyuan `tencent/hy-image-v3.5-preview`: **1024 × 576** (1K 16:9)
- Runtime export: **960 × 540**
- Scale: **62.5%** for OpenAI (5/8); **75%** for Grok (3/4); **93.75%** for Hunyuan (15/16)
- Resample: **Lanczos** by default (`box`/`bicubic` available for testing only)
- Output: **PNG, lossless**
- Display reference: 1920 × 1080 (2× nearest), not the runtime file

## Rationale

OpenAI 16:9 currently defaults to **1536×864** when only `aspect_ratio=16:9` is sent; sending `size=1280x720` causes a fallback to 1672×941, so OpenAI must not send `size`.
Grok 1K 16:9 is **1280×720**.
All three map to 960×540 by exact uniform factors: OpenAI 5/8 (62.5%), Grok 3/4 (75%), Hunyuan 15/16 (93.75%). None can natively produce exact 960×540; runtime export is deterministic resampling.

## Rules

- Approve and QC at the provider-specific native master: OpenAI **1536×864**, Grok **1280×720**, Hunyuan **1024×576**.
- Export runtime with `tools/export_cg_runtime_960.py` (accepts both master sizes).
- Do not use generative re-grid or style pass for the runtime export.
- Do not install a 960×540 file that was not produced from an approved provider-specific master.
- Existing 1664×936 CGs are frozen; do not mix them into this standard.
- `release-files.json` must be updated after any runtime replacement.

## Tool

```powershell
python tools/export_cg_runtime_960.py <master1536x864_1280x720_or_1024x576> <runtime960x540.png> --filter lanczos
```

The tool accepts 1536×864 (OpenAI), 1280×720 (Grok) or 1024×576 (Hunyuan), validates output 960×540, records the exact scale factor, and prints SHA-256 for both files.
