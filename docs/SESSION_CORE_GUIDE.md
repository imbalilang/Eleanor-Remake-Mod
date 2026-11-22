# Session Core Guide — CG Generation Pipeline

Status: **binding for this session**
Date: 2026-11-22
Priority: read this before any CG generation or style-QA decision after a context reset.

## 1. Role

The assistant is not the artist. Its job is to translate the user's natural-language intent into controllable OpenRouter / `dsh-image-gen` requests, and to independently QA the result.

The main optimization target is the **language-to-model translation**:
- reference selection and order
- prompt wording and negative constraints
- provider / model / size / quality
- repeatable recipe, not one-off lucky generations

## 2. What "standard" means

A standard is **not** a single global five-metric range and not a subjective label.

It is a two-layer contract:

### Production contract
- one primary style mother per production
- one fixed prompt template
- fixed model and parameters
- only content variables change between generations
- success = same style across multiple different contents, with stable repeatability and low reroll cost

### QA contract
- independent from generation
- checks content preservation first
- then checks style-family membership
- returns `PASS` / `FAIL` / `UNCERTAIN`
- `UNCERTAIN` must not be silently accepted

Quantitative metrics are internal QA evidence only. They are not the generation target and are not a requirement the user has to interpret.

## 3. Target style

The target is defined by the project references, not by the style of previously approved CGs:

- scene style: `Art/场景参考`
- character style: `Art/原图参考` (少女当铺 / QQ character references)

Existing approved CGs are **content references only**. Their content was approved; their style is not the standard.

`image-8ebd4ec5.png` remains an approved historical style mother, but is not automatically the primary target for every new CG.

## 4. Content-preserving style conversion

The required operation is:

> preserve content, change style.

Preserve:
- composition / camera
- characters and identity
- pose
- clothing and key props
- scene layout

Change:
- rendering language only: pixel edges, palette behavior, shading, anti-aliasing, dithering

Minimum requirement:
- stable conversion from a fixed style mother + prompt

Higher in-game requirement:
- scene looks consistent with `Art/场景参考`
- characters look consistent with `Art/原图参考`

## 5. Stop rules

Do not start another generative batch until:
1. `Art/场景参考` has been audited and a primary scene mother is chosen
2. `Art/原图参考` character mapping is confirmed
3. one prompt template + model recipe is fixed
4. a 2–3 content repeatability test has passed

No game asset is installed or replaced without explicit approval.

## 6. Context-reset checklist

On a new context, before doing CG work:
- read this file
- recall: production contract + independent QA
- recall: content preservation first
- recall: target = 场景参考 + 原图参考
- do not re-open the "one absolute five-metric range" debate
