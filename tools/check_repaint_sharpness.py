"""Global sharpness gate for generated/edited character art.

Normalizes the longest edge to 1024 px, converts to grayscale, and measures
Laplacian variance and Sobel gradient strength. If the image is too soft, the
image must be redrawn from source references instead of patched repeatedly.
"""
from pathlib import Path
import argparse
import json
import sys

from PIL import Image, ImageFilter
import numpy as np

LAP_VAR_MIN = 270.0
SOBEL_MEAN_MIN = 3.4


def measure(path):
    im = Image.open(path).convert('L')
    scale = 1024 / max(im.size)
    im = im.resize((round(im.width * scale), round(im.height * scale)), Image.Resampling.LANCZOS)
    a = np.asarray(im, dtype=np.float32)
    lap = 4 * a[1:-1, 1:-1] - a[:-2, 1:-1] - a[2:, 1:-1] - a[1:-1, :-2] - a[1:-1, 2:]
    gx = a[1:-1, 2:] - a[1:-1, :-2]
    gy = a[2:, 1:-1] - a[:-2, 1:-1]
    sob = np.sqrt(gx * gx + gy * gy)
    hf = a - np.asarray(im.filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32)
    lap_var = float(lap.var())
    sobel_mean = float(sob.mean())
    result = {
        'file': str(path),
        'size': list(Image.open(path).size),
        'lap_var': round(lap_var, 1),
        'sobel_mean': round(sobel_mean, 2),
        'sobel_p95': round(float(np.percentile(sob, 95)), 1),
        'hf_std': round(float(hf.std()), 1),
        'hf_ratio': round(float(hf.var() / (a.var() + 1e-6)), 4),
        'thresholds': {'lap_var_min': LAP_VAR_MIN, 'sobel_mean_min': SOBEL_MEAN_MIN},
    }
    result['verdict'] = 'PASS' if lap_var >= LAP_VAR_MIN and sobel_mean >= SOBEL_MEAN_MIN else 'FAIL_BLUR'
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('images', nargs='+')
    parser.add_argument('--json-out', default='')
    args = parser.parse_args()
    results = [measure(Path(p)) for p in args.images]
    payload = {'metric': 'normalized_1024_laplacian_sobel', 'results': results}
    text = json.dumps(payload, ensure_ascii=False, indent=2)
    print(text)
    if args.json_out:
        Path(args.json_out).write_text(text + '\n', encoding='utf-8')
    return 1 if any(r['verdict'] != 'PASS' for r in results) else 0


if __name__ == '__main__':
    raise SystemExit(main())
