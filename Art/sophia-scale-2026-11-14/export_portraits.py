"""Reframe Sophia's approved masters and rebuild both expression families."""
import importlib.util
import json
from hashlib import sha256
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
EXPRESSION_ART = ART.with_name("expression-expansion-2026-11-14")
spec = importlib.util.spec_from_file_location("expression_export", EXPRESSION_ART / "export_faces.py")
expressions = importlib.util.module_from_spec(spec)
spec.loader.exec_module(expressions)
face_export = expressions.export
from author_portrait_pipeline import render

SCALE_FACTOR = 1.15
EYE_ANCHOR = np.array([145.0, 115.0])


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def sha(path):
    return sha256(path.read_bytes()).hexdigest()


def load_rgba(path):
    with Image.open(path) as image:
        return image.convert("RGBA")


def resize_portrait(master, original):
    # The sampler runs on 288×211 before the transparent top row is added.
    ax, ay = EYE_ANCHOR - (0, 1)
    scale = original["s"] * SCALE_FACTOR
    tx = ax + (original["tx"] - ax) * SCALE_FACTOR
    ty = ay + (original["ty"] - ay) * SCALE_FACTOR
    alpha = master.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    x0, y0, x1, y1 = alpha.getbbox()
    args = (scale * (y1 - y0), tx + scale * (x0 + x1) / 2, ty + scale * y1)
    result, report, _ = render(master, *args, fringe=False)
    assert result.tobytes() == render(master, *args, fringe=False)[0].tobytes()
    assert result.size == face_export.SIZE and report["alpha_values"] == [0, 255]
    assert report["output_bbox"][3] == 212 and not np.any(np.asarray(result)[0])
    return result, report


def map_points(points):
    return (EYE_ANCHOR + (np.asarray(points) - EYE_ANCHOR) * SCALE_FACTOR).tolist()


def remap_profile(profile):
    eye_masks = [
        expressions.eye_bounds(eye, "sophia", side) for side, eye in enumerate(profile["eyes"])
    ]
    return {
        "eyes": map_points(profile["eyes"]),
        "mouth": map_points(profile["mouth"]),
        "eye_bounds": [map_points(mask) for mask in eye_masks],
        "mouth_bounds": map_points(expressions.mouth_bounds(profile["mouth"], "sophia")),
    }


def preview(rows):
    box = (87, 52, 204, 212)
    width, height = box[2] - box[0], box[3] - box[1]
    sheet = Image.new("RGB", (width * 3, (height + 23) * 2), "#252C35")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 12)
    for row, items in enumerate(rows):
        for col, (label, image) in enumerate(items):
            x, y = col * width, row * (height + 23)
            crop = image.crop(box)
            sheet.paste(crop, (x, y + 23), crop)
            draw.text((x + width // 2, y + 11), label, font=font, fill="white", anchor="mm")
    sheet.save(ART / "previews/comparison-native.png", optimize=True)
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.Resampling.NEAREST).save(
        ART / "previews/comparison-2x.png", optimize=True
    )


def main():
    for folder in ("portraits", "previews"):
        (ART / folder).mkdir(exist_ok=True)
    approval = read(ART.with_name("sophia-ruan-mei-home-2026-11-13") / "approval.json")
    designs = read(ART.with_name("sophia-three-directions-2026-11-13") / "asset_validation.json")
    daily = next(asset for asset in designs["assets"] if asset["direction"] == "ruan-mei")
    landmarks = read(EXPRESSION_ART / "landmarks.json")
    legacy = {
        item["key"]: item for item in read(EXPRESSION_ART / "asset_validation.json")["expression_art"]
        if item["family"] in ("sophia", "sophia_home")
    }
    comparisons = [
        [("埃莉诺常服", load_rgba(ROOT / "Textures/Items/cp_portrait_eleanor1.png"))],
        [("莉迪亚约会装", load_rgba(ROOT / "Textures/Items/er_portrait_lydia_date_female_beauty.png"))],
    ]
    files = {}
    transforms = {}
    face_export.ART = ART
    for row, (role, family, old_pipeline) in enumerate((
        ("daily", "sophia", daily["pipeline"]),
        ("home", "sophia_home", approval["home_pipeline"]),
    )):
        outfit = approval[role]
        original = ROOT / outfit["portrait"]["file"]
        master = ROOT / outfit["master"]["file"]
        assert sha(original) == outfit["portrait"]["sha256"]
        assert sha(master) == outfit["master"]["sha256"]
        base, transforms[family] = resize_portrait(load_rgba(master), old_pipeline["transform"])
        target = Path(outfit["runtime_path"])
        path = ART / "portraits" / target.name
        base.save(path, optimize=True)
        files[target.as_posix()] = {"previous_sha256": sha(original), "output_sha256": sha(path)}
        outfit_label = "常服" if role == "daily" else "私服"
        comparisons[row].extend([
            (f"原尺寸{outfit_label}", load_rgba(original)),
            (f"调整后{outfit_label}", base),
        ])

        profile = remap_profile(landmarks["families"][family])
        variants = [("neutral", base)]
        for expression in face_export.EXPRESSIONS:
            key = f"er_expr_{family}_{expression}"
            source = EXPRESSION_ART / f"masters/er_expr_sophia_{expression}.png"
            assert sha(source) == legacy[key]["source_sha256"]
            with Image.open(source) as image:
                scale = np.array(image.size) / 1000
                points = [(np.asarray(group) * scale).tolist() for group in landmarks["sophia"][expression]]
                result, changed = face_export.export_portrait(base, image.convert("RGBA"), profile, points)
            target = Path("Textures/Items") / f"{key}.png"
            path = ART / "portraits" / target.name
            result.save(path, optimize=True)
            files[target.as_posix()] = {
                "previous_sha256": legacy[key]["output_sha256"],
                "output_sha256": sha(path),
                "changed_facial_pixels": changed,
            }
            variants.append((expression, result))
        face_export.preview(family, variants)

    preview(comparisons)
    assert len(files) == 20
    report = {
        "factor": SCALE_FACTOR,
        "native_eye_anchor": EYE_ANCHOR.tolist(),
        "transforms": transforms,
        "files": files,
        "workspace_installed": all(sha(ROOT / path) == info["output_sha256"]
                                   for path, info in files.items()),
        "in_game_verified": False,
    }
    (ART / "asset_validation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("Exported two neutral portraits and 18 expression variants.")


if __name__ == "__main__":
    main()
