"""Assemble the high-resolution face edits and export the two game sprites."""
from hashlib import sha256
from pathlib import Path
import importlib.util
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
helper_path = ART.with_name("expression-expansion-2026-11-13") / "export_faces.py"
spec = importlib.util.spec_from_file_location("face_export", helper_path)
face_export = importlib.util.module_from_spec(spec)
spec.loader.exec_module(face_export)

SOURCES = {
    "smile": (
        "Art/expression-expansion-2026-11-13/masters/er_expr_eleanor2_smile.png",
        (101, 28, 930, 857),
        [((203, 469), (222, 443), (310, 414), (329, 441), (347, 491),
          (356, 529), (323, 553), (230, 562), (205, 544)),
         ((432, 407), (462, 393), (492, 401), (530, 423), (570, 441),
          (578, 472), (567, 501), (483, 521), (456, 514), (442, 482)),
         ((358, 605), (434, 600), (491, 594), (500, 622), (468, 669),
          (404, 676), (366, 662))],
    ),
    "angry": (
        "Art/expressions-and-endings-2026-11-10/masters/er_expr_eleanor1_angry.png",
        (463, 278, 954, 769),
        [((143, 266), (160, 257), (201, 263), (215, 271), (226, 290),
          (222, 313), (196, 322), (160, 319), (145, 308)),
         ((266, 245), (295, 232), (321, 225), (342, 237), (347, 258),
          (339, 282), (310, 299), (282, 293), (270, 275)),
         ((236, 357), (287, 352), (303, 363), (295, 386), (241, 389))],
    ),
}

MOUTH = ((135, 125), (142, 124), (150, 123), (151, 128), (148, 133),
         (138, 134), (133, 131), (133, 127))
SMILE_FEATURES = [(10.1, (528, 667), (142, 128.1), MOUTH)]
APPROVED_ANGRY = ART / "masters" / "angry.png"
APPROVED_ANGRY_POLYGONS = (
    ((118, 106), (123, 102), (130, 102), (132, 105), (134, 112),
     (136, 117), (131, 121), (121, 122), (116, 120), (114, 116), (116, 112)),
    ((143, 102), (149, 99), (156, 100), (159, 103), (163, 108),
     (160, 113), (153, 118), (146, 117), (142, 111), (141, 106)),
    ((135, 125), (143, 124), (150, 123), (151, 127), (149, 131),
     (144, 133), (136, 132), (134, 129)),
)


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def full_master(source, edit, crop, polygons):
    # The generator's square is registered to the authored crop. Only the
    # designated expression areas enter the full-resolution half-body master.
    region = source.crop(crop)
    warped = edit.resize(region.size, Image.Resampling.LANCZOS)
    stencil = Image.new("L", region.size)
    draw = ImageDraw.Draw(stencil)
    for polygon in polygons:
        draw.polygon(polygon, fill=255)
    weight = np.asarray(stencil.filter(ImageFilter.GaussianBlur(4))).copy()
    weight[(np.asarray(stencil) == 0) | (np.asarray(warped.getchannel("A")) < 250)] = 0
    combined = Image.composite(warped, region, Image.fromarray(weight))
    combined.putalpha(region.getchannel("A"))
    result = source.copy()
    result.paste(combined, crop[:2])
    assert result.getchannel("A").tobytes() == source.getchannel("A").tobytes()
    return result


def runtime(base, edited, features):
    # Retain the canonical in-game eyes. A separate map for each transferred
    # brow and mouth keeps the nose, hair and fixed lower-face edges intact.
    x0, y0, x1, y1 = (113, 97, 166, 136)
    box = (x0, y0, x1, y1)
    original = np.asarray(base)
    result = original.copy()
    working = result[y0:y1, x0:x1, :3].astype(float)
    for scale, origin, target, polygon in features:
        sampler = np.eye(2) * scale
        patch = face_export.sample_feature(edited, sampler, origin, np.array(target), box)
        strength = face_export.feature_mask(polygon, box)
        strength *= (patch[:, :, 3] == 255) & (original[y0:y1, x0:x1, 3] == 255)
        strength = strength[:, :, None]
        working = working * (1 - strength) + patch[:, :, :3] * strength
    result[y0:y1, x0:x1, :3] = np.floor(working + 0.5).astype(np.uint8)
    for x0, y0, x1, y1 in ((113, 112, 137, 124), (142, 108, 166, 122),
                           (136, 118, 146, 124), (0, 135, 288, 212)):
        assert np.array_equal(result[y0:y1, x0:x1], original[y0:y1, x0:x1])
    assert np.array_equal(result[:, :, 3], original[:, :, 3])
    return Image.fromarray(result)


