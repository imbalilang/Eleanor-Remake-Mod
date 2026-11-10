"""Literal offline implementation of Art/图片生成提示词.txt (B/detail).

Outputs are review artifacts. This module does not replace production portraits.
NumPy/Pillow are used for deterministic masks and exact separable pixel areas;
Pillow resize is deliberately not used for the core sampler.
"""
from collections import deque
import math

import numpy as np
from PIL import Image, ImageFilter


def inward_band(mask, radius):
    """Eight-neighbour distance <= radius from actual transparent pixels.

    A portrait cropped at the canvas bottom is not a transparency boundary.
    Replicate edge pixels here; only the area sampler treats exterior as empty.
    """
    padded = np.pad(np.uint8(mask) * 255, radius, mode='edge')
    eroded = np.asarray(Image.fromarray(padded).filter(ImageFilter.MinFilter(2 * radius + 1)))
    eroded = eroded[radius:-radius, radius:-radius] > 0
    return mask & ~eroded


def prepare_master(image, magenta_key=False):
    rgba = np.array(image.convert('RGBA'), copy=True)
    original_rgb = rgba[:, :, :3].copy()
    counts = {'strong': 0, 'weak_near_strong': 0, 'residual_edge': 0}
    if magenta_key:
        rgb = rgba[:, :, :3].astype(np.int16)
        r, g, b = (rgb[:, :, c] for c in range(3))
        strong = (np.minimum(r, b) > 160) & (g < 100) & (r - g > 100) & (b - g > 100)
        weak = (np.minimum(r, b) > 130) & (g < 100) & (r - g > 70) & (b - g > 70)
        near_strong = np.asarray(Image.fromarray(np.uint8(strong) * 255).filter(ImageFilter.MaxFilter(5))) > 0
        mask = rgba[:, :, 3] >= 128
        counts['strong'] = int((mask & strong).sum())
        counts['weak_near_strong'] = int((mask & ~strong & weak & near_strong).sum())
        mask &= ~(strong | (weak & near_strong))
        residual = inward_band(mask, 4) & (r > 110) & (b > 110) & (np.minimum(r, b) - g > 55)
        counts['residual_edge'] = int(residual.sum())
        mask &= ~residual
        rgba[:, :, 3] = np.uint8(mask) * 255
    assert np.array_equal(rgba[:, :, :3], original_rgb), 'Keying must not change master RGB'
    return Image.fromarray(rgba), counts


def remove_magenta_fringe(rgba, radius):
    result = rgba.copy()
    rgb = rgba[:, :, :3].astype(np.int16)
    r, g, b = (rgb[:, :, c] for c in range(3))
    affected = inward_band(rgba[:, :, 3] >= 128, radius)
    affected &= (np.minimum(r, b) - g > 24) & (r > 55) & (b > 55)
    result[affected, :3] = (48, 46, 50)
    return result, int(affected.sum())


def axis_weights(length, count, scale, translation):
    result = []
    for dst in range(count):
        a, b = (dst - translation) / scale, (dst + 1 - translation) / scale
        indices = np.arange(max(0, math.floor(a)), min(length, math.ceil(b)), dtype=np.int64)
        weights = np.maximum(0.0, np.minimum(b, indices + 1) - np.maximum(a, indices))
        result.append((indices, weights))
    return result


def exact_area_sample(rgba, canvas_size, scale, tx, ty, coverage_threshold=0.35):
    """Integrate each source pixel's actual rectangular overlap, in 8-bit RGB.

    Overlap factors into x and y weights, so the two sums are separable. No
    integer-size resize or linear-colour conversion is substituted.
    """
    if not (scale > 0 and math.isfinite(scale)):
        raise ValueError('Scale must be positive and finite')
    if not 0 < coverage_threshold <= 1:
        raise ValueError('Invalid coverage threshold')
    height, width = rgba.shape[:2]
    out_w, out_h = canvas_size
    foreground = rgba[:, :, 3] >= 128
    values = np.empty((height, width, 4), dtype=np.float64)
    values[:, :, :3] = rgba[:, :, :3] * foreground[:, :, None]
    values[:, :, 3] = foreground
    horizontal = np.zeros((height, out_w, 4), dtype=np.float64)
    for x, (indices, weights) in enumerate(axis_weights(width, out_w, scale, tx)):
        if len(indices):
            horizontal[:, x] = np.sum(values[:, indices] * weights[None, :, None], axis=1)
    sums = np.zeros((out_h, out_w, 4), dtype=np.float64)
    for y, (indices, weights) in enumerate(axis_weights(height, out_h, scale, ty)):
        if len(indices):
            sums[y] = np.sum(horizontal[indices] * weights[:, None, None], axis=0)
    area = (1 / scale) ** 2
    coverage = sums[:, :, 3] / area
    mask = (coverage >= coverage_threshold) & (sums[:, :, 3] > 0)
    out = np.zeros((out_h, out_w, 4), dtype=np.uint8)
    averages = np.zeros_like(sums[:, :, :3])
    np.divide(sums[:, :, :3], sums[:, :, 3, None], out=averages, where=sums[:, :, 3, None] > 0)
    out[mask, :3] = np.clip(np.floor(averages[mask] + 0.5), 0, 255).astype(np.uint8)
    out[mask, 3] = 255
    return out, coverage


