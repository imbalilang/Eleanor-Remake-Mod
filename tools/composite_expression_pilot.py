"""Composite reviewed expression face layers onto a pixel portrait neutral.

Pilot-only: uses fixed native feature ellipses plus the reviewed jaw guard.
The neutral alpha and every pixel outside the face mask stay untouched.
"""
from pathlib import Path
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from expression_face_bounds import jaw_guard


def feature_mask(expression):
    mask = Image.new('L', (288, 212), 0)
    draw = ImageDraw.Draw(mask)
    # Tight eye + brow regions, kept inside the face and away from bangs.
    draw.ellipse((116, 106, 141, 123), fill=255)
    draw.ellipse((145, 105, 170, 122), fill=255)
    # Mouth only; do not paste cheeks or jaw shading.
    draw.ellipse((126, 125, 155, 137), fill=255)
    soft = np.asarray(mask.filter(ImageFilter.GaussianBlur(1.5))).astype(float) / 255
    guard = jaw_guard((288, 212), 'eleanor1')
    combined = soft * guard
    # Feather the jaw-boundary transition too; the guard's hard edge otherwise
    # becomes a visible vertical seam along the chin.
    feathered = Image.fromarray(np.clip(combined * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(.9))
    return np.asarray(feathered).astype(float) / 255


def _mask_indices(mask):
    ys, xs = np.where(mask)
    idx = -np.ones(mask.shape, dtype=np.int32)
    idx[ys, xs] = np.arange(len(ys), dtype=np.int32)
    return ys, xs, idx


def _laplacian(v, ys, xs, idx):
    out = 4.0 * v
    for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        ny, nx = ys + dy, xs + dx
        ok = (ny >= 0) & (ny < idx.shape[0]) & (nx >= 0) & (nx < idx.shape[1])
        nb = np.zeros(len(v), dtype=np.int32)
        nb[ok] = idx[ny[ok], nx[ok]]
        valid = ok & (nb >= 0)
        out[valid] -= v[nb[valid]]
    return out


def poisson_blend(target, source, mask, iters=260):
    """Seamless-clone RGB inside mask; boundary is fixed to target."""
    ys, xs, idx = _mask_indices(mask)
    n = len(ys)
    h, w = mask.shape[:2]
    out = target.copy()
    if n == 0:
        return out
    src = source
    for c in range(3):
        rhs = 4.0 * src[ys, xs, c]
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            ny, nx = ys + dy, xs + dx
            ok = (ny >= 0) & (ny < h) & (nx >= 0) & (nx < w)
            vals = np.zeros(n)
            # inside mask: source; outside mask: target boundary
            ni = np.zeros(n, dtype=np.int32); ni[:] = -1
            ni[ok] = idx[ny[ok], nx[ok]]
            inside = ni >= 0
            vals[inside] = src[ny[inside], nx[inside], c]
            outside = ok & ~inside
            vals[outside] = target[ny[outside], nx[outside], c]
            rhs -= vals
        x = target[ys, xs, c].astype(float).copy()
        r = rhs - _laplacian(x, ys, xs, idx)
        pvec = r.copy()
        rsold = float(np.dot(r, r))
        for _ in range(iters):
            ap = _laplacian(pvec, ys, xs, idx)
            denom = float(np.dot(pvec, ap))
            if abs(denom) < 1e-12:
                break
            alpha = rsold / denom
            x += alpha * pvec
            r -= alpha * ap
            rsnew = float(np.dot(r, r))
            if rsnew < 1e-8:
                break
            pvec = r + (rsnew / rsold) * pvec
            rsold = rsnew
        out[ys, xs, c] = np.clip(x, 0, 255)
    return out

def main():
    root = Path(__file__).resolve().parents[1]
    base_path = root / 'Textures/Items/cp_portrait_eleanor1.png'
    donor_dir = root / 'Art/character-assets-2026-11-20/portrait_expression_pilot_2026-11-22/eleanor1/donors'
    out_dir = root / 'Art/character-assets-2026-11-20/portrait_expression_pilot_2026-11-22/eleanor1/native'
    out_dir.mkdir(parents=True, exist_ok=True)
    base = Image.open(base_path).convert('RGBA')
    base_a = np.array(base)
    report = {}
    for name in ['angry', 'sad', 'serious', 'determined', 'hurt', 'shy', 'smile', 'soft', 'tired']:
        donor = Image.open(donor_dir / f'{name}_288.png').convert('RGBA')
        # Align the generated face to the neutral before masking. The donor
        # is consistently a few pixels high; locate the best face patch shift.
        base_patch = base_a[100:140, 115:175, :3].astype(float)
        best = None
        for dy in range(-6, 7):
            for dx in range(-6, 7):
                crop = np.array(donor)[100 + dy:140 + dy, 115 + dx:175 + dx, :3].astype(float)
                mae = float(np.abs(crop - base_patch).mean())
                if best is None or mae < best[0]:
                    best = (mae, dx, dy)
        _, dx, dy = best
        shift_x, shift_y = -dx, -dy
        aligned = Image.new('RGBA', donor.size, (0, 0, 0, 0))
        aligned.paste(donor, (shift_x, shift_y))
        d = np.array(aligned).astype(float)
        # Match the generated face's skin tone to the neutral before masking.
        y0, y1, x0, x1 = 100, 140, 115, 175
        b = base_a[y0:y1, x0:x1, :3].astype(float)
        dd = d[y0:y1, x0:x1, :3]
        b_skin = (b[:, :, 0] > 150) & (b[:, :, 1] > 95) & (b[:, :, 2] > 70) & (b[:, :, 0] > b[:, :, 2] + 10)
        d_skin = (dd[:, :, 0] > 150) & (dd[:, :, 1] > 95) & (dd[:, :, 2] > 70) & (dd[:, :, 0] > dd[:, :, 2] + 10)
        shift = []
        face = d[y0:y1, x0:x1]
        if b_skin.any() and d_skin.any():
            for c in range(3):
                delta = float(b[:, :, c][b_skin].mean() - dd[:, :, c][d_skin].mean())
                shift.append(delta)
                channel = face[:, :, c]
                channel[d_skin] += delta
                face[:, :, c] = channel
        d = np.clip(d + 0.5, 0, 255).astype(np.uint8)
        weight = feature_mask(name)[:, :, None]
        out = base_a.copy()
        # High-frequency detail swap: keep the neutral's skin tone and lighting,
        # replace only the drawn expression lines/shapes. This removes the
        # low-frequency skin-colour step that caused the visible mask seam.
        b = base_a[:, :, :3].astype(float)
        dd = d[:, :, :3].astype(float)
        blur_b = np.array(Image.fromarray(base_a[:, :, :3]).filter(ImageFilter.GaussianBlur(2.0))).astype(float)
        blur_d = np.array(Image.fromarray(d[:, :, :3]).filter(ImageFilter.GaussianBlur(2.0))).astype(float)
        detail = (dd - blur_d) - (b - blur_b)
        rgb = b + detail * weight
        out[:, :, :3] = np.clip(rgb + 0.5, 0, 255).astype(np.uint8)
        # Alpha is deliberately from the neutral, never from the donor.
        out[:, :, 3] = base_a[:, :, 3]
        Image.fromarray(out, 'RGBA').save(out_dir / f'er_expr_eleanor1_{name}.png', optimize=True)
        changed = np.any(out[:, :, :3] != base_a[:, :, :3], axis=2)
        alpha_same = bool(np.array_equal(out[:, :, 3], base_a[:, :, 3]))
        report[name] = {
            'changed_pixels': int(changed.sum()),
            'alpha_identical': alpha_same,
            'changed_bbox': [int(v) for v in Image.fromarray(changed.astype(np.uint8) * 255).getbbox()] if changed.any() else None,
            'face_align_shift': [int(shift_x), int(shift_y)],
            'face_align_mae': round(best[0], 3),
            'skin_mean_shift_rgb': [round(v, 3) for v in shift],
        }
    (out_dir / 'pilot_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
