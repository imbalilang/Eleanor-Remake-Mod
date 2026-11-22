#!/usr/bin/env python3
"""Quantitative pixel-CG style gate for project CG exports."""
from __future__ import annotations
import argparse, json
from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np

DEFAULT_PROFILE = Path(__file__).resolve().parents[1] / "docs" / "PIXEL_STYLE_PROFILE_2026-11-20.json"

def measure(path: Path) -> dict:
    im = Image.open(path).convert("RGB")
    w, h = im.size
    if w != 960:
        im = im.resize((960, round(h * 960 / w)), Image.Resampling.LANCZOS)
    a = np.asarray(im, dtype=np.int16)
    g = np.asarray(im.convert("L"), dtype=np.float32)
    eq = (a[:, 1:, :] == a[:, :-1, :]).all(axis=2)
    flat_ratio = float(eq.mean())
    blurred = np.asarray(im.convert("L").filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32)
    hf = g - blurred
    hf_ratio = float(hf.var() / (g.var() + 1e-6))
    q = (a >> 3)
    palette_5bit = int(len(np.unique(q.reshape(-1, 3), axis=0)))
    gx = np.abs(np.diff(g, axis=1))[:-1, :]
    gy = np.abs(np.diff(g, axis=0))[:, :-1]
    mag = gx + gy
    edge_density = float((mag > 18).mean())
    smooth_fraction = float((mag <= 3).mean())
    return {
        "file": str(path),
        "original_size": [w, h],
        "hf_ratio": round(hf_ratio, 4),
        "flat_ratio": round(flat_ratio, 3),
        "palette_5bit": palette_5bit,
        "edge_density": round(edge_density, 4),
        "smooth_fraction": round(smooth_fraction, 3),
    }

def check(path: Path, profile_path: Path = DEFAULT_PROFILE) -> dict:
    profile = json.loads(profile_path.read_text(encoding="utf-8"))
    m = measure(path)
    rows = {}
    ok = True
    for name, spec in profile["metrics"].items():
        value = m[name]
        inside = spec["min"] <= value <= spec["max"]
        rows[name] = {"value": value, "min": spec["min"], "max": spec["max"], "pass": inside}
        ok = ok and inside
    return {
        "profile": profile["name"],
        "canonical_id": profile["canonical_id"],
        "metrics": rows,
        "verdict": "PASS_STYLE" if ok else "FAIL_STYLE",
        "measured": m,
        "manual_checks": ["no uniform checkerboard dithering", "no smooth anime rendering"],
    }

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("images", nargs="+")
    ap.add_argument("--profile", default=str(DEFAULT_PROFILE))
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    results = [check(Path(x), Path(args.profile)) for x in args.images]
    if args.json:
        print(json.dumps(results, ensure_ascii=False, indent=2))
    else:
        for r in results:
            print(f"{r['measured']['file']}: {r['verdict']}")
            for k, v in r["metrics"].items():
                print(f"  {k}: {v['value']} (allowed {v['min']}..{v['max']}) {'PASS' if v['pass'] else 'FAIL'}")
    return 0 if all(r["verdict"] == "PASS_STYLE" for r in results) else 1

if __name__ == "__main__":
    raise SystemExit(main())
