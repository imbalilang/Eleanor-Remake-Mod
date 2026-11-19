"""Retarget expression features to the approved neutral portrait for each outfit."""
from pathlib import Path
from hashlib import sha256
import importlib.util
import json

import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parent
PREVIOUS = ART.with_name("expression-expansion-2026-11-13")
spec = importlib.util.spec_from_file_location("face_export", PREVIOUS / "export_faces.py")
export = importlib.util.module_from_spec(spec)
spec.loader.exec_module(export)


def eye_bounds(eye, style, side):
    (left, ly), (right, ry) = eye
    midpoint = (left + right) / 2
    line = (ly + ry) / 2
    # Follow the base hair edge on the inner side of each eye.
    upper_left = left + (2 if side == 0 else 0)
    upper_right = right - (3 if side == 0 else 1)
    above = 13 if style == "eleanor" else 9
    return [
        (upper_left, line - above), (upper_right, line - above),
        (right + (1 if side else 0), ry - 4),
        (right + 2, ry + 2), (right - 1, line + 5),
        (midpoint, line + 7), (left + 1, line + 6),
        (left - 3, ly + 1), (left - 1, ly - 4),
    ]


def mouth_bounds(center, style):
    x, y = center
    width = {"eleanor": 11, "lydia": 8, "sophia": 8}[style]
    return [(x - width, y - 3), (x + width, y - 6),
            (x + width, y + 2), (x + 4, y + 5),
            (x - 4, y + 5), (x - width, y + 2)]


def sha(path):
    return sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def choose_master(style, expression, landmarks, previous_points):
    if style == "eleanor":
        key = f"eleanor2_{expression}"
        return PREVIOUS / f"masters/er_expr_{key}.png", previous_points[key], False
    name = "lydia_daily" if style == "lydia" else "sophia"
    path = ART / f"masters/er_expr_{name}_{expression}.png"
    return path, landmarks[name][expression], True


def main():
    landmarks = read(ART / "landmarks.json")
    references = read(ART / "references.json")
    previous_points = read(PREVIOUS / "landmarks.json")["masters"]
    records = []
    for family, profile in landmarks["families"].items():
        style = profile["family_style"]
        reference = references[family]
        base_path = export.ROOT / reference.get("original_base", reference["base"])
        assert sha(base_path) == references[family]["base_sha256"], base_path
        with Image.open(base_path) as im:
            base = im.convert("RGBA")
        assert base.size == export.SIZE
        profile["eye_bounds"] = [eye_bounds(eye, style, i) for i, eye in enumerate(profile["eyes"])]
        profile["mouth_bounds"] = mouth_bounds(profile["mouth"], style)
        portraits = [("neutral", base)]
        for expression in export.EXPRESSIONS:
            stem = f"er_expr_{family}_{expression}"
            if expression not in profile.get("expressions", export.EXPRESSIONS):
                with Image.open(export.ROOT / f"Textures/Items/{stem}.png") as im:
                    portraits.append((expression, im.convert("RGBA")))
                continue
            if style == "lydia" and expression == "shy":
                # Keep the exact previously published expression under the canonical key.
                master_path = export.ROOT / f"Textures/Items/er_expr_{family}_flustered.png"
                with Image.open(master_path) as im:
                    result = im.convert("RGBA")
                count = int(np.any(np.asarray(result) != np.asarray(base), axis=2).sum())
                method = "existing flustered art under canonical shy key"
            else:
                master_path, points, normalized = choose_master(style, expression, landmarks, previous_points)
                with Image.open(master_path) as im:
                    if normalized:
                        scale = np.array(im.size) / 1000
                        points = [(np.asarray(group) * scale).tolist() for group in points]
                    result, count = export.export_portrait(
                        base, im.convert("RGBA"), profile, points, brow_key=(family, expression),
                    )
                method = "eye-corner registration and local B/detail compositing"
            output = ART / f"portraits/{stem}.png"
            assert result.size == export.SIZE and result.mode == "RGBA"
            assert result.getchannel("A").tobytes() == base.getchannel("A").tobytes()
            result.save(output, optimize=True)
            portraits.append((expression, result))
            records.append({
                "key": stem, "family": family, "expression": expression,
                "source": master_path.relative_to(export.ROOT).as_posix(),
                "source_sha256": sha(master_path), "base_sha256": sha(base_path),
                "output_sha256": sha(output), "changed_pixels": count, "method": method,
            })
        export.ART = ART
        export.preview(family, portraits)
    report = {
        "size": list(export.SIZE), "mode": "RGBA",
        "alpha_matches_base": True, "expression_art": records,
    }
    (ART / "asset_validation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Exported {len(records)} expression PNGs")


if __name__ == "__main__":
    main()