def approved_angry(base):
    # The approved close-up was authored from the (88, 50)-(200, 162)
    # neutral crop. Sample that same frame directly to retain the selected
    # brows, eyelids and mouth, then place them on the full game portrait.
    width, height = 112, 112
    box = (88, 50, 200, 162)
    edit = Image.open(APPROVED_ANGRY).convert("RGBA")
    assert edit.width == edit.height
    sampled, _ = face_export.exact_area_sample(np.asarray(edit), (width, height), width / edit.width, 0, 0)
    strength = np.maximum.reduce([face_export.feature_mask(poly, box) for poly in APPROVED_ANGRY_POLYGONS])
    original = np.asarray(base)
    result = original.copy()
    window = result[50:162, 88:200]
    strength = (strength * (window[:, :, 3] == 255))[:, :, None]
    window[:, :, :3] = np.floor(window[:, :, :3] * (1 - strength) + sampled[:, :, :3] * strength + 0.5)
    assert np.array_equal(result[:, :, 3], original[:, :, 3])
    assert np.array_equal(result[135:], original[135:])
    assert np.array_equal(result[118:123, 136:145], original[118:123, 136:145])
    return Image.fromarray(result)


def main():
    base_path = ROOT / "Textures/Items/cp_portrait_eleanor1.png"
    base = Image.open(base_path).convert("RGBA")
    preview = [("Neutral", base)]
    records = []
    for expression, (relative, crop, polygons) in SOURCES.items():
        stem = f"er_expr_eleanor1_{expression}"
        source_path = ROOT / relative
        edit_path = ART / "masters" / f"{expression}-hd-local.png"
        source = Image.open(source_path).convert("RGBA")
        edit = Image.open(edit_path).convert("RGBA")
        master = full_master(source, edit, crop, polygons)
        master_path = ART / "masters" / f"eleanor1-{expression}-half-body.png"
        master.save(master_path, optimize=True)
        runtime_source = APPROVED_ANGRY if expression == "angry" else master_path
        portrait = approved_angry(base) if expression == "angry" else runtime(base, master, SMILE_FEATURES)
        output = ART / "portraits" / f"{stem}.png"
        portrait.save(output, optimize=True)
        before = ART / "before" / f"{stem}.png"
        preview.extend([(f"{expression} before", Image.open(before).convert("RGBA")),
                        (f"{expression} revised", portrait)])
        records.append({
            "sprite": output.name, "original_master": relative, "original_master_size": source.size,
            "original_master_sha256": digest(source_path), "source_crop_native_hd_pixels": crop,
            "edit_reference_sha256": digest(ART / "references" / f"{expression}-hd-target.png"),
            "edit_sha256": digest(edit_path), "full_master_sha256": digest(master_path),
            "runtime_source": runtime_source.relative_to(ROOT).as_posix(),
            "runtime_source_sha256": digest(runtime_source),
            "old_sprite_sha256": digest(before), "new_sprite_sha256": digest(output),
            "changed_pixels_against_neutral": int(np.any(np.asarray(base) != np.asarray(portrait), axis=2).sum()),
        })
    face_export.ART = ART
    face_export.preview("eleanor1", preview)
    sheet = Image.new("RGB", (1020, 332), "#252C35")
    for column, (_, portrait) in enumerate((preview[0], preview[2], preview[4])):
        closeup = portrait.crop((60, 52, 228, 212)).resize((336, 320), Image.Resampling.NEAREST)
        sheet.paste(closeup, (column * 340 + 2, 6), closeup)
    sheet.save(ART / "previews" / "eleanor1-half-body-2x.png", optimize=True)
    (ART / "asset_validation.json").write_text(json.dumps({
        "size": base.size, "base_sha256": digest(base_path), "jaw_and_alpha_match_base": True,
        "output": records,
    }, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
