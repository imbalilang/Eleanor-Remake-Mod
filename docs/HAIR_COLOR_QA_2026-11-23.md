# Hair Colour QA (2026-11-23)

Tool: `tools/check_hair_color.py`

Run after generation:

```powershell
python tools\check_hair_color.py --json <generated-image>
```

## Method

- Masks orange pixels in the upper 72% of the frame.
- Rejects low-saturation wood/skin and pixels far from the canonical copper-orange palette.
- Reports median hair colour, CIE Lab ΔE to the QQ 200057 hair median, hue delta, saturation delta and value delta.
- The mask is heuristic; coverage and ΔE are relative QA signals, not a segmentation ground truth.

## Canonical reference

`Art/原图参考/QQ图片20261119200057.png`

- measured median: `#E07B49`
- reference self-distance: ΔE 4.72
- canonical locks: base `#CC6A47`, shadow `#83344D`, highlight `#EEA175`

## Results

| image | median | ΔE to ref | hue Δ | sat Δ | val Δ | verdict |
|---|---|---:|---:|---:|---:|---|
| `image-e5953494.jpg` (kabedon) | `#E26E4C` | 15.62 | +1.69° | -4.41 | +8.63 | FAIL |
| `image-f6695819.jpg` / Sophia style-aligned | `#B26A47` | 24.08 | +2.08° | -11.40 | -10.20 | FAIL |
| `image-2cdcd8c6.jpg` (book style-aligned) | `#C16239` | 18.58 | +0.25° | -2.30 | -4.31 | FAIL |
| `eleanor_shy_romance_book_haircal_master_1280x720.jpg` | `#BF613A` | 18.36 | +0.04° | -2.47 | -5.10 | FAIL |
| `eleanor_white_casual_kabedon_haircal_master_1280x720.jpg` | `#DE6C4D` | 14.93 | +1.57° | -4.45 | +7.06 | FAIL |
| `sophia_cooking_station_haircal_master_1280x720.jpg` | `#B36846` | 22.46 | +1.29° | -9.79 | -9.80 | FAIL |

## Conclusion

- Hue is already close in all samples (≤2.08°), but brightness/saturation and ΔE are not within the desired copper-orange lock.
- Post-hoc hair recolouring barely improved the numbers; it is not a reliable fix.
- Hair colour must be included in the initial generation prompt and this checker must be run before approval.

Thresholds currently: PASS ≤6, WARN ≤12, FAIL >12 ΔE.
