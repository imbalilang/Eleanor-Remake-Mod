"""Export approved Lydia's generated facial layers into daily/date expressions."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

from author_portrait_pipeline import render, bleed_transparent_rgb
from expression_face_bounds import jaw_guard

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/lydia-expressions-2026-11-10'
DESIGN = ROOT / 'Art/lydia-style-fusion-2026-11-10'
BASES = {
    'daily': ('approved/er_portrait_lydia_female_beauty.png', 'er_portrait_lydia_female_beauty.png'),
    'date': ('portraits/er_portrait_lydia_date_female_beauty_fusion_v1.png', 'er_portrait_lydia_date_female_beauty.png'),
}
EXPRESSIONS = ['smile', 'flustered', 'serious', 'hurt', 'tease']


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def face_mask():
    # Manual native-frame facial landmarks. Do not fit to iris area: opening
    # the eyelids is the intended edit and must not cause compensating resizing.
    mask = Image.new('L', (288, 212))
    draw = ImageDraw.Draw(mask)
    for x, y in ((130.5, 117.5), (152.5, 114.0)):
        draw.ellipse((x - 11, y - 16, x + 11, y + 6), fill=255)
    draw.ellipse((126, 117, 156, 137), fill=255)
    return np.asarray(mask.filter(ImageFilter.GaussianBlur(.55))).astype(float) / 255


def eye_white_pixels(rgba):
    # A diagnostic measure, not an anatomical detector: same fixed eye regions
    # in every image. Warm skin/amber irises are excluded by low saturation.
    r = rgba[:, :, :3].astype(int)
    region = np.zeros(r.shape[:2], bool)
    region[109:123, 119:142] = True
    region[106:120, 142:165] = True
    white = (r.min(axis=2) > 170) & (r.max(axis=2) - r.min(axis=2) < 45)
    return int((white & region & (rgba[:, :, 3] == 255)).sum())


def export(install=False):
    ART.mkdir(exist_ok=True)
    for folder in ('portraits', 'previews', 'layers', 'prompts'):
        (ART / folder).mkdir(exist_ok=True)
    approval = json.loads((DESIGN / 'approval.json').read_text(encoding='utf-8'))
    source = DESIGN / approval['approved_master']
    assert sha(source) == approval['master_sha256'], 'Approved master changed'
    master_size = Image.open(source).size
    transform = json.loads((DESIGN / 'validation_v4.json').read_text(encoding='utf-8'))['transform']
    protected = {p: sha(p) for p in (ROOT / 'Textures/Items').glob('*.png')
                 if not p.name.startswith(('er_expr_lydia_', 'er_portrait_lydia_'))}
    # Original-game variants are outside this beautified expression task.
    for p in (ROOT / 'Textures/Items').glob('er_portrait_lydia_*vanilla.png'):
        protected[p] = sha(p)
    jobs_path = ART / 'generation_jobs.json'
    data = json.loads(jobs_path.read_text(encoding='utf-8'))
    aligned = {}
    pipeline_records = {}
    for job in data['jobs']:
        expression = job['expression']
        path = ART / 'masters' / f'lydia_face_{expression}.png'
        master = Image.open(path).convert('RGBA')
        assert master.size == master_size, 'Review new source framing: ' + expression
        bbox = master.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
        s, tx, ty = (transform[key] for key in ('s', 'tx', 'ty'))
        image, pipeline, _ = render(master, s * (bbox[3] - bbox[1]),
                                     tx + s * (bbox[0] + bbox[2]) / 2,
                                     ty + s * bbox[3], fringe=False)
        assert all(abs(pipeline['transform'][key] - transform[key]) < 1e-10 for key in transform)
        image.save(ART / 'layers' / f'{expression}_aligned.png')
        aligned[expression] = np.asarray(image)
        pipeline_records[expression] = dict(master=path.relative_to(ROOT).as_posix(),
                                             master_sha256=sha(path), pipeline=pipeline)
        (ART / 'prompts' / f'{expression}.txt').write_text(job['prompt'] + '\n', encoding='utf-8')
        job['status'] = 'installed_daily_and_date' if install else 'exported_daily_and_date'
        job['master'] = 'masters/' + path.name
    mask = face_mask()
    records, panels = [], {}
    base_records = {}
    for outfit, (base_relative, runtime_name) in BASES.items():
        base_path = DESIGN / base_relative
        base_image = Image.open(base_path).convert('RGBA')
        base = np.asarray(base_image)
        assert base_image.size == (288, 212)
        panels[outfit] = [('neutral', base_image)]
        base_out = ART / 'portraits' / runtime_name
        shutil.copy2(base_path, base_out)
        if install:
            shutil.copy2(base_out, ROOT / 'Textures/Items' / runtime_name)
            shutil.copy2(base_out, ROOT / 'Art/dual-romance-v0.3/portraits' / runtime_name)
        base_records[outfit] = {'source': base_path.relative_to(ROOT).as_posix(), 'sha256': sha(base_path),
                               'runtime_file': runtime_name, 'neutral_eye_white_pixels': eye_white_pixels(base)}
        for expression in EXPRESSIONS:
            face = aligned[expression]
            guard = jaw_guard(base_image.size, 'lydia_' + outfit)
            weight = mask * guard * ((base[:, :, 3] == 255) & (face[:, :, 3] == 255))
            result = base.copy()
            result[:, :, :3] = np.floor(base[:, :, :3] * (1 - weight[:, :, None]) +
                                       face[:, :, :3] * weight[:, :, None] + .5).astype(np.uint8)
            result = bleed_transparent_rgb(result)
            unchanged = (weight == 0) & (base[:, :, 3] == 255)
            assert np.array_equal(result[:, :, 3], base[:, :, 3])
            assert np.array_equal(result[unchanged], base[unchanged]), 'Body drift: ' + expression
            jaw_and_neck = (guard == 0) & (base[:, :, 3] == 255)
            assert np.array_equal(result[jaw_and_neck], base[jaw_and_neck])
            name = f'er_expr_lydia_{outfit}_{expression}.png'
            path = ART / 'portraits' / name
            image = Image.fromarray(result)
            image.save(path)
            assert image.mode == 'RGBA' and set(np.unique(result[:, :, 3])) == {0,255}
            if install:
                shutil.copy2(path, ROOT / 'Textures/Items' / name)
            count = eye_white_pixels(result)
            if expression in ('smile', 'flustered'):
                assert count > base_records[outfit]['neutral_eye_white_pixels'], 'Review eyelid aperture: ' + name
            records.append({'file': name, 'outfit': outfit, 'expression': expression,
                            'sha256': sha(path), 'size': [288,212],
                            'alpha_identical_to_neutral': True, 'visible_body_unchanged': True,
                            'jaw_and_neck_pixels_identical_to_neutral': True,
                            'jaw_guard_profile': 'lydia_' + outfit,
                            'face_changed_pixels': int((np.any(result[:,:,:3] != base[:,:,:3], axis=2) & (base[:,:,3] == 255)).sum()),
                            'eye_white_pixels_diagnostic': count, 'installed': install})
            panels[outfit].append((expression, image))
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 16)
    sheet = Image.new('RGB', (6 * 288, 2 * 240), (37,44,53))
    draw = ImageDraw.Draw(sheet)
    for row, (outfit, values) in enumerate(panels.items()):
        for col, (expression, image) in enumerate(values):
            bg = Image.new('RGBA', image.size, (37,44,53,255))
            bg.alpha_composite(image)
            sheet.paste(bg.convert('RGB'), (col * 288, row * 240 + 28))
            draw.text((col * 288 + 5, row * 240 + 5), outfit + ' / ' + expression, font=font, fill='white')
        crop = sheet.crop((0, row * 240, 6 * 288, row * 240 + 240))
        crop.save(ART / 'previews' / f'{outfit}-native.png')
        crop.resize((3456,480), Image.Resampling.NEAREST).save(ART / 'previews' / f'{outfit}-2x.png')
        # Tight per-face panels, with the same crop in each original frame.
        face_sheet = Image.new('RGB', (6 * 78 * 4, 65 * 4), (37,44,53))
        for col in range(6):
            face_crop = crop.crop((col*288+105, 28+91, col*288+183, 28+156))
            face_sheet.paste(face_crop.resize((312,260), Image.Resampling.NEAREST), (col*312,0))
        face_sheet.save(ART / 'previews' / f'{outfit}-faces-4x.png')
    sheet.save(ART / 'previews/contact-sheet-native.png')
    assert all(sha(path) == before for path, before in protected.items()), 'Out-of-scope artwork changed'
    report = {'date': '2026-11-10', 'tool': 'image_gen.imagegen', 'installed': install,
              'source_approval': approval['status'], 'approved_master_sha256': sha(source),
              'fixed_transform': transform, 'base_portraits': base_records,
              'generated_faces': pipeline_records, 'expression_assets': records,
              'image_generation_calls': 5, 'native_expression_assets': len(records),
              'method': 'AI expression editing followed by exact B/detail sampling and fixed facial-layer assembly; no iris-area fitting.',
              'out_of_scope_art_unchanged': True}
    (ART / 'asset_validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    jobs_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'expressions': len(records), 'installed': install,
                      'eye_white_pixels': {o: {'neutral': base_records[o]['neutral_eye_white_pixels'],
                          **{r['expression']: r['eye_white_pixels_diagnostic'] for r in records if r['outfit'] == o}} for o in BASES},
                      'all_alpha_and_body_locked': True, 'out_of_scope_art_unchanged': True}, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    export(parser.parse_args().install)
