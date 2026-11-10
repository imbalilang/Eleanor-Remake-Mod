"""Export the generated opaque CG with one isotropic area resample.

No portrait crop rules, palette reduction, dither, sharpening or runtime install.
The provider's original PNG is preserved byte-for-byte.
"""
from pathlib import Path
import argparse
import hashlib
import json

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parent


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def weights(source_count, target_count, start, stop):
    edges = np.linspace(start, stop, target_count + 1)
    left = np.arange(source_count, dtype=np.float64)
    overlap = np.maximum(
        0.0,
        np.minimum(edges[1:, None], left[None, :] + 1.0)
        - np.maximum(edges[:-1, None], left[None, :]),
    )
    return overlap / (edges[1:] - edges[:-1])[:, None]


def area_sample(source, target, box):
    height, width = source.shape[:2]
    wx = weights(width, target[0], box[0], box[2])
    wy = weights(height, target[1], box[1], box[3])
    horizontal = np.einsum('hsc,ws->hwc', source.astype(np.float64), wx)
    sampled = np.einsum('yh,hwc->ywc', wy, horizontal)
    return Image.fromarray(np.clip(np.floor(sampled + 0.5), 0, 255).astype(np.uint8))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--revision', choices=['v2', 'v3'], default='v2')
    revision = parser.parse_args().revision
    master = ROOT / 'masters' / f'ending_revolution_master_{revision}.png'
    master_hash = sha256(master)
    source_image = Image.open(master).convert('RGB')
    width, height = source_image.size
    # Centre crop to 16:9 without stretching; record the exact crop below.
    crop_width = min(width, height * 16 / 9)
    crop_height = crop_width * 9 / 16
    crop = ((width - crop_width) / 2, (height - crop_height) / 2,
            (width + crop_width) / 2, (height + crop_height) / 2)
    source = np.asarray(source_image)
    endings = ROOT / 'endings'
    previews = ROOT / 'previews' / revision
    endings.mkdir(parents=True, exist_ok=True)
    previews.mkdir(parents=True, exist_ok=True)

    records = []
    for size, filename in [
        ((640, 360), f'er_ending_revolution_cg_{revision}_native640.png'),
        ((960, 540), f'er_ending_revolution_cg_{revision}.png'),
    ]:
        image = area_sample(source, size, crop)
        destination = endings / filename
        image.save(destination)
        records.append({'path': str(destination.relative_to(ROOT)),
                        'size': list(size), 'mode': image.mode,
                        'sha256': sha256(destination)})
        if size == (640, 360):
            image.resize((1280, 720), Image.Resampling.NEAREST).save(
                previews / 'ending_revolution_native640_2x.png')
            # Local reference comparison contains native-game art; keep ignored.
            reference = Image.open(ROOT / 'references_local' / 'wall_36.png').convert('RGB')
            comparison = Image.new('RGB', (1280, 360))
            comparison.paste(reference, (0, 0))
            comparison.paste(image, (640, 0))
            comparison.save(ROOT / 'references_local' / f'native_ending_vs_revolution_{revision}.png')

    assert sha256(master) == master_hash
    report = {
        'master': str(master.relative_to(ROOT)), 'master_size': [width, height],
        'master_sha256': master_hash, 'crop_box_half_open_source_pixels': crop,
        'method': 'Separable exact rectangular overlap area average in 8-bit RGB; floor(v+0.5)',
        'uniform_scale': True, 'palette_reduction': False, 'added_dither': False,
        'sharpening': False, 'master_modified': False, 'runtime_installation': False,
        'outputs': records,
    }
    (ROOT / f'validation_{revision}.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=True))


if __name__ == '__main__':
    main()
