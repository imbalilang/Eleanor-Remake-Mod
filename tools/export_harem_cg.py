"""Export the generated shared-romance ending CG without portrait framing."""
from pathlib import Path
import argparse
import hashlib
import json

import numpy as np
from PIL import Image

from author_portrait_pipeline import exact_area_sample, enhance_details

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/harem-ending-2026-11-10'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export(revision='v3'):
    # This is a review export. Generating the first CG candidate does not replace
    # the existing runtime placeholder or change previously installed artwork.
    protected = {p: sha(p) for p in (ROOT / 'Textures').rglob('*.png')}
    job = json.loads((ART / f'generation_job_{revision}.json').read_text(encoding='utf-8'))
    source = ART / job['master']
    assert sha(source) == job['source_sha256'], 'Master changed; review source provenance'
    master = Image.open(source).convert('RGBA')
    rgba = np.asarray(master)
    assert np.all(rgba[:, :, 3] == 255), 'CG must be an opaque full scene'
    width, height = 960, 540
    # Cover the exact scene canvas isotropically. Generator dimensions may be a
    # fraction away from 16:9; centre-crop that fractional strip, never stretch.
    scale = max(width / master.width, height / master.height)
    tx, ty = (width - master.width * scale) / 2, (height - master.height * scale) / 2
    sampled, _ = exact_area_sample(rgba, (width, height), scale, tx, ty)
    pixels, details = enhance_details(sampled)
    assert np.all(pixels[:, :, 3] == 255)
    assert master.width > width and master.height > height
    (ART / 'endings').mkdir(exist_ok=True)
    previews = ART / 'previews' / revision
    previews.mkdir(parents=True, exist_ok=True)
    output = ART / 'endings' / f'er_ending_harem_cg_{revision}.png'
    native = Image.fromarray(pixels)
    native.save(output)
    native.resize((width * 2, height * 2), Image.Resampling.NEAREST).save(
        previews / 'ending-harem-native-2x.png')
    # A concise face crop at native pixels and integer zoom aids expression QA.
    face_crop = native.crop((260, 90, 680, 270))
    face_crop.save(previews / 'faces-native.png')
    face_crop.resize((840, 360), Image.Resampling.NEAREST).save(previews / 'faces-2x.png')
    assert all(sha(p) == before for p, before in protected.items())
    report = {
        'tool': 'image_gen.imagegen', 'status': 'candidate_for_review', 'revision': revision,
        'source': job['master'], 'source_sha256': sha(source),
        'source_size': list(master.size),
        'output': output.relative_to(ART).as_posix(), 'output_sha256': sha(output),
        'output_size': [width, height], 'mode': native.mode, 'alpha_values': [255],
        'pipeline': 'Verified author exact area sampling and one B/detail pass for a complete opaque scene',
        'transform': {'scale': scale, 'tx': tx, 'ty': ty},
        'detail_pixels': int(details), 'portrait_top_row_added': False,
        'fixed_palette': False, 'dither': False,
        'all_runtime_art_unchanged': True,
        'runtime_placeholder_sha256': protected[ROOT / 'Textures/Items/er_ending_harem_cg.png'],
        'in_game_visual_test_performed': False, 'published': False,
    }
    (ART / f'asset_validation_{revision}.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    if revision == 'v3':
        (ART / 'asset_validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', choices=['v1', 'v2', 'v3'], default='v3')
    export(parser.parse_args().revision)
