# Grok Style Lane (2026-11-23)

This lane is frozen. No new style anchor is to be selected in this session.

- Primary model: `x-ai/grok-imagine-image-2.0`
- Master size: 1280×720, quality medium
- Runtime: 960×540, Lanczos 0.75, lossless PNG
- Sole style anchor: `Art/character-assets-2026-11-20/cg/white_floral_bed/approved/white_floral_bed_master_1280x720.jpg` (`image-789bbf25.jpg`, approved)
- Anchor is style-only. Do not copy its pose, hands, outfit, bed or composition.
- Content, identity and body references are supplied separately and are not style anchors.
- OpenAI is fallback only; its output is marked separately and never used to redefine this lane.
- This anchor is not to be changed unless the user explicitly reopens the decision.

## Hair color lock

Both Eleanor and Sophia use the SAME canonical hair base color, taken from `Art/原图参考/QQ图片20261119200057.png`:

- base copper-orange: `#CC6A47`
- shadow auburn: `#83344D`
- highlight peach: `#EEA175`

Lighting may shift value/temperature, but the base hue family must stay in this copper-orange range. CG outputs whose hair reads as dark brown-orange or golden-orange are not compliant.

## Hair color at generation time

Do NOT add or correct hair color on an already generated image. Image-to-image recolors are weak and often preserve the old color.

Every new Grok generation must include this fixed hair block from the first prompt:

`HAIR COLOR LOCK: both Eleanor and Sophia use the same canonical copper-orange hair family: base #CC6A47, shadow #83344D, highlight #EEA175. Lighting may shift value, but the base hue must stay copper-orange.`

The hair reference crop from `Art/原图参考/QQ图片20261119200057.png` must be supplied at generation time, not used afterward.

## Hair Colour QA gate (2026-11-23)

Every new Grok CG must run:

```powershell
python tools\check_hair_color.py --json <generated-image>
```

Record the result in the CG report. Thresholds: PASS <=6 ΔE, WARN <=12 ΔE, FAIL >12 ΔE.
A FAIL image is not approvable; re-generate with the fixed HAIR COLOR LOCK block. Do not post-hoc recolour.
