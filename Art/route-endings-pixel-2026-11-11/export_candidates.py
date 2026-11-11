"""Export both new CG candidates using the selected revolutionary CG's sampler.

Preserves provider masters and existing runtime art; no palette reduction,
sharpening, dither, portrait framing parameters or automatic installation.
"""
from pathlib import Path
import hashlib
import importlib.util
import json

import numpy as np
from PIL import Image

BASE = Path(__file__).resolve().parent
PROJECT = BASE.parents[1]
sampler_path = PROJECT / 'Art/revolution-ending-2026-11-11/export_cg_v2.py'
spec = importlib.util.spec_from_file_location('selected_cg_sampler', sampler_path)
sampler = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sampler)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def describe(path):
    with Image.open(path) as image:
        image.load()
        size, mode = list(image.size), image.mode
    return {'path': path.relative_to(BASE).as_posix(), 'size': size,
            'mode': mode, 'sha256': digest(path), 'bytes': path.stat().st_size}


def main():
    # The user may install approved candidates after generation. Protect the
    # actual current runtime files across export while recognizing both states.
    protected = json.loads((BASE / 'runtime_before.json').read_text(encoding='utf-8'))
    installed = BASE / 'installation_0.89.json'
    approved = {}
    if installed.exists():
        approved = {Path(row['runtime']).as_posix(): row['after_sha256']
                    for row in json.loads(installed.read_text(encoding='utf-8'))['files']}
    for path, original in list(protected.items()):
        current = digest(PROJECT / path)
        assert current in (original, approved.get(Path(path).as_posix())), f'Unexpected runtime image: {path}'
        protected[path] = current
    reports = []
    for route in ['maid', 'harem']:
        master = BASE / 'masters' / f'ending_{route}_pixel_master_v1.png'
        original_hash = digest(master)
        with Image.open(master) as image:
            image.load()
            width, height = image.size
            if image.mode == 'RGBA':
                assert image.getchannel('A').getextrema() == (255, 255)
            source = np.asarray(image.convert('RGB'))
        crop_width = min(width, height * 16 / 9)
        crop_height = crop_width * 9 / 16
        crop = ((width - crop_width) / 2, (height - crop_height) / 2,
                (width + crop_width) / 2, (height + crop_height) / 2)
        outputs = []
        for size, suffix in [((640, 360), '_native640'), ((960, 540), '')]:
            result = sampler.area_sample(source, size, crop)
            out = BASE / 'endings' / f'er_ending_{route}_cg_pixel_v1{suffix}.png'
            result.save(out)
            outputs.append(describe(out))
            if size == (640, 360):
                result.resize((1280, 720), Image.Resampling.NEAREST).save(
                    BASE / 'previews' / f'ending_{route}_pixel_native640_2x.png')
        assert digest(master) == original_hash
        report = {'route': route, 'revision': 'pixel_v1', 'master': describe(master),
                  'crop_box_half_open_source_pixels': crop,
                  'method': 'Separable exact rectangular overlap area average in 8-bit RGB; floor(v+0.5)',
                  'sampler_source': sampler_path.relative_to(PROJECT).as_posix(),
                  'sampler_sha256': digest(sampler_path),
                  'isotropic': True, 'palette_reduction': False,
                  'added_dither': False, 'sharpening': False,
                  'master_modified': False, 'runtime_replaced': False,
                  'published': False, 'outputs': outputs}
        reports.append(report)
        job_path = BASE / f'generation_{route}_pixel_v1.json'
        job = json.loads(job_path.read_text(encoding='utf-8'))
        if not job.get('runtime_replaced'):
            job['status'] = 'generated_candidate'
        job.update(generation_attempts=1, successful_images=1, actual_master=describe(master))
        job_path.write_text(json.dumps(job, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for path, expected in protected.items():
        assert digest(PROJECT / path) == expected, f'Runtime image changed: {path}'
    (BASE / 'asset_validation.json').write_text(
        json.dumps({'runtime_existing_images_unchanged': True, 'routes': reports},
                   ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(reports, ensure_ascii=False))


if __name__ == '__main__':
    main()