def enhance_details(rgba):
    result = rgba.copy()
    rgb = rgba[:, :, :3].astype(np.float64)
    alpha = rgba[:, :, 3] == 255
    eligible = alpha[1:-1, 1:-1].copy()
    for neighbour in (alpha[:-2, 1:-1], alpha[2:, 1:-1], alpha[1:-1, :-2], alpha[1:-1, 2:]):
        eligible &= neighbour
    centre = rgb[1:-1, 1:-1]
    avg = (rgb[:-2, 1:-1] + rgb[2:, 1:-1] + rgb[1:-1, :-2] + rgb[1:-1, 2:]) / 4
    delta = centre - avg
    changed = eligible[:, :, None] & (np.abs(delta) > 6)
    enhanced = np.clip(np.floor(centre + 0.35 * delta + 0.5), 0, 255).astype(np.uint8)
    result[1:-1, 1:-1, :3] = np.where(changed, enhanced, rgba[1:-1, 1:-1, :3])
    return result, int(np.any(result[:, :, :3] != rgba[:, :, :3], axis=2).sum())


def bleed_transparent_rgb(rgba):
    result = rgba.copy()
    visited = result[:, :, 3] == 255
    queue = deque(map(tuple, np.argwhere(visited)))
    height, width = visited.shape
    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < height and 0 <= nx < width and not visited[ny, nx]:
                visited[ny, nx] = True
                result[ny, nx, :3] = result[y, x, :3]
                queue.append((ny, nx))
    assert np.array_equal(result[:, :, 3], rgba[:, :, 3])
    return result


def render(image, visible_height, centre_x, bottom, canvas_size=(288, 211),
           add_top_row=True, magenta_key=False, fringe=True, detail=True,
           bleed=True, coverage_threshold=0.35):
    clean, key_counts = prepare_master(image, magenta_key)
    rgba = np.asarray(clean)
    alpha = Image.fromarray(np.uint8(rgba[:, :, 3] >= 128) * 255)
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError('Empty master')
    scale = visible_height / (bbox[3] - bbox[1])
    tx = centre_x - scale * (bbox[0] + bbox[2]) / 2
    ty = bottom - scale * bbox[3]
    work, source_fringe_count = remove_magenta_fringe(rgba, 4) if fringe else (rgba.copy(), 0)
    sampled, coverage = exact_area_sample(work, canvas_size, scale, tx, ty, coverage_threshold)
    enhanced, detail_count = enhance_details(sampled) if detail else (sampled.copy(), 0)
    edged, output_fringe_count = remove_magenta_fringe(enhanced, 1) if fringe else (enhanced.copy(), 0)
    finished = bleed_transparent_rgb(edged) if bleed else edged.copy()
    if add_top_row:
        finished = np.pad(finished, ((1, 0), (0, 0), (0, 0)), constant_values=0)
    output = Image.fromarray(finished)
    report = {
        'source_size': list(image.size), 'clean_bbox': list(bbox),
        'parameters': {'visible_height': visible_height, 'centre_x': centre_x, 'bottom_exclusive': bottom,
                       'sampling_canvas': list(canvas_size), 'add_top_row': add_top_row,
                       'coverage_threshold': coverage_threshold, 'magenta_key': magenta_key,
                       'fringe': fringe, 'detail': detail, 'bleed': bleed},
        'transform': {'s': scale, 'tx': tx, 'ty': ty},
        'keying_counts': key_counts, 'source_fringe_pixels': source_fringe_count,
        'detail_pixels': detail_count, 'target_fringe_pixels': output_fringe_count,
        'output_size': list(output.size), 'output_bbox': list(output.getchannel('A').getbbox()),
        'alpha_values': np.unique(finished[:, :, 3]).tolist(),
        'opaque_colours': len(np.unique(finished[finished[:, :, 3] == 255, :3], axis=0)),
    }
    stages = {'clean_master': clean, 'derived_fringe_master': Image.fromarray(work),
              'sampled': Image.fromarray(sampled), 'detail': Image.fromarray(enhanced),
              'edge_corrected': Image.fromarray(edged), 'output': output}
    return output, report, stages
