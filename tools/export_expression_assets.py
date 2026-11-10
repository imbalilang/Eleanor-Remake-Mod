"""Assemble generated face differences on locked portrait bases, and export E3 CG.

Creative expression editing is performed by image_gen. This export stage aligns
its results and assembles the facial layers without redrawing the body.
"""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

from author_portrait_pipeline import render, bleed_transparent_rgb, exact_area_sample, enhance_details
from expression_face_bounds import jaw_guard

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/expressions-and-endings-2026-11-10'
BEAUTY = Path('D:/Program Files (x86)/Steam/steamapps/workshop/content/4348910/3829696854/Textures/Items')
FAMILIES = {
    'eleanor1': BEAUTY / 'cp_portrait_eleanor1.png',
    'eleanor_scav': BEAUTY / 'cp_portrait_eleanor_scav.png',
    'eleanor_maid': ROOT / 'Textures/Items/er_portrait_eleanor_maid_female_beauty.png',
}
IRIS_OVERRIDES = {
    # Warm highlights make the left iris of this master amber rather than green.
    'er_expr_eleanor1_angry': [[656.5, 580.5], [775.5, 557.5]],
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def eyes(image):
    a = np.asarray(image.convert('RGBA'))
    bbox = image.getchannel('A').point(lambda n: 255 if n >= 128 else 0).getbbox()
    x0, y0, x1, y1 = bbox
    rgb = a[:, :, :3].astype(float)
    r, g, b = (rgb[:, :, c] for c in range(3))
    mask = (g - r > 4) & (g - b > 4) & (g > r * 1.05) & (g > b * 1.05) & (a[:, :, 3] >= 128)
    region = np.zeros(mask.shape, dtype=bool)
    region[round(y0 + (y1 - y0) * .2):round(y0 + (y1 - y0) * .55),
           round(x0 + (x1 - x0) * .15):round(x1 - (x1 - x0) * .15)] = True
    yy, xx = np.where(mask & region)
    if len(xx) < 2:
        raise ValueError('Green iris landmarks unavailable')
    points = np.column_stack((xx + .5, yy + .5))
    centres = np.array([points[points[:, 0].argmin()], points[points[:, 0].argmax()]])
    for _ in range(12):
        side = np.abs(points[:, 0, None] - centres[None, :, 0]).argmin(axis=1)
        centres = np.array([points[side == k].mean(axis=0) for k in range(2)])
    return centres[np.argsort(centres[:, 0])]


def face_mask(base, iris):
    """Fixed facial layer; alpha and all pixels outside it remain from neutral."""
    mask = Image.new('L', base.size)
    draw = ImageDraw.Draw(mask)
    left, right = iris
    midpoint = (left + right) / 2
    distance = np.linalg.norm(right - left)
    # This broad mask selects features; the separate neutral-face jaw guard
    # must still remove its lower edge, including any feathered spill.
    for x, y in iris:
        draw.ellipse((x - distance * .40, y - 8, x + distance * .40, y + 6), fill=255)
    draw.ellipse((midpoint[0] - distance * .60, midpoint[1] + 2,
                  midpoint[0] + distance * .57, midpoint[1] + 21), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(.55))
    return np.asarray(mask).astype(float) / 255


def export(install=False):
    jobs_path = ART / 'generation_jobs.json'
    data = json.loads(jobs_path.read_text(encoding='utf-8'))
    refs = ART / 'references_local'
    refs.mkdir(exist_ok=True)
    bases = {}
    for family, path in FAMILIES.items():
        local = refs / f'{family}_neutral.png'
        if not local.exists():
            shutil.copy2(path, local)
        bases[family] = Image.open(local).convert('RGBA')
    deferred = [ROOT / 'Textures/Items' / (j['id'] + '.png') for j in data['jobs'] if j['family'] == 'lydia_daily']
    deferred.append(ROOT / 'Textures/Items/er_ending_harem_cg.png')
    protected = {p: sha(p) for p in deferred}
    records = []
    panels = {}
    for job in data['jobs']:
        if job['family'] == 'lydia_daily':
            job['status'] = 'deferred_by_user_pending_character_redesign'
            continue
        source_path = ART / 'masters' / (job['id'] + '.png')
        master = Image.open(source_path).convert('RGBA')
        base = bases[job['family']]
        ref_eyes = eyes(base)
        source_eyes = np.array(IRIS_OVERRIDES[job['id']], dtype=float) if job['id'] in IRIS_OVERRIDES else eyes(master)
        scale = float(np.linalg.norm(ref_eyes[1] - ref_eyes[0]) / np.linalg.norm(source_eyes[1] - source_eyes[0]))
        ref_mid, source_mid = ref_eyes.mean(axis=0), source_eyes.mean(axis=0)
        tx, ty = ref_mid - scale * source_mid - (0, 1)
        bbox = master.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
        height = scale * (bbox[3] - bbox[1])
        typical_height = 182 if job['family'] == 'eleanor_maid' else 150
        assert .8 * typical_height <= height <= 1.25 * typical_height, 'Review iris landmarks: ' + job['id']
        cx = tx + scale * (bbox[0] + bbox[2]) / 2
        bottom = ty + scale * bbox[3]
        aligned, pipeline, _ = render(master, height, cx, bottom, fringe=False)
        aligned.save(ART / 'portraits' / (job['id'] + '_aligned.png'))
        neutral, face = np.asarray(base).copy(), np.asarray(aligned)
        weight = face_mask(base, ref_eyes)
        guard = jaw_guard(base.size, job['family'])
        weight *= guard
        weight *= (neutral[:, :, 3] == 255) & (face[:, :, 3] == 255)
        final = neutral.copy()
        final[:, :, :3] = np.floor(neutral[:, :, :3] * (1 - weight[:, :, None])
                                   + face[:, :, :3] * weight[:, :, None] + .5).astype(np.uint8)
        assert np.array_equal(final[:, :, 3], neutral[:, :, 3])
        outside = weight == 0
        assert np.array_equal(final[outside], neutral[outside])
        jaw_and_neck = (guard == 0) & (neutral[:, :, 3] == 255)
        assert np.array_equal(final[jaw_and_neck], neutral[jaw_and_neck])
        final = bleed_transparent_rgb(final)
        output = ART / 'portraits' / (job['id'] + '.png')
        Image.fromarray(final).save(output)
        prompt_path = ART / 'prompts' / (job['id'] + '.txt')
        prompt_path.write_text(job['prompt'] + '\n', encoding='utf-8')
        if install:
            shutil.copy2(output, ROOT / 'Textures/Items' / output.name)
        changed = np.any(final[:, :, :3] != neutral[:, :, :3], axis=2) & (neutral[:, :, 3] == 255)
        record = {'id': job['id'], 'master': source_path.relative_to(ROOT).as_posix(),
                  'master_sha256': sha(source_path), 'base_sha256': sha(refs / f"{job['family']}_neutral.png"),
                  'output_sha256': sha(output), 'size': list(base.size), 'alpha_identical_to_base': True,
                  'body_opaque_pixels_identical_to_base': True,
                  'jaw_and_neck_pixels_identical_to_base': True,
                  'jaw_guard_profile': job['family'],
                  'changed_visible_pixels': int(changed.sum()),
                  'landmark_method': 'manual iris check' if job['id'] in IRIS_OVERRIDES else 'green iris clustering, reviewed at native size',
                  'reference_iris_landmarks': ref_eyes.tolist(), 'generated_iris_landmarks': source_eyes.tolist(),
                  'pipeline': pipeline, 'installed': install}
        records.append(record)
        job.update(status='installed' if install else 'exported_for_review', master='masters/' + source_path.name,
                   output='portraits/' + output.name)
        panels.setdefault(job['family'], []).append((job['expression'], Image.fromarray(final)))
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
    sheet = Image.new('RGB', (7 * 288, 3 * 240), (37, 44, 53))
    draw = ImageDraw.Draw(sheet)
    for row, (family, values) in enumerate(panels.items()):
        values = [('neutral', bases[family]), *values]
        for col, (name, image) in enumerate(values):
            bg = Image.new('RGBA', image.size, (37, 44, 53, 255))
            bg.alpha_composite(image)
            sheet.paste(bg.convert('RGB'), (col * 288, row * 240 + 28))
            draw.text((col * 288 + 5, row * 240 + 5), family + ' / ' + name, font=font, fill='white')
    sheet.save(ART / 'previews/expression-contact-sheet.png')
    # Detail views keep true native pixels and enlarge by an exact integer.
    for row, family in enumerate(panels):
        crop = sheet.crop((0, row * 240, 7 * 288, row * 240 + 240))
        crop.resize((crop.width * 2, crop.height * 2), Image.Resampling.NEAREST).save(ART / 'previews' / (family + '-2x.png'))
    cg_path = ART / 'masters/er_ending_maid_cg.png'
    cg = Image.open(cg_path).convert('RGBA')
    # Exact isotropic area sampling of an opaque scene. Centre-crop a fractional
    # extra strip if the generator's 16:9 dimensions are rounded by a few pixels.
    cg_scale = max(960 / cg.width, 540 / cg.height)
    cg_tx, cg_ty = (960 - cg.width * cg_scale) / 2, (540 - cg.height * cg_scale) / 2
    small, _ = exact_area_sample(np.asarray(cg), (960, 540), cg_scale, cg_tx, cg_ty)
    small, _ = enhance_details(small)
    assert np.all(small[:, :, 3] == 255)
    cg_out = ART / 'endings/er_ending_maid_cg.png'
    Image.fromarray(small).save(cg_out)
    if install:
        shutil.copy2(cg_out, ROOT / 'Textures/Items' / cg_out.name)
    assert all(sha(p) == value for p, value in protected.items()), 'Deferred artwork changed'
    report = {'scope': '17 Eleanor expressions and E3 CG only; Lydia and E4 are outside this exporter scope',
              'tool': 'image_gen.imagegen', 'installed': install, 'expressions': records,
              'ending': {'id': 'er_ending_maid_cg', 'source_size': list(cg.size), 'size': [960, 540],
                         'source_sha256': sha(cg_path), 'output_sha256': sha(cg_out),
                         'opaque_fraction': 1, 'isotropic_scale': cg_scale, 'installed': install},
              'deferred_unchanged': {p.name: value for p, value in protected.items()}}
    (ART / 'asset_validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    jobs_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'expressions': len(records), 'installed': install, 'deferred_unchanged': True,
                      'all_alpha_identical_to_neutral': all(r['alpha_identical_to_base'] for r in records),
                      'ending_size': [960, 540]}, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true', help='Replace the explicitly authorized runtime PNGs after reviewing the previews')
    export(parser.parse_args().install)
