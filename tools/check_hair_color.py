#!/usr/bin/env python3
"""Heuristic hair-colour deviation check against the QQ 200057 copper-orange lock.

This is a QA heuristic, not a segmentation model. It masks orange pixels in the
upper portion of the frame, rejects low-saturation wood/skin and pixels far from
the reference hair palette, then reports the median hair colour and CIE Lab /
HSV deviation from the canonical base.
"""
from __future__ import annotations
import argparse, json, math
from pathlib import Path
import numpy as np
from PIL import Image

BASE = (204, 106, 71)      # #CC6A47
SHADOW = (131, 52, 77)     # #83344D
HIGHLIGHT = (238, 161, 117) # #EEA175


def srgb_to_linear(c):
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def rgb_to_lab(rgb):
    rgb = np.asarray(rgb, dtype=np.float64)
    lin = srgb_to_linear(rgb)
    x = lin[..., 0] * 0.4124564 + lin[..., 1] * 0.3575761 + lin[..., 2] * 0.1804375
    y = lin[..., 0] * 0.2126729 + lin[..., 1] * 0.7151522 + lin[..., 2] * 0.0721750
    z = lin[..., 0] * 0.0193339 + lin[..., 1] * 0.1191920 + lin[..., 2] * 0.9503041
    x /= 0.95047; y /= 1.00000; z /= 1.08883
    xyz = np.stack([x, y, z], -1)
    f = np.where(xyz > 0.008856, np.cbrt(np.maximum(xyz, 1e-12)), 7.787 * xyz + 16/116)
    fx, fy, fz = f[..., 0], f[..., 1], f[..., 2]
    return np.stack([116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)], -1)


def hsv(rgb):
    a = np.asarray(rgb, dtype=np.float64) / 255.0
    mx = a.max(-1); mn = a.min(-1); d = mx - mn
    h = np.zeros_like(mx)
    m = d != 0
    r, g, b = a[...,0], a[...,1], a[...,2]
    idx = m & (mx == r); h[idx] = ((g[idx]-b[idx]) / d[idx]) % 6
    idx = m & (mx == g); h[idx] = (b[idx]-r[idx]) / d[idx] + 2
    idx = m & (mx == b); h[idx] = (r[idx]-g[idx]) / d[idx] + 4
    h = h / 6.0
    s = np.where(mx == 0, 0, d / np.maximum(mx, 1e-12))
    return h, s, mx


def palette_distance_lab(rgb, palettes):
    lab = rgb_to_lab(rgb)
    ds = []
    for p in palettes:
        ds.append(np.linalg.norm(lab - rgb_to_lab(np.array(p)), axis=-1))
    return np.min(np.stack(ds, -1), axis=-1)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('image')
    ap.add_argument('--reference', default=r'B:\Eleanor Story Expand\Art\原图参考\QQ图片20261119200057.png')
    ap.add_argument('--top-fraction', type=float, default=0.72)
    ap.add_argument('--json', action='store_true')
    ap.add_argument('--save-mask', default='')
    args = ap.parse_args()

    def hair_mask_for(rgb):
        h, s, v = hsv(rgb)
        H, W = h.shape
        yy = np.arange(H)[:, None] / max(H - 1, 1)
        region = yy <= args.top_fraction
        m = region & (h >= 0.015) & (h <= 0.115) & (s >= 0.43) & (v >= 0.22)
        m &= ~((h > 0.060) & (s < 0.60) & (v < 0.62))
        m &= palette_distance_lab(rgb.astype(np.float64), [BASE, HIGHLIGHT]) <= 38.0
        return m

    ref = np.asarray(Image.open(args.reference).convert('RGB'), dtype=np.uint8)
    ref_mask = hair_mask_for(ref)
    if ref_mask.sum() < 50:
        raise SystemExit('reference hair mask failed')
    ref_median = np.median(ref[ref_mask].astype(np.float64), axis=0)
    ref_lab = rgb_to_lab(ref_median)

    im = Image.open(args.image).convert('RGB')
    a = np.asarray(im, dtype=np.uint8)
    mask = hair_mask_for(a)

    if mask.sum() < 50:
        out = {'image': str(Path(args.image).resolve()), 'verdict': 'NO_HAIR_MASK', 'pixels': int(mask.sum()), 'coverage': float(mask.mean())}
        print(json.dumps(out, indent=2, ensure_ascii=False) if args.json else out)
        return 2

    px = a[mask].astype(np.float64)
    median = np.median(px, axis=0).round().astype(int)
    mean = px.mean(axis=0).round().astype(int)
    lab = rgb_to_lab(px)
    base_lab = rgb_to_lab(np.array(BASE, dtype=float))
    hh, ss, vv = hsv(px.reshape(1, -1, 3))
    hh = float(np.median(hh)); ss = float(np.median(ss)); vv = float(np.median(vv))
    bh, bs, bv = hsv(np.array(BASE).reshape(1,1,3))
    bh, bs, bv = float(bh[0,0]), float(bs[0,0]), float(bv[0,0])
    dh = abs(hh - bh); dh = min(dh, 1-dh) * 360
    de_base = float(np.median(np.linalg.norm(lab - base_lab, axis=-1)))
    de_hi = float(np.median(np.linalg.norm(lab - rgb_to_lab(np.array(HIGHLIGHT, dtype=float)), axis=-1)))
    de_ref = float(np.median(np.linalg.norm(lab - ref_lab, axis=-1)))
    if de_ref <= 6: verdict = 'PASS'
    elif de_ref <= 12: verdict = 'WARN'
    else: verdict = 'FAIL'

    out = {
        'image': str(Path(args.image).resolve()),
        'reference': args.reference,
        'verdict': verdict,
        'pixels': int(mask.sum()),
        'coverage': round(float(mask.mean()), 5),
        'median_rgb': [int(x) for x in median],
        'median_hex': '#%02X%02X%02X' % tuple(median),
        'mean_rgb': [int(x) for x in mean],
        'mean_hex': '#%02X%02X%02X' % tuple(mean),
        'delta_e_base': round(de_base, 2),
        'delta_e_highlight': round(de_hi, 2),
        'delta_e_reference': round(de_ref, 2),
        'reference_median_hex': '#%02X%02X%02X' % tuple(np.round(ref_median).astype(int)),
        'hue_delta_deg': round(dh, 2),
        'sat_delta': round((ss - bs) * 100, 2),
        'val_delta': round((vv - bv) * 100, 2),
        'canonical': {'base':'#CC6A47','highlight':'#EEA175','shadow':'#83344D'},
    }
    if args.save_mask:
        m = (mask[..., None] * np.array([255, 255, 255], dtype=np.uint8))
        Image.fromarray(m.astype(np.uint8)).save(args.save_mask)
    print(json.dumps(out, indent=2, ensure_ascii=False) if args.json else out)
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
