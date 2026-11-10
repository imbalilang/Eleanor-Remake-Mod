"""Export the existing masters with face framing from Maiden's Pawnshop.

Reference portraits are not needed at export time and are never shipped.
"""
from pathlib import Path
import argparse
import hashlib
import json
from PIL import Image
from portrait_resampling import render_portrait

ROOT = Path(__file__).resolve().parents[1]
PROFILES = {
    'gala': {
        'source': 'Art/gala/masters/eleanor_gala_beauty_master.png',
        'file': 'Textures/Items/er_portrait_eleanor_gala_female_beauty.png',
        'manifest': 'Art/gala/asset_validation.json',
        'size': (171, 209), 'position': (58, 58),
        'previous_size': (149, 182), 'bbox': (59, 58, 224, 212),
        # Approximate manual landmarks, not a face detector's exact boundaries.
        'landmarks': {'left_eye': (641, 494), 'right_eye': (774, 467),
                      'chin': (723, 583),
                      'left_cheek': (656, 526), 'right_cheek': (803, 526)},
    },
    'maid': {
        'source': 'Art/v0.5/masters/eleanor_maid_master.png',
        'file': 'Textures/Items/er_portrait_eleanor_maid_female_beauty.png',
        'manifest': 'Art/v0.5/beauty_export_validation.json',
        'size': (154, 182), 'position': (66, 52),
        'previous_size': (133, 158), 'bbox': (66, 52, 220, 212),
        'landmarks': {'left_eye': (650, 560), 'right_eye': (790, 529),
                      'chin': (724, 662),
                      'left_cheek': (667, 600), 'right_cheek': (818, 600)},
    },
}
REFERENCE = {
    'workshop': 3829696854, 'mod': 'cute_portraits', 'version': '1.0.3',
    'portrait': 'cp_portrait_eleanor1.png',
    'crosscheck': 'cp_portrait_eleanor_scav.png',
    'method': 'Approximate manual native-pixel landmarks; integer-zoom visual check',
    'landmarks': {'left_eye': [126, 118], 'right_eye': [155, 112],
                  'chin': [142, 138],
                  'left_cheek': [128, 125], 'right_cheek': [164, 125]},
}


def export(variant, resampling='box', output_dir=None):
    p = PROFILES[variant]
    source = ROOT / p['source']
    output = (Path(output_dir) / Path(p['file']).name).resolve() if output_dir else ROOT / p['file']
    with Image.open(source) as original:
        image = original.convert('RGBA')
    source_size = image.size
    canvas, bbox = render_portrait(image,p['size'],p['position'],resampling)
    sw, sh = bbox[2] - bbox[0], bbox[3] - bbox[1]
    width, height = p['size']
    pixels = list(canvas.get_flattened_data())
    colors = {px[:3] for px in pixels if px[3]}
    actual_bbox = canvas.getchannel('A').getbbox()
    assert actual_bbox and all(abs(a-b)<=1 for a,b in zip(actual_bbox,p['bbox'])), 'Unexpected framing shift'
    assert {px[3] for px in pixels} == {0, 255}
    assert len(colors) > 256, 'Keep source RGB; do not quantize to a small palette'
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    projected = {
        key: [round(p['position'][0] + (point[0] - bbox[0]) * width / sw, 2),
              round(p['position'][1] + (point[1] - bbox[1]) * height / sh, 2)]
        for key, point in p['landmarks'].items()
    }
    report = {
        'source': p['source'], 'file': str(output.relative_to(ROOT)).replace('\\','/') if output.is_relative_to(ROOT) else str(output),
        'revision': '2026-11-10 supplied source/export pair calibration; original masters, area sampling, full RGB',
        'source_size': list(source_size), 'source_opaque_bbox': list(bbox),
        'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'export': {'resampling': 'BOX area averaging in premultiplied alpha; single resize' if resampling=='box'
                       else 'NEAREST pixel-centre sampling, no interpolation',
                   'palette': 'Full RGB; no colour quantization',
                   'dither': 'NONE', 'alpha': 'threshold 128, binary silhouette',
                   'game_filter': 'Point (native ModLoader)'},
        'framing': {'reference': REFERENCE, 'scaled_figure_size': list(p['size']),
                    'position': list(p['position']),
                    'previous_scaled_figure_size': list(p['previous_size']),
                    'scale_relative_to_previous': height / p['previous_size'][1],
                    'bottom_crop_pixels': p['position'][1] + height - 212,
                    'source_landmarks_approx': p['landmarks'],
                    'projected_landmarks_approx': projected,
                    'intent': 'Match face/eye/chin scale and position to actual Eleanor reference'},
        'size': list(canvas.size), 'alpha_bbox': list(actual_bbox),
        'opaque_colors': len(colors), 'alpha_values': [0, 255],
        'bytes': output.stat().st_size,
        'sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
        'visual_review': 'Offline native and integer zoom comparison; in-game verification pending',
    }
    if variant == 'gala':
        report['vanilla'] = 'Uses native eleanor1. No new vanilla sprite.'
    report_path = output.with_suffix('.json') if output_dir else ROOT / p['manifest']
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False))
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--variant', choices=['gala', 'maid', 'all'], default='all')
    parser.add_argument('--resampling', choices=['box','nearest'], default='box')
    parser.add_argument('--output-dir', type=Path, help='Write review candidates and sidecars here without changing production sprites/manifests')
    args = parser.parse_args()
    for name in PROFILES if args.variant == 'all' else [args.variant]:
        export(name,args.resampling,args.output_dir)
