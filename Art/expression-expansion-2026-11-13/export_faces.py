"""Sample generated expressions into the exact in-game neutral portraits."""
from pathlib import Path
from hashlib import sha256
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from author_portrait_pipeline import exact_area_sample, enhance_details
from expression_face_bounds import include_left_brow

SIZE = (288, 212)
PATCH = (103, 96, 166, 137)
SUPERSAMPLE = 6
EXPRESSIONS = ("serious", "smile", "soft", "sad", "hurt", "angry", "shy", "determined", "tired")


def sample_feature(master, scale_rotation, source_anchor, base_anchor, patch_box=PATCH):
    """Map native pixel coordinates to the local generated facial feature."""
    x0, y0, x1, y1 = patch_box
    width, height = x1 - x0, y1 - y0
    offset = np.asarray(source_anchor) + scale_rotation @ (np.array([x0, y0]) - base_anchor)
    inverse = scale_rotation / SUPERSAMPLE
    coefficients = (*inverse[0], offset[0], *inverse[1], offset[1])
    patch = master.transform(
        (width * SUPERSAMPLE, height * SUPERSAMPLE),
        Image.Transform.AFFINE, coefficients, Image.Resampling.BICUBIC,
    )
    sampled, _ = exact_area_sample(np.asarray(patch), (width, height), 1 / SUPERSAMPLE, 0, 0)
    detailed, _ = enhance_details(sampled)
    return detailed


def inverse_eye_transform(source_eye, base_eye):
    # A separate similarity map per eye keeps both endpoints anchored.
    start, end = (complex(*p) for p in source_eye)
    base_start, base_end = (complex(*p) for p in base_eye)
    ratio = (end - start) / (base_end - base_start)
    return np.array([[ratio.real, -ratio.imag], [ratio.imag, ratio.real]])


def feature_mask(polygon, patch_box=PATCH):
    x0, y0, x1, y1 = patch_box
    stencil = Image.new("L", (x1 - x0, y1 - y0))
    ImageDraw.Draw(stencil).polygon([(x - x0, y - y0) for x, y in polygon], fill=255)
    feather = np.asarray(stencil.filter(ImageFilter.GaussianBlur(.65))) / 255
    return (np.asarray(stencil) > 0) * feather


def export_portrait(base, master, profile, source_points, patch_box=PATCH, *, brow_key=None):
    x0, y0, x1, y1 = patch_box
    original = np.asarray(base)
    neutral = original[y0:y1, x0:x1]
    output = original.copy()
    working = neutral[:, :, :3].astype(float)
    permitted = np.zeros(neutral.shape[:2], dtype=bool)

    transforms = []
    for source_eye, base_eye, polygon in zip(source_points[:2], profile["eyes"], profile["eye_bounds"]):
        matrix = inverse_eye_transform(source_eye, base_eye)
        transforms.append((matrix, source_eye[0], np.array(base_eye[0]), polygon))
    average_scale = np.mean([np.linalg.norm(matrix[:, 0]) for matrix, *_ in transforms])
    transforms.append((np.eye(2) * average_scale, source_points[2],
                       np.array(profile["mouth"]), profile["mouth_bounds"]))

    for index, (matrix, source_anchor, base_anchor, polygon) in enumerate(transforms):
        sampled = sample_feature(master, matrix, source_anchor, base_anchor, patch_box)
        weight = feature_mask(polygon, patch_box)
        if index == 0 and brow_key is not None:
            weight = include_left_brow(weight, *brow_key, origin=(x0, y0))
        weight *= (sampled[:, :, 3] == 255) & (neutral[:, :, 3] == 255)
        permitted |= weight > 0
        weight = weight[:, :, None]
        working = working * (1 - weight) + sampled[:, :, :3] * weight
    output[y0:y1, x0:x1, :3] = np.floor(working + .5).astype(np.uint8)
    changed = np.any(output != original, axis=2)
    mask = np.zeros(changed.shape, dtype=bool)
    mask[y0:y1, x0:x1] = permitted
    assert np.array_equal(output[:, :, 3], original[:, :, 3])
    assert not np.any(changed & ~mask)
    assert not np.any(changed[136:])
    assert int(changed.sum()) > 60
    return Image.fromarray(output), int(changed.sum())


def sha(path):
    return sha256(path.read_bytes()).hexdigest()


def preview(family, portraits):
    box = (102, 92, 168, 142)
    scale = 4
    width = (box[2] - box[0]) * scale
    height = (box[3] - box[1]) * scale + 21
    columns = min(5, len(portraits))
    rows = (len(portraits) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * width, rows * height), "#252C35")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 13)
    for index, (expression, im) in enumerate(portraits):
        x, y = (index % columns) * width, (index // columns) * height
        face = im.crop(box).resize((width, height - 21), Image.Resampling.NEAREST)
        sheet.paste(face, (x, y + 21), face)
        draw.text((x + 7, y + 2), expression, fill="white", font=font)
    sheet.save(ART / f"previews/{family}-faces-4x.png", optimize=True)
    native_width, native_height = 126, 175
    native = Image.new("RGB", (columns * native_width, rows * native_height), "#252C35")
    draw = ImageDraw.Draw(native)
    for index, (expression, im) in enumerate(portraits):
        x, y = (index % columns) * native_width, (index // columns) * native_height
        crop = im.crop((85, 57, 211, 212))
        native.paste(crop, (x, y + 20), crop)
        draw.text((x + 5, y + 1), expression, fill="white", font=font)
    native.save(ART / f"previews/{family}-native.png", optimize=True)


def main():
    geometry = json.loads((ART / "landmarks.json").read_text(encoding="utf-8"))
    records = []
    for family, profile in geometry["base"].items():
        base_path = ROOT / f"Textures/Items/cp_portrait_{family}.png"
        with Image.open(base_path) as im:
            base = im.convert("RGBA")
        assert base.size == SIZE
        portraits = [("neutral", base)]
        for expression in EXPRESSIONS:
            name = f"{family}_{expression}"
            if name not in geometry["masters"]:
                continue
            stem = f"er_expr_{name}"
            master_path = ART / f"masters/{stem}.png"
            output_path = ART / f"portraits/{stem}.png"
            with Image.open(master_path) as im:
                result, count = export_portrait(base, im.convert("RGBA"), profile, geometry["masters"][name])
            result.save(output_path, optimize=True)
            portraits.append((expression, result))
            records.append({
                "key": stem, "family": family, "expression": expression,
                "base_sha256": sha(base_path), "master_sha256": sha(master_path),
                "output_sha256": sha(output_path), "changed_pixels": count,
            })
        preview(family, portraits)
    report = {
        "size": list(SIZE), "mode": "RGBA", "alpha_matches_base": True,
        "unchanged_outside_local_feature_masks": True, "jaw_and_neck_protected": True,
        "expression_art": records,
    }
    (ART / "asset_validation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Exported {len(records)} expression PNGs")


if __name__ == "__main__":
    main()
