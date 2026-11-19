"""Repair the numbered mouth, eyelid and brow artifacts in the runtime sprites."""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from export_registered_expression import assemble, digest, load_image, polygon_mask

TEXTURES = ROOT / "Textures" / "Items"
INPUTS = ART / "before" / "detail-review"
OUTPUTS = ART / "portraits" / "detail-review"
MOUTH = {
    "002": ("smile", (136, 125, 148, 131)),
    "003": ("soft", (136, 126, 147, 131)),
    "004": ("shy", (136, 126, 147, 131)),
    "005": ("serious", (137, 127, 147, 131)),
    "006": ("angry", (137, 127, 146, 131)),
    "007": ("sad", (136, 128, 148, 133)),
    "008": ("hurt", (137, 125, 147, 131)),
    "009": ("determined", (136, 126, 147, 131)),
    "010": ("tired", (137, 125, 148, 131)),
}
DETAILS = {"081": ("eleanor_love_intimate", "tired"), "112": ("lydia_date", "determined")}


def mouth_on_neutral(current, neutral, lip_box):
    """Carry the lip stroke and shading without the sampled skin beneath it."""
    result = current.copy()
    y0, y1, x0, x1 = 123, 135, 131, 154
    source = current[y0:y1, x0:x1, :3].astype(float)
    base = neutral[y0:y1, x0:x1, :3].astype(float)
    lx0, ly0, lx1, ly1 = lip_box
    lip = np.s_[ly0 - y0:ly1 - y0, lx0 - x0:lx1 - x0]
    # Estimate the existing skin at the lips from rows immediately above and
    # below. The skin colour itself always comes from the in-game neutral.
    columns = slice(lx0 - x0, lx1 - x0)
    pitch = (np.arange(ly0, ly1) - (ly0 - 1)) / (ly1 - (ly0 - 1))
    pitch = pitch[:, None, None]
    donor_skin = (source[ly0 - y0 - 1, columns] * (1 - pitch)
                  + source[ly1 - y0, columns] * pitch)
    signal = source[lip] - donor_skin
    # Low contrast around the outside of the stroke is the donor's skin halo.
    strength = np.clip((-signal[..., 1] - 4) / 10, 0, 1)[..., None]
    clean = base.copy()
    # Fill the old neutral mouth with the same portrait's nearby skin, so its
    # original smile cannot show underneath a new expression.
    for y in range(126, 132):
        t = (y - 125) / 7
        clean[y - y0, 136 - x0:148 - x0] = (
            base[125 - y0, 136 - x0:148 - x0] * (1 - t)
            + base[132 - y0, 136 - x0:148 - x0] * t)
    clean[lip] += signal * strength
    result[y0:y1, x0:x1, :3] = np.clip(np.floor(clean + .5), 0, 255).astype(np.uint8)
    return result


def eyelid(source_points, target_points, polygon):
    return {"source_points": source_points, "target_points": target_points,
            "polygon": polygon, "match_boundary_colour": True}


def replace_eyes(current, number, grid_spec):
    result = current.copy()
    eye_band = np.s_[103:123, 113:166]
    if number == "010":
        # The tired expression is carried by the mouth. Keep both eyes from
        # the same neutral frame, including their original lashes and irises.
        neutral = np.asarray(load_image(TEXTURES / "cp_portrait_eleanor1.png"))
        result[eye_band] = neutral[eye_band]
        return result

    spec = grid_spec["eleanor_love_intimate"]
    base = load_image(ROOT / spec["runtime_base"])
    master = load_image(ART / "masters" / "eleanor_love_intimate-grid.png")
    width, height = master.width / 3, master.height / 3
    ox, oy = spec["native_sheet_origin"]
    span = spec["native_sheet_span"]

    def in_tile(points):
        return [(2 * width + (x - ox) * width / span,
                 2 * height + (y - oy) * height / span) for x, y in points]

    features = [
        eyelid(in_tile([(117.6, 119.8), (135.1, 116.6)]), [(119.0, 118.6), (134.8, 116.3)],
               [(115, 117), (119, 113), (124, 110), (130, 109), (135, 111),
                (137, 116), (134, 120), (128, 122), (121, 122), (115, 120)]),
        eyelid(in_tile([(148.0, 112.5), (161.5, 109.6)]), [(147.0, 112.5), (161.6, 109.2)],
               [(143, 111), (147, 107), (153, 104), (159, 103), (163, 105),
                (165, 109), (161, 113), (155, 116), (149, 117), (145, 116)]),
    ]
    original = np.asarray(base)
    rebuilt, _ = assemble(base, master, features, protected=[])
    result[eye_band] = original[eye_band]
    fit = np.asarray(rebuilt)
    changed = np.any(fit != original, axis=2)
    result[changed] = fit[changed]
    return result


def brows_on_neutral(current, neutral, info):
    shape = next(f["polygon"] for f in info["112"]["features"] if f["feature"] == "near brow")
    mask = np.asarray(polygon_mask([shape], (288, 212))) > 0
    result = current.copy()
    result[mask] = neutral[mask]
    return result


def main():
    spec = json.loads((ART / "family-grid-spec.json").read_text(encoding="utf-8"))
    positions = json.loads((ART / "family-registration.json").read_text(encoding="utf-8"))
    group = {number: ("eleanor1", tag) for number, (tag, _) in MOUTH.items()} | DETAILS
    e1 = np.asarray(load_image(TEXTURES / "cp_portrait_eleanor1.png"))
    OUTPUTS.mkdir(parents=True, exist_ok=True)
    report = []
    for number, (family, tag) in group.items():
        path = f"er_expr_{family}_{tag}.png"
        source, target = INPUTS / path, OUTPUTS / path
        original = np.asarray(load_image(source))
        updated = original.copy()
        if number in MOUTH:
            updated = mouth_on_neutral(updated, e1, MOUTH[number][1])
        if number in ("010", "081"):
            updated = replace_eyes(updated, number, spec)
        if number == "112":
            updated = brows_on_neutral(updated, np.asarray(load_image(ROOT / spec[family]["runtime_base"])), positions)
        assert np.array_equal(original[:, :, 3], updated[:, :, 3])
        assert np.array_equal(original[135:], updated[135:])
        assert np.any(updated != original), path
        Image.fromarray(updated).save(target, optimize=True)
        report.append({"number": number, "sprite": path, "previous_sha256": digest(source),
                       "sha256": digest(target), "changed_pixels": int(np.any(updated != original, axis=2).sum())})
    (OUTPUTS / "report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
