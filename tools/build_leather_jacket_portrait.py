"""Legacy full-body leather pipeline. For new bust-first portraits use tools/build_bust_portrait_asset.py."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageOps
from collections import deque
import numpy as np, json, hashlib, shutil, sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/pixel-art-direction-2026-11-19/leather-jacket-portrait-pipeline'
SOURCE = ROOT / 'Art/model-tests/openai-fullbody-image6c610a32-2026-11-19/outputs/01_openai_fullbody_eleanor_cyberpunk.png'
sys.path.insert(0, str(ROOT / 'tools'))
from author_portrait_pipeline import render

CROP = (230, 0, 760, 560)
KEY_THRESHOLD = 30
TRIM_PASSES = 2
TRIM_MIN = 170
TRIM_SPREAD = 70
PARAMS = dict(visible_height=204, centre_x=144.5, bottom=211,
              canvas_size=(288, 211), add_top_row=True, magenta_key=False,
              fringe=False, detail=True, bleed=True, coverage_threshold=0.35)
PROPOSED_KEY = 'er_portrait_eleanor_revolution_date_female_beauty'
CANDIDATE_NAME = PROPOSED_KEY + '_candidate.png'
# Automatic hair-gap repair is disabled: it removed facial alpha and was rejected.
AUTO_HAIR_GAP_REPAIR = False

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def white_key(crop, threshold):
    w, h = crop.size
    pad = 3
    canvas = Image.new('RGB', (w + 2 * pad, h + 2 * pad), (255, 255, 255))
    canvas.paste(crop, (pad, pad))
    ImageDraw.floodfill(canvas, (0, 0), (255, 0, 255), thresh=threshold)
    arr = np.array(canvas)
    key = (arr[:, :, 0] > 250) & (arr[:, :, 1] < 5) & (arr[:, :, 2] > 250)
    key = key[pad:-pad, pad:-pad]
    rgba = np.dstack([np.array(crop), np.full((h, w), 255, dtype=np.uint8)])
    rgba[key, 3] = 0
    return Image.fromarray(rgba, 'RGBA'), int(key.sum())

def trim_light(rgba_img, passes, minv, spread):
    a = np.array(rgba_img).copy()
    alpha = a[:, :, 3]
    before = int((alpha > 0).sum())
    for _ in range(passes):
        op = alpha > 0
        trans = ~op
        nb = np.zeros_like(op)
        nb[1:, :] |= trans[:-1, :]
        nb[:-1, :] |= trans[1:, :]
        nb[:, 1:] |= trans[:, :-1]
        nb[:, :-1] |= trans[:, 1:]
        boundary = op & nb
        rgb = a[:, :, :3].astype(int)
        mn = rgb.min(axis=2)
        mx = rgb.max(axis=2)
        light = (mn >= minv) & ((mx - mn) <= spread)
        rem = boundary & light
        if not rem.any():
            break
        alpha[rem] = 0
    a[:, :, 3] = alpha
    return Image.fromarray(a, 'RGBA'), before - int((alpha > 0).sum())


def repair_hair_gaps(rgba_img, ratio_threshold=0.55, max_area=1300,
                     alpha_max_y=310, near_min=200, near_spread=80,
                     edge_radius=3):
    """Repair white pockets inside the hair without punching holes in the jacket.

    Stage 1: find light connected components near hair. Components whose ring is
    hair-dominant are repaired. Above the shoulder line they are made
    transparent; below it they are recolored with the local hair colour because
    hair overlaps the white jacket there.

    Stage 2: recolor leftover light pixels within a few pixels of hair. This
    catches small enclosed background gaps and pale anti-aliased specks while
    leaving the central jacket untouched.
    """
    im = rgba_img.convert('RGBA')
    a = np.array(im).copy()
    rgb = a[:, :, :3].astype(int)
    alpha = a[:, :, 3]
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    hair = ((r > 110) & ((r - g) > 25) & ((g - b) > 0)) | (
        (r > 70) & (r < 180) & ((r - g) > 20) & ((g - b) > 2)
    )
    mn = np.minimum(np.minimum(r, g), b)
    mx = np.maximum(np.maximum(r, g), b)
    spread = mx - mn
    near = (mn >= near_min) & (spread <= near_spread)
    hair_near = np.asarray(
        Image.fromarray((hair.astype(np.uint8) * 255)).filter(ImageFilter.MaxFilter(25))
    ) > 0
    candidate = near & (alpha > 0) & hair_near
    h, w = candidate.shape
    seen = np.zeros((h, w), bool)
    comps = []
    for y, x in zip(*np.where(candidate & ~seen)):
        if seen[y, x]:
            continue
        q = deque([(y, x)])
        seen[y, x] = True
        px = []
        while q:
            cy, cx = q.popleft()
            px.append((cy, cx))
            for ny in (cy - 1, cy, cy + 1):
                for nx in (cx - 1, cx, cx + 1):
                    if 0 <= ny < h and 0 <= nx < w and candidate[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        q.append((ny, nx))
        ys = [p[0] for p in px]
        xs = [p[1] for p in px]
        x0, x1, y0, y1 = min(xs), max(xs) + 1, min(ys), max(ys) + 1
        pixels = set(px)
        hair_count = other_count = 0
        for yy in range(max(0, y0 - 4), min(h, y1 + 4)):
            for xx in range(max(0, x0 - 4), min(w, x1 + 4)):
                if (yy, xx) in pixels:
                    continue
                if alpha[yy, xx] > 0:
                    if hair[yy, xx]:
                        hair_count += 1
                    else:
                        other_count += 1
        ratio = hair_count / (hair_count + other_count) if hair_count + other_count else 0.0
        if len(px) <= max_area and ratio >= ratio_threshold:
            comps.append((len(px), x0, y0, x1, y1, ratio, px))

    # Local average of nearby hair pixels. Used only for recoloring; alpha is
    # never removed where hair overlaps the jacket.
    wt = hair.astype(np.float32)
    num = np.dstack([
        np.asarray(
            Image.fromarray(np.clip(rgb[:, :, c] * wt, 0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(15)),
            dtype=np.float32,
        )
        for c in range(3)
    ])
    den = np.asarray(
        Image.fromarray((wt * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(15)),
        dtype=np.float32,
    ) / 255.0
    avg = np.zeros_like(rgb, dtype=np.float32)
    for c in range(3):
        avg[:, :, c] = np.divide(num[:, :, c], np.maximum(den, 1e-6))
    fallback = np.array([190, 95, 55], dtype=np.float32)
    avg = np.where((den > 0.003)[:, :, None], avg, fallback)

    alpha_removed = 0
    recolored = 0
    for area, x0, y0, x1, y1, ratio, px in comps:
        if y1 <= alpha_max_y:
            for yy, xx in px:
                a[yy, xx, 3] = 0
            alpha_removed += area
        else:
            for yy, xx in px:
                a[yy, xx, :3] = np.clip(avg[yy, xx], 0, 255).astype(np.uint8)
            recolored += area

    # Stage 2: tight hair-edge light recolor. The central jacket is excluded.
    hair_edge = np.asarray(
        Image.fromarray((hair.astype(np.uint8) * 255)).filter(ImageFilter.MaxFilter(2 * edge_radius + 1))
    ) > 0
    Y, X = np.mgrid[0:h, 0:w]
    face = ((X - 265) / 125.0) ** 2 + ((Y - 205) / 110.0) ** 2 <= 1
    central_jacket = (X >= 170) & (X <= 360) & (Y >= 260)
    edge_light = near & (a[:, :, 3] > 0) & hair_edge & (~face) & (~central_jacket)
    for yy, xx in zip(*np.where(edge_light)):
        a[yy, xx, :3] = np.clip(avg[yy, xx], 0, 255).astype(np.uint8)

    return Image.fromarray(a, 'RGBA'), {
        'ratio_threshold': ratio_threshold,
        'max_area': max_area,
        'alpha_removal_max_y': alpha_max_y,
        'near_min': near_min,
        'near_spread': near_spread,
        'components': len(comps),
        'alpha_removed_pixels': alpha_removed,
        'recolored_pixels': recolored,
        'edge_recolored_pixels': int(edge_light.sum()),
    }

def metrics(im):
    im = im.convert('RGBA')
    a = np.array(im)
    al = a[:, :, 3]
    bbox = list(im.getchannel('A').getbbox())
    return {
        'size': list(im.size),
        'bbox': bbox,
        'top_margin_pct': round(bbox[1] / im.height * 100, 2),
        'left_margin_pct': round(bbox[0] / im.width * 100, 2),
        'right_margin_pct': round((im.width - bbox[2]) / im.width * 100, 2),
        'visible_height_pct': round((bbox[3] - bbox[1]) / im.height * 100, 2),
        'alpha_values': sorted(np.unique(al).tolist()),
        'opaque_colours': len(np.unique(a[al == 255, :3], axis=0)),
    }

def make_sheet(entries, path, scale=3, bg=(37, 44, 53)):
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 16)
    sheet = Image.new('RGB', (288 * len(entries), 240), bg)
    draw = ImageDraw.Draw(sheet)
    for i, (label, im) in enumerate(entries):
        panel = Image.new('RGBA', (288, 212), (*bg, 255))
        panel.alpha_composite(im.convert('RGBA'))
        sheet.paste(panel.convert('RGB'), (i * 288, 28))
        draw.text((i * 288 + 5, 6), label, fill=(240, 240, 245), font=font)
    sheet.resize((sheet.width * scale, sheet.height * scale), Image.Resampling.NEAREST).save(path, optimize=True)

def main():
    for sub in ('source', 'masters', 'candidates', 'stages', 'previews', 'comparisons', 'experiments'):
        (OUT / sub).mkdir(parents=True, exist_ok=True)

    if not (OUT / 'source' / SOURCE.name).exists():
        shutil.copy2(SOURCE, OUT / 'source' / SOURCE.name)

    source = Image.open(SOURCE).convert('RGB')
    crop = source.crop(CROP)
    crop.save(OUT / 'masters/leather_jacket_bust_master_rgb.png', optimize=True)

    raw, keyed_count = white_key(crop, KEY_THRESHOLD)
    raw.save(OUT / 'masters/leather_jacket_bust_transparent_raw.png', optimize=True)
    boundary, removed_count = trim_light(raw, TRIM_PASSES, TRIM_MIN, TRIM_SPREAD)
    boundary.save(OUT / 'masters/leather_jacket_bust_transparent_boundary_trimmed.png', optimize=True)
    if AUTO_HAIR_GAP_REPAIR:
        trans, gap_counts = repair_hair_gaps(boundary)
    else:
        trans = boundary.copy()
        gap_counts = {'status': 'disabled_auto_repair_rejected_face_holes'}
    trans.save(OUT / 'masters/leather_jacket_bust_transparent.png', optimize=True)

    result, report, stages = render(trans, **PARAMS)
    candidate_path = OUT / 'candidates' / CANDIDATE_NAME
    result.save(candidate_path, optimize=True)
    for name, im in stages.items():
        im.save(OUT / 'stages' / f'{name}.png', optimize=True)

    for label, bg in [('white', (255, 255, 255, 255)), ('black', (0, 0, 0, 255)), ('gray', (37, 44, 53, 255))]:
        canvas = Image.new('RGBA', result.size, bg)
        canvas.alpha_composite(result)
        canvas.convert('RGB').resize((result.width * 4, result.height * 4), Image.Resampling.NEAREST).save(
            OUT / 'previews' / f'{label}_4x.png', optimize=True)

    master_preview = Image.new('RGBA', trans.size, (0, 0, 0, 255))
    master_preview.alpha_composite(trans)
    master_preview.convert('RGB').resize((trans.width * 2, trans.height * 2), Image.Resampling.NEAREST).save(
        OUT / 'previews/transparent_master_black_2x.png', optimize=True)

    edge = Image.new('RGBA', result.size, (0, 0, 0, 255))
    edge.alpha_composite(result)
    edge.convert('RGB').crop((35, 0, 253, 150)).resize((218 * 6, 150 * 6), Image.Resampling.NEAREST).save(
        OUT / 'previews/head_edge_black_6x.png', optimize=True)

    current = Image.open(ROOT / 'Textures/Items/er_portrait_eleanor_revolution_undersuit_female_beauty.png').convert('RGBA')
    adopted = Image.open(ROOT / 'Art/pixel-art-direction-2026-11-19/portrait_postprocess/processed_288x212.png').convert('RGBA')

    qq_rgba = Image.open(ROOT / 'Art/原图参考/QQ图片20261119194822.png').convert('RGBA')
    qq_panel = Image.new('RGBA', qq_rgba.size, (37, 44, 53, 255))
    qq_panel.alpha_composite(qq_rgba)
    qq = ImageOps.fit(qq_panel.convert('RGB'), (288, 212), method=Image.Resampling.LANCZOS, centering=(0.5, 0.5)).convert('RGBA')

    make_sheet([
        ('New leather jacket', result),
        ('Current leather runtime', current),
        ('Adopted pixel portrait', adopted),
        ('QQ framing reference', qq),
    ], OUT / 'comparisons/four_way_framing_3x.png', scale=3)
    make_sheet([
        ('New leather jacket', result),
        ('Current leather runtime', current),
    ], OUT / 'comparisons/new_vs_current_3x.png', scale=3)

    new_metrics = metrics(result)
    full_report = {
        'status': 'candidate_not_installed',
        'proposed_runtime_key': PROPOSED_KEY,
        'candidate_file': str(candidate_path.relative_to(ROOT)),
        'source': {'file': str(SOURCE.relative_to(ROOT)), 'sha256': sha(SOURCE), 'size': list(source.size)},
        'crop': {'box': list(CROP), 'size': list(crop.size)},
        'transparency': {
            'method': 'white floodfill from padded border; bottom included through padding; alpha only 0/255',
            'threshold': KEY_THRESHOLD,
            'keyed_pixels': keyed_count,
            'light_boundary_trim': {'passes': TRIM_PASSES, 'min_channel': TRIM_MIN,
                                    'channel_spread': TRIM_SPREAD, 'removed_pixels': removed_count},
            'hair_gap_repair': gap_counts,
            'transparent_bbox': list(trans.getchannel('A').getbbox()),
            'alpha_values': sorted(np.unique(np.array(trans)[:, :, 3]).tolist()),
        },
        'sampler': report,
        'output_metrics': new_metrics,
        'output_sha256': sha(candidate_path),
        'candidate_size_bytes': candidate_path.stat().st_size,
        'comparison': {
            'current_leather_runtime': metrics(current),
            'adopted_pixel_portrait': metrics(adopted),
            'qq_reference_source_size': [1470, 1070],
        },
        'comparison_sheet': str((OUT / 'comparisons/four_way_framing_3x.png').relative_to(ROOT)),
        'notes': [
            'B/detail exact area sampling with Ht=204, Cx=144.5, Yb=211.',
            'White jacket protected by boundary-only light trim; no global white removal.',
            'Rich-color preservation: no palette quantization, per Art/图片生成提示词.txt.',
            'Candidate only: not copied into Textures/Items and no runtime key registered.',
        ],
    }
    (OUT / 'report.json').write_text(json.dumps(full_report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'candidate': str(candidate_path), 'keyed': keyed_count,
                      'trimmed': removed_count, 'metrics': new_metrics,
                      'sha': full_report['output_sha256']}, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    main()
