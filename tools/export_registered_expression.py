"""Export one expression from reviewed same-outfit artwork to its neutral sprite.

The job names the source and neutral images, with two corresponding landmarks
and a native polygon for each painted feature. Art review still covers the
landmark choices, the shape of the feature and the blend at native size.
"""
import argparse
import json
from hashlib import sha256
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from author_portrait_pipeline import exact_area_sample, enhance_details


ROOT = Path(__file__).resolve().parents[1]
SIZE = (288, 212)
SUPERSAMPLE = 6


def load_image(path):
    with Image.open(path) as image:
        return image.convert("RGBA")


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def polygon_mask(polygons, size, origin=(0, 0)):
    mask = Image.new("L", size)
    draw = ImageDraw.Draw(mask)
    for polygon in polygons:
        draw.polygon([(x - origin[0], y - origin[1]) for x, y in polygon], fill=255)
    return mask


def sample_feature(master, feature, box):
    x0, y0, x1, y1 = box
    width, height = x1 - x0, y1 - y0
    source_start, source_end = (complex(*p) for p in feature["source_points"])
    target_start, target_end = (complex(*p) for p in feature["target_points"])
    # A fit per feature holds its two landmarks. An explicitly reviewed
    # normal scale can keep a thin brow patch clear of the neighboring lid.
    target_axis = target_end - target_start
    source_axis = source_end - source_start
    normal_scale = feature.get("normal_scale", 1.0)

    def map_delta(delta):
        projection = delta / target_axis
        return source_axis * complex(projection.real, projection.imag * normal_scale)

    shift = source_start + map_delta(complex(x0, y0) - target_start)
    dx, dy = map_delta(1 / SUPERSAMPLE), map_delta(1j / SUPERSAMPLE)
    warped = master.transform((width * SUPERSAMPLE, height * SUPERSAMPLE),
                              Image.Transform.AFFINE, (dx.real, dy.real, shift.real, dx.imag, dy.imag, shift.imag),
                              Image.Resampling.BICUBIC)
    sampled, _ = exact_area_sample(np.asarray(warped), (width, height), 1 / SUPERSAMPLE, 0, 0)
    detailed, _ = enhance_details(sampled)
    return detailed


def assemble(base, master, features, protected):
    if base.size != SIZE:
        raise ValueError(f"Expected the in-game neutral portrait to be {SIZE}")
    neutral = np.asarray(base)
    result = neutral.copy()
    locked = np.asarray(polygon_mask(protected, SIZE)) > 0
    allowed = np.zeros(locked.shape, dtype=bool)
    for feature in features:
        stencil = polygon_mask([feature["polygon"]], SIZE)
        x0, y0, x1, y1 = stencil.getbbox()
        box = (max(0, x0 - 2), max(0, y0 - 2), min(SIZE[0], x1 + 2), min(SIZE[1], y1 + 2))
        x0, y0, x1, y1 = box
        output = result[y0:y1, x0:x1]
        # The feather ends inside the painted region. Protected landmarks and
        # transparent parts of the original belong to the neutral sprite.
        stencil = stencil.crop(box)
        sample = sample_feature(master, feature, box)
        visible = (sample[:, :, 3] == 255) & (output[:, :, 3] == 255)
        support = (np.asarray(stencil) > 0) & visible & ~locked[y0:y1, x0:x1]
        weight = np.asarray(stencil.filter(ImageFilter.GaussianBlur(.65))) / 255 * support
        colour = sample[:, :, :3].astype(float)
        if feature.get("match_boundary_colour"):
            transition = (weight > 0) & (weight < .85)
            if transition.any():
                neutral_skin = neutral[y0:y1, x0:x1, :3][transition]
                difference = np.median(neutral_skin - colour[transition], axis=0)
                colour = np.clip(colour + np.clip(difference, -16, 16), 0, 255)
        weight = weight[:, :, None]
        output[:, :, :3] = np.floor(output[:, :, :3] * (1 - weight) + colour * weight + .5)
        allowed[y0:y1, x0:x1] |= support

    changed = np.any(result != neutral, axis=2)
    if not changed.any() or (changed & (locked | ~allowed)).any():
        raise ValueError("Expression is empty or changes pixels outside its reviewed features")
    if not np.array_equal(result[:, :, 3], neutral[:, :, 3]):
        raise ValueError("Expression changes the neutral silhouette")
    return Image.fromarray(result), int(changed.sum())


def export(jobs, output):
    base_path = ROOT / jobs["neutral"]
    if digest(base_path) != jobs["neutral_sha256"]:
        raise ValueError("Neutral portrait changed after the landmarks were recorded")
    base = load_image(base_path)
    output.mkdir(parents=True, exist_ok=True)
    records = []
    for job in jobs["expressions"]:
        source_path = ROOT / job["master"]
        reference_path = ROOT / job["edit_reference"]
        if digest(source_path) != job["master_sha256"] or digest(reference_path) != job["edit_reference_sha256"]:
            raise ValueError(f"Master or edit reference changed after registration: {source_path}")
        protected = jobs["protected"] + job.get("protected", [])
        result, changed = assemble(base, load_image(source_path), job["features"], protected)
        path = output / f"er_expr_{jobs['family']}_{job['expression']}.png"
        result.save(path, optimize=True)
        records.append({"number": job["number"], "sprite": path.name, "sha256": digest(path),
                        "changed_pixels": changed})
    (output / "registration-report.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
    return records


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("jobs", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    export(json.loads(args.jobs.read_text(encoding="utf-8")), args.output)
