# CG Repeatability Test Result 2026-11-22

Status: generation-stage PASS; runtime re-grid pending  
Contract: `cg_pixel_contract_v1`

## Run outputs

| Test | Output | Size | Verdict |
|---|---|---:|---|
| `er_event_harem_lydia_date_cg.png` | `Art/character-assets-2026-11-20/cg/style_contract_repeatability_2026-11-22/harem_lydia_date.png` | 1536×864 | PASS (generation stage) |
| `er_event_love_water_cut_cg.png` | `.../love_water_cut.png` | 1536×864 | PASS (generation stage) |
| `er_event_maid_gala_cg.png` | `.../maid_gala.png` | 1536×864 | PASS (generation stage) |

Contact sheet: `.../repeatability_contact.jpg`  
Report: `.../repeatability_report.json`

## QA notes

- Composition, camera, character count, identity, pose, clothing silhouette, props and scene layout were preserved in all three.
- The three outputs share the same pixel-art treatment and palette behavior.
- The harem date scene contains an enclosed glass atrium; this is allowed by the updated contract as an enclosed-sky exception. It is not an open horizon.
- `tools/check_pixel_style.py` reports FAIL 0/5 on all three because the old CORE profile does not cover this content family. Per contract, it is supporting evidence only, not the gate.

## Not done

- No 832×468 logical re-grid
- No 1664×936 runtime export
- No `Textures/Items` changes
- No install

## Next

1. Implement deterministic 832×468 re-grid + exact 2× nearest export.
2. Re-run content/style QA on the runtime-size files.
3. If still PASS, prepare the CG batch plan; do not install without approval.
