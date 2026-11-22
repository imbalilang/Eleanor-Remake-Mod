# Maid Facial Fusion Test (2026-11-22)

Status: **test only, not final, not installed**.

## Inputs

- Target neutral: `Textures/Items/er_portrait_eleanor_maid_female_beauty.png`
- Target high-res master: `Art/character-assets-2026-11-20/portrait_pixel_magenta_2026-11-22/final_masters/er_portrait_eleanor_maid_female_beauty_master.png`
- Expression reference: `Facial.png` (3x3, 1254x1254)
- Per-cell sources: `Art/eleanor1-expression-review-redraw-2026-11-15/masters/eleanor_maid-grid_cells/`

## Model fusion

- Full 3x3 model proof: `dsh-image-gen/image-c764e889.png` (returned 1024x1024, not requested 1536x1536).
- The nine expressions read correctly, but cell head scale/crop is not consistent; the sheet cannot be split directly into runtime assets.
- Individual 1536x1152 model fusion samples were generated for smile, serious, angry, sad, hurt, determined, tired, tsun.
- Raw smile: `dsh-image-gen/image-591fdf93.png`.
- 288 donors: `Art/character-assets-2026-11-20/portrait_expression_maid_facial_fusion_2026-11-22/donors/`.

## Runtime candidates

- 288 candidates: `Art/character-assets-2026-11-20/portrait_expression_maid_facial_fusion_2026-11-22/runtime/`
- All candidates preserve the neutral alpha exactly (alpha IoU 1.0).
- Smile full-model donor alpha IoU is only 0.879, so the full donor is not body-safe.
- Feature-only composites were made to lock the body; they work for smile/serious/sad/hurt/tired/tsun but angry/determined still show eye/ear registration artifacts.
- Contact sheet: `.../runtime/maid_facial_fusion_final_contact.jpg`.
- Do not use as final assets.

## Conclusion

The model can fuse the expression style from `Facial.png` into the maid portrait, but direct 288 compositing still needs per-expression facial registration. The 3x3 sheet also has non-uniform cell scale. No runtime assets were replaced.
