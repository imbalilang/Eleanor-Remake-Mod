"""Register and sample each independently redrawn Sophia expression."""
import importlib.util
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent
STUDY = ART.parent
ROOT = STUDY.parents[2]
spec = importlib.util.spec_from_file_location("sophia_expression_study", STUDY / "export_expressions.py")
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)
faces = study.faces
PATCH = (117, 102, 172, 138)
FIT_SIZE = 192


def align_head(reference, master):
    """Fit a single similarity using hair, nose, and lower face landmarks in the image."""
    ref = reference.resize((FIT_SIZE, FIT_SIZE), Image.Resampling.LANCZOS)
    candidate = master.resize(ref.size, Image.Resampling.LANCZOS)
    original = np.asarray(ref, dtype=np.float32)
    mask = Image.new("1", ref.size)
    draw = ImageDraw.Draw(mask)
    for polygon in (
        [(35, 28), (63, 4), (123, 3), (152, 30), (160, 75), (133, 75), (102, 52), (66, 69), (35, 76)],
        [(87, 106), (104, 103), (107, 118), (91, 119)],
        [(49, 106), (65, 112), (77, 131), (76, 144), (51, 135)],
        [(142, 104), (155, 99), (153, 138), (125, 146), (126, 128)],
        [(77, 137), (120, 137), (117, 150), (94, 159), (78, 150)],
    ):
        draw.polygon(polygon, fill=1)
    valid = np.asarray(mask) & (original[:, :, 3] >= 250)
    centre = np.full(2, FIT_SIZE / 2)

    def transform(parameters):
        dx, dy, log_scale, angle = parameters
        c, s = np.cos(angle), np.sin(angle)
        matrix = np.exp(log_scale) * np.array([[c, -s], [s, c]])
        shift = centre + [dx, dy] - matrix @ centre
        return matrix, shift

    def loss(parameters):
        matrix, shift = transform(parameters)
        aligned = candidate.transform(
            ref.size, Image.Transform.AFFINE,
            (*matrix[0], shift[0], *matrix[1], shift[1]), Image.Resampling.BILINEAR,
        )
        error = np.abs(np.asarray(aligned, dtype=np.float32)[valid] - original[valid])
        return float(np.minimum(error, 48).mean())

    parameters = np.zeros(4)
    best = loss(parameters)
    for step in (1, .5, .25, .125):
        increments = np.diag([3 * step, 3 * step, .035 * step, .025 * step])
        for _ in range(12):
            previous = best
            for increment in increments:
                for sign in (-1, 1):
                    proposal = parameters + sign * increment
                    score = loss(proposal)
                    if score < best:
                        parameters, best = proposal, score
            if previous == best:
                break
    matrix, shift = transform(parameters)
    return matrix * (master.width / reference.width), shift * (master.width / FIT_SIZE), best


def composite(base, sampled, profile):
    x0, y0, x1, y1 = PATCH
    original = np.asarray(base)
    neutral = original[y0:y1, x0:x1]
    output = original.copy()
    weight = np.maximum.reduce([
        faces.feature_mask(polygon, PATCH)
        for polygon in (*profile["eye_bounds"], profile["mouth_bounds"])
    ])
    weight *= (sampled[:, :, 3] == 255) & (neutral[:, :, 3] == 255)
    blended = neutral[:, :, :3] * (1 - weight[:, :, None]) + sampled[:, :, :3] * weight[:, :, None]
    output[y0:y1, x0:x1, :3] = np.rint(blended).astype(np.uint8)
    changed = np.any(output != original, axis=2)
    permitted = np.zeros(changed.shape, dtype=bool)
    permitted[y0:y1, x0:x1] = weight > 0
    assert np.array_equal(output[:, :, 3], original[:, :, 3])
    assert not np.any(changed & ~permitted)
    assert not np.any(changed[136:])
    assert int(changed.sum()) > 60
    return Image.fromarray(output), int(changed.sum())


def main():
    references = study.read(ART / "references.json")
    faces.ART = ART
    files = {}
    for family, profile in study.OUTFITS.items():
        provenance = study.read(STUDY / profile["study"])
        base_path = ROOT / f"Textures/Items/{profile['target']}.png"
        base_hash = study.digest(base_path)
        assert base_hash == provenance["portrait"]["sha256"], base_path
        with Image.open(base_path) as im:
            base = im.convert("RGBA")
        transform = provenance["pipeline"]["transform"]
        scale = transform["s"]
        origin = np.array([transform["tx"], transform["ty"] + 1])
        crop = references[family]["crop"]
        source_path = ROOT / references[family]["master"]
        assert study.digest(source_path) == references[family]["master_sha256"]
        with Image.open(source_path) as im:
            reference = im.convert("RGBA").crop(crop)
        previews = [("neutral", base)]
        for expression in faces.EXPRESSIONS:
            key = f"er_expr_{family}_{expression}"
            source = ART / f"masters/{key}.png"
            with Image.open(source) as im:
                master = im.convert("RGBA")
            matrix, shift, loss = align_head(reference, master)
            inverse = matrix / scale
            offset = shift - matrix @ (origin / scale + np.asarray(crop[:2]))
            sampled = faces.sample_feature(master, inverse, offset, np.zeros(2), PATCH)
            portrait, count = composite(base, sampled, profile)
            output = ART / f"portraits/{key}.png"
            portrait.save(output, optimize=True)
            previews.append((expression, portrait))
            files[f"Textures/Items/{key}.png"] = {
                "source": output.relative_to(ROOT).as_posix(),
                "sha256": study.digest(output),
                "master_sha256": study.digest(source),
                "base_sha256": base_hash,
                "native_to_master_matrix": inverse.tolist(),
                "native_to_master_offset": offset.tolist(),
                "fit_error": round(loss, 4),
                "changed_facial_pixels": count,
            }
        faces.preview(family, previews)
    assert len(files) == len({item["sha256"] for item in files.values()}) == 18
    assert len({item["master_sha256"] for item in files.values()}) == 18
    report = {"size": [288, 212], "mode": "RGBA", "files": files}
    (ART / "asset_validation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("Exported 18 independently redrawn expression PNGs")


if __name__ == "__main__":
    main()
