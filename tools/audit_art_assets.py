"""Inventory artwork and record geometry issues without modifying PNGs."""
from pathlib import Path
import argparse
import hashlib
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/author-pipeline-validation-2026-11-10'
BEAUTY = Path('D:/Program Files (x86)/Steam/steamapps/workshop/content/4348910/3829696854/Textures/Items')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main(output_dir=OUT):
    output_dir.mkdir(parents=True, exist_ok=True)
    files = sorted((ROOT / 'Textures').rglob('*.png'))
    before = {path: sha(path) for path in files}
    items, expression_comparisons = [], []
    groups = {
        'eleanor1': BEAUTY / 'cp_portrait_eleanor1.png',
        'eleanor_scav': BEAUTY / 'cp_portrait_eleanor_scav.png',
        'eleanor_maid': ROOT / 'Textures/Items/er_portrait_eleanor_maid_female_beauty.png',
        'lydia_daily': ROOT / 'Textures/Items/er_portrait_lydia_female_beauty.png',
        'lydia_date': ROOT / 'Textures/Items/er_portrait_lydia_date_female_beauty.png',
    }
    for path in files:
        image = Image.open(path)
        rgba = np.asarray(image.convert('RGBA'))
        alpha = rgba[:, :, 3] == 255
        family = ('expression' if path.stem.startswith('er_expr_') else
                  'portrait' if path.stem.startswith('er_portrait_') else
                  'ending_cg' if path.stem.startswith('er_ending_') else 'item')
        record = {'file': path.relative_to(ROOT).as_posix(), 'sha256': before[path],
                  'family': family, 'size': list(image.size), 'mode': image.mode,
                  'alpha_values': np.unique(rgba[:, :, 3]).tolist(),
                  'alpha_bbox': list(image.convert('RGBA').getchannel('A').getbbox()),
                  'opaque_pixels': int(alpha.sum()), 'opaque_fraction': float(alpha.mean()), 'flags': []}
        if family == 'expression':
            if image.size != (288, 212):
                record['flags'].append('legacy_211_requires_base_family_alignment_review')
            base_path = next((v for k, v in groups.items() if path.stem.startswith('er_expr_' + k + '_')), None)
            if base_path is not None and base_path.exists():
                base = Image.open(base_path).convert('RGBA')
                candidate = image.convert('RGBA')
                # Diagnostic only: add the missing top row in memory, then
                # compare. This is not an automatic repair of the delivered PNG.
                if candidate.size == (288, 211):
                    padded = Image.new('RGBA', (288, 212))
                    padded.paste(candidate, (0, 1))
                    candidate = padded
                if candidate.size == base.size:
                    a, b = np.asarray(candidate)[:, :, 3] == 255, np.asarray(base)[:, :, 3] == 255
                    diff = int((a != b).sum())
                    record['base_comparison'] = {'reference': str(base_path), 'reference_sha256': sha(base_path),
                                                 'padding_applied_in_memory_only': image.height == 211,
                                                 'alpha_different_pixels': diff,
                                                 'silhouette_iou': float((a & b).sum() / (a | b).sum())}
                    if diff:
                        record['flags'].append('expression_silhouette_differs_from_neutral_base')
                    if path.stem.endswith(('_angry', '_smile')):
                        expression_comparisons.append((path.stem, base, candidate))
            else:
                record['flags'].append('local_neutral_reference_unavailable')
        if family == 'ending_cg' and record['opaque_fraction'] < .01:
            record['flags'].append('sparse_placeholder_not_finished_scene_cg')
        items.append(record)
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 14)
    sheet = Image.new('RGB', (864, len(expression_comparisons) * 240), (37, 44, 53))
    draw = ImageDraw.Draw(sheet)
    for row, (name, base, expression) in enumerate(expression_comparisons):
        a, b = np.asarray(base), np.asarray(expression)
        delta = np.zeros_like(a)
        delta[(a[:, :, 3] == 255) & (b[:, :, 3] != 255)] = (255, 80, 70, 255)
        delta[(a[:, :, 3] != 255) & (b[:, :, 3] == 255)] = (65, 210, 255, 255)
        panels = [('Neutral base', base), (name.replace('er_expr_', ''), expression), ('Red removed / blue added', Image.fromarray(delta))]
        for col, (title, im) in enumerate(panels):
            bg = Image.new('RGBA', im.size, (37, 44, 53, 255))
            bg.alpha_composite(im)
            sheet.paste(bg.convert('RGB'), (col * 288, row * 240 + 28))
            draw.text((col * 288 + 4, row * 240 + 5), title, font=font, fill='white')
    if expression_comparisons:
        sheet.save(output_dir / 'expression-alignment-audit.png')
        sheet.resize((sheet.width * 2, sheet.height * 2), Image.Resampling.NEAREST).save(output_dir / 'expression-alignment-audit-2x.png')
    result = {'date': '2026-11-10', 'scope': 'Offline inventory; no artwork or runtime changes',
              'total_png': len(items), 'counts': {key: sum(r['family'] == key for r in items)
                                                  for key in ('portrait', 'expression', 'ending_cg', 'item')},
              'flagged': sum(bool(r['flags']) for r in items), 'assets': items}
    assert all(sha(path) == value for path, value in before.items())
    result['all_runtime_png_unchanged'] = True
    (output_dir / 'asset_inventory.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: result[k] for k in ('total_png', 'counts', 'flagged', 'all_runtime_png_unchanged')}))
    for name in ('eleanor1', 'eleanor_scav', 'eleanor_maid', 'lydia_daily', 'lydia_date'):
        rows = [r for r in items if r['file'].split('/')[-1].startswith('er_expr_' + name + '_')]
        print(name, 'files', len(rows), 'alpha differences', sorted({r.get('base_comparison', {}).get('alpha_different_pixels', -1) for r in rows}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, default=OUT)
    main(parser.parse_args().output_dir)
