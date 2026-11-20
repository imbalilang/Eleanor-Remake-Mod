"""Build a 288x212 in-game portrait from a bust-first chroma master.

The master must already be a 1536x1152 bust portrait, not a full-body image.
Use a solid #FF00FF background wherever the final image should be transparent.
Profiles:
  cp: match the built-in CutePortraits framing (Ht=150)
  qq: match the full-height QQ card framing (Ht=204)
"""
from pathlib import Path
import argparse
import hashlib
import json
import sys

from PIL import Image, ImageDraw, ImageFont
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from author_portrait_pipeline import render

PROFILES = {
    'cp': {'visible_height': 150, 'centre_x': 144.5, 'bottom': 211},
    'qq': {'visible_height': 204, 'centre_x': 144.5, 'bottom': 211},
}
CP_REFERENCE = ROOT / 'Textures/Items/cp_portrait_eleanor1.png'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def metrics(image):
    im = image.convert('RGBA')
    a = np.array(im)
    alpha = a[:, :, 3]
    bbox = list(im.getchannel('A').getbbox())
    return {
        'size': list(im.size),
        'bbox': bbox,
        'top_margin_pct': round(bbox[1] / im.height * 100, 2),
        'left_margin_pct': round(bbox[0] / im.width * 100, 2),
        'right_margin_pct': round((im.width - bbox[2]) / im.width * 100, 2),
        'visible_height_pct': round((bbox[3] - bbox[1]) / im.height * 100, 2),
        'alpha_values': sorted(np.unique(alpha).tolist()),
        'opaque_colours': len(np.unique(a[alpha == 255, :3], axis=0)),
    }


def eye_center(image):
    im = image.convert('RGBA')
    a = np.array(im)
    r = a[:, :, 0].astype(int)
    g = a[:, :, 1].astype(int)
    b = a[:, :, 2].astype(int)
    alpha = a[:, :, 3]
    mask = (g > r + 10) & (g > b + 5) & (g > 60) & (alpha > 0)
    mask[170:, :] = False
    ys, xs = np.where(mask)
    if not len(xs):
        return None
    return {
        'center': [round(float(xs.mean()), 2), round(float(ys.mean()), 2)],
        'bbox': [int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)],
    }


def preview_sheet(result, output_path):
    stem = output_path.stem
    preview_dir = output_path.parent / 'previews'
    preview_dir.mkdir(parents=True, exist_ok=True)
    for name, bg in [('white', (255, 255, 255, 255)), ('black', (0, 0, 0, 255)), ('gray', (37, 44, 53, 255))]:
        canvas = Image.new('RGBA', result.size, bg)
        canvas.alpha_composite(result)
        canvas.convert('RGB').resize((result.width * 4, result.height * 4), Image.Resampling.NEAREST).save(
            preview_dir / f'{stem}_{name}_4x.png', optimize=True)
    edge = Image.new('RGBA', result.size, (0, 0, 0, 255))
    edge.alpha_composite(result)
    edge.convert('RGB').crop((35, 0, 253, 150)).resize((218 * 6, 150 * 6), Image.Resampling.NEAREST).save(
        preview_dir / f'{stem}_head_black_6x.png', optimize=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--profile', choices=sorted(PROFILES), default='cp')
    parser.add_argument('--runtime-key', default='')
    parser.add_argument('--no-magenta-key', action='store_true')
    parser.add_argument('--no-fringe', action='store_true')
    parser.add_argument('--no-detail', action='store_true')
    parser.add_argument('--archive-dir', default='')
    args = parser.parse_args()

    source_path = Path(args.source)
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    source = Image.open(source_path).convert('RGBA')
    profile = PROFILES[args.profile]
    result, report, stages = render(
        source, visible_height=profile['visible_height'], centre_x=profile['centre_x'],
        bottom=profile['bottom'], canvas_size=(288, 211), add_top_row=True,
        magenta_key=not args.no_magenta_key, fringe=not args.no_fringe,
        detail=not args.no_detail, bleed=True, coverage_threshold=0.35)
    result.save(output_path, optimize=True)
    stage_dir = output_path.parent / (output_path.stem + '_stages')
    stage_dir.mkdir(parents=True, exist_ok=True)
    for name, image in stages.items():
        image.save(stage_dir / f'{name}.png', optimize=True)

    # Archive the full-resolution transparent master, analogous to the QQ RGBA
    # originals. This is the keyed/chroma-cleaned image before B/detail sampling.
    archive_dir = Path(args.archive_dir) if args.archive_dir else output_path.parent / 'transparent_masters'
    archive_dir.mkdir(parents=True, exist_ok=True)
    archive_master = stages.get('derived_fringe_master', stages['clean_master']).convert('RGBA')
    archive_path = archive_dir / f'{output_path.stem}_transparent_master.png'
    archive_master.save(archive_path, optimize=True)
    archive_meta = {
        'file': str(archive_path),
        'sha256': sha(archive_path),
        'size': list(archive_master.size),
        'mode': archive_master.mode,
        'alpha_values': sorted(np.unique(np.array(archive_master)[:, :, 3]).tolist()),
        'alpha_bbox': list(archive_master.getchannel('A').getbbox()),
        'source': str(source_path),
        'runtime_key': args.runtime_key,
        'profile': args.profile,
        'note': 'Full-resolution transparent portrait master after magenta key and fringe cleanup; archive before B/detail export.',
    }
    archive_path.with_suffix('.json').write_text(json.dumps(archive_meta, ensure_ascii=False, indent=2), encoding='utf-8')
    preview_dir = output_path.parent / 'previews'
    preview_dir.mkdir(parents=True, exist_ok=True)
    black = Image.new('RGBA', archive_master.size, (0, 0, 0, 255))
    black.alpha_composite(archive_master)
    black.convert('RGB').resize((archive_master.width * 2, archive_master.height * 2), Image.Resampling.NEAREST).save(
        preview_dir / f'{output_path.stem}_transparent_master_black_2x.png', optimize=True)
    preview_sheet(result, output_path)

    cp_metrics = metrics(Image.open(CP_REFERENCE))
    record = {
        'status': 'candidate_not_installed',
        'runtime_key': args.runtime_key,
        'profile': args.profile,
        'source': {'file': str(source_path), 'sha256': sha(source_path), 'size': list(source.size)},
        'parameters': {'visible_height': profile['visible_height'], 'centre_x': profile['centre_x'],
                       'bottom': profile['bottom'], 'canvas_size': [288, 211], 'add_top_row': True,
                       'magenta_key': not args.no_magenta_key, 'fringe': not args.no_fringe,
                       'detail': not args.no_detail, 'bleed': True, 'coverage_threshold': 0.35},
        'sampler': report,
        'output': {'file': str(output_path), 'sha256': sha(output_path),
                   'bytes': output_path.stat().st_size, 'metrics': metrics(result),
                   'eye_center': eye_center(result)},
        'archive_transparent_master': archive_meta,
        'cp_reference': {'file': str(CP_REFERENCE), 'metrics': cp_metrics, 'eye_center': eye_center(Image.open(CP_REFERENCE))},
        'notes': [
            'Generate a bust portrait directly; do not complete the full body for a portrait asset.',
            'The chroma master background must be solid #FF00FF.',
            'cp profile uses Ht=150 to match the built-in CutePortraits portrait; qq profile uses Ht=204 for full-height QQ cards.',
        ],
    }
    report_path = output_path.with_suffix('.report.json')
    report_path.write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(record, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
