"""Reproduce Sophia portraits through the verified author B/detail sampler."""
from pathlib import Path
import argparse
import hashlib
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from author_portrait_pipeline import render


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export(revision='v2'):
    suffix = {'v2': 'v2', 'v3': 'v3_upper', 'v4_intimate': 'v4_intimate',
              'v5_redesign': 'v5_redesign'}[revision]
    config = json.loads((HERE / f'framing_{revision}.json').read_text(encoding='utf-8'))
    master = HERE / config['master']
    source = Image.open(master).convert('RGBA')
    assert digest(master) == config['master_sha256'], 'Recalibrate after master changes'
    image, report, _ = render(source, **config['parameters'])
    repeated, _, _ = render(source, **config['parameters'])
    assert image.tobytes() == repeated.tobytes()
    pixels = np.asarray(image)
    assert image.size == (288, 212) and image.mode == 'RGBA'
    assert set(np.unique(pixels[:, :, 3])) == {0, 255}
    assert not pixels[0].any()
    target = HERE / {
        'v2': 'portraits/er_portrait_mother_sophia_beauty.png',
        'v3': 'portraits/er_portrait_mother_sophia_beauty_upper_v3.png',
        'v4_intimate': 'portraits/er_portrait_mother_sophia_home_beauty_v4.png',
        'v5_redesign': 'portraits/er_portrait_mother_sophia_home_beauty_v5.png',
    }[revision]
    target.parent.mkdir(exist_ok=True)
    image.save(target)
    font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 16)
    approval_path = HERE / 'approved/approval.json'
    approval = json.loads(approval_path.read_text(encoding='utf-8')) if approval_path.exists() else None
    approved = next((a for a in approval['assets'] if a['revision'] == revision), None) if approval else None
    if approved:
        assert digest(target) == approved['portrait_sha256'], 'Approved output changed'
    references = [
        ('Eleanor', ROOT / 'Textures/Items/er_expr_eleanor1_soft.png'),
        ('Lydia v4', ROOT / 'Textures/Items/er_portrait_lydia_female_beauty.png'),
        ('Sophia / approved' if approved else 'Sophia / candidate', target),
        ('Sophia / previous v4', HERE / 'portraits/er_portrait_mother_sophia_home_beauty_v4.png') if revision == 'v5_redesign' else
        ('Sophia / upper v3', HERE / 'portraits/er_portrait_mother_sophia_beauty_upper_v3.png') if revision == 'v4_intimate' else
        ('Sophia / previous v2', HERE / 'portraits/er_portrait_mother_sophia_beauty.png') if revision == 'v3' else
        ('Mother / old runtime', ROOT / 'Textures/Items/er_portrait_mother_female_beauty.png'),
    ]
    sheet = Image.new('RGB', (1152, 240), '#252c35')
    draw = ImageDraw.Draw(sheet)
    hashes = {}
    for col, (title, path) in enumerate(references):
        portrait = Image.open(path).convert('RGBA')
        assert portrait.size == (288, 212)
        hashes[path.relative_to(ROOT).as_posix()] = digest(path)
        bg = Image.new('RGBA', portrait.size, '#252c35')
        bg.alpha_composite(portrait)
        sheet.paste(bg.convert('RGB'), (col * 288, 28))
        draw.text((col * 288 + 10, 4), title, font=font, fill='white')
    sheet.save(HERE / f'previews/comparison_{suffix}_native.png')
    sheet.resize((2304, 480), Image.Resampling.NEAREST).save(HERE / f'previews/comparison_{suffix}_2x.png')
    mattes = Image.new('RGB', (864, 240), '#252c35')
    draw = ImageDraw.Draw(mattes)
    for col, (title, colour) in enumerate((('White', 'white'), ('Black', 'black'), ('Grey', '#252c35'))):
        bg = Image.new('RGBA', image.size, colour)
        bg.alpha_composite(image)
        mattes.paste(bg.convert('RGB'), (col * 288, 28))
        draw.text((col * 288 + 10, 4), title, font=font, fill='white')
    mattes.save(HERE / f'previews/mattes_{suffix}_native.png')
    mattes.resize((2592, 720), Image.Resampling.NEAREST).save(HERE / f'previews/mattes_{suffix}_3x.png')
    s, tx, ty = (report['transform'][k] for k in ('s', 'tx', 'ty'))
    projected = {key: [round(s*x+tx, 2), round(s*y+ty+1, 2)]
                 for key, (x, y) in config['manual_landmarks_source'].items()}
    baseline = json.loads((HERE / 'runtime_before.json').read_text(encoding='utf-8'))
    runtime = {p.relative_to(ROOT).as_posix(): digest(p) for p in (ROOT / 'Textures/Items').glob('*.png')}
    expected = dict(baseline)
    installed = False
    if approval and any(runtime.get(a['runtime_path']) == a['portrait_sha256'] for a in approval['assets']):
        installed = True
        expected.update({a['runtime_path']: a['portrait_sha256'] for a in approval['assets']})
    changed = sorted(p for p in set(runtime) | set(expected) if runtime.get(p) != expected.get(p))
    assert not changed, f'Runtime changed outside the approved installation: {changed}'
    old_report_path = HERE / f'validation_{revision}.json'
    previous = json.loads(old_report_path.read_text(encoding='utf-8')) if old_report_path.exists() else {}
    report.update(
        master=config['master'], master_sha256=digest(master),
        prompt=config.get('prompt', 'prompts/mother_beauty_v2.txt'),
        prompt_sha256=digest(HERE / config.get('prompt', 'prompts/mother_beauty_v2.txt')),
        output=target.relative_to(HERE).as_posix(), output_sha256=digest(target),
        manual_landmarks_source=config['manual_landmarks_source'],
        projected_landmarks_native=projected,
        landmark_method='Manual visual estimates on the generated master; not automated anatomy measurements',
        reference_sha256=hashes, repeat_identical=True,
        runtime_replaced=installed and approved is not None,
        runtime_baseline_unchanged=runtime == baseline,
        unrelated_runtime_unchanged=True, published=False,
        status='User-approved final; installed in workspace' if installed and approved else
               'User-approved final; pending installation' if approved else 'Historical design candidate',
        actual_post_receipt='request_receipts.jsonl',
    )
    for key in ('visual_review', 'all_uploads_precompressed', 'actual_serialized_post_body_bytes', 'same_transform_as_upper_v3'):
        if key in previous:
            report[key] = previous[key]
    if approved:
        report['approval'] = 'approved/approval.json'
        report['visual_review']['remaining_limitations'] = [
            'No in-game UI verification; restart is needed to load changed sprites and assembly',
            'Source waist, hands and low hair tie are outside the calibrated runtime crop',
            'Larger bust than Lydia was not independently measured; existing v3/v4 accepted as final by user',
        ]
    (HERE / f'validation_{revision}.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({k:report[k] for k in ('output', 'output_size', 'output_bbox', 'projected_landmarks_native', 'repeat_identical', 'runtime_baseline_unchanged')}, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', choices=['v2', 'v3', 'v4_intimate', 'v5_redesign'], default='v2')
    export(parser.parse_args().revision)
