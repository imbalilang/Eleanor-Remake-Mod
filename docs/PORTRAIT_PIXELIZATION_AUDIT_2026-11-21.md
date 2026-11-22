# Portrait Pixelization Audit - 2026-11-21

Scope: B:\Eleanor Story Expand\Textures\Items

Audited 121 PNGs: 17 base/outfit portraits and 104 expression variants across 11 bases. All are 288x212 RGBA.

Visual contact sheet: Art/art-audit-current/portrait_pixel_audit/portrait_base_and_sample_2x_contact.jpg

## Result

Not all implemented portraits are pixelized. Only 2 of 121 are low-color / flat-shaded enough to be considered pixel-art-adjacent.

- cp_portrait_* Eleanor bases: 5 files, 7,930-9,426 unique colors, not pixelized
- er_portrait_*_female_beauty authored outfits: 8 files, 4,860-9,898 unique colors, not pixelized
- er_portrait_*_female_vanilla fallbacks: 4 files, 48-8,576 unique colors; only maid vanilla (49) and mother vanilla (48) are low-color
- er_expr_* expression variants: 104 files, 4,855-9,952 unique colors, not pixelized

Low-color files:
- er_portrait_mother_female_vanilla.png: 48 unique colors
- er_portrait_eleanor_maid_female_vanilla.png: 49 unique colors

The other 119 files are smooth anime portraits with gradients and anti-aliasing. Expression samples inherit the smooth base rendering.

## Release folder discrepancy

dist/candidate-0.67-rc2/EleanorRomance/Textures/Items contains only 6 portrait PNGs: maid beauty/vanilla, Lydia beauty/vanilla, mother beauty/vanilla. It has no cp_portrait_*, no gala/love-intimate/revolution/date/home beauty portraits, and no er_expr_* files. build.sh copies all Textures/Items/*.png, so a fresh build would include the 121-file source set; the checked dist candidate is stale/incomplete.

## Conclusion

No, the portrait set has not been pixelized. The CG pixel-style work has not been applied to the currently implemented portrait/expression set.

## Recommended next step

1. Freeze the portrait family list and decide whether pixelized portrait means native 288x212 pixel art or a larger master downsampled to 288.
2. Pixelize the 17 base portraits first; expressions can be regenerated from the pixelized base.
3. Do not apply the 960px CG style gate directly to 288x212 transparent portraits; use portrait-specific checks.
4. Rebuild dist and verify all 121 portrait/expression files are present.
