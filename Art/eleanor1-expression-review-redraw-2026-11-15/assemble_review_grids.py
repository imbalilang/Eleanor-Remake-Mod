"""Register the eight same-outfit proof sheets and prepare numbered candidates."""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parent
ROOT = ART.parents[1]
sys.path.insert(0, str(ROOT / "tools"))
from export_registered_expression import assemble, digest, load_image
from expression_face_bounds import LOWER_FACE_EDGES


def rectangle(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def locate_cell(reference, cell, span):
    """Estimate the pose from static hair and outer-edge pixels for review."""
    image = np.asarray(cell.resize((span, span), Image.Resampling.LANCZOS).convert("RGB"), dtype=float)
    compare = np.asarray(reference.resize((span, span), Image.Resampling.LANCZOS).convert("RGB"), dtype=float)
    y, x = np.mgrid[0:span:2, 0:span:2]
    stable = ((y >= 5) & (y <= 36) & (x >= 8) & (x <= 76)) | (
        (y >= 32) & (y <= 71) & ((x < 12) | (x > 72)) & (x >= 4) & (x <= 80))
    x, y = x[stable], y[stable]
    anchor = compare[y, x]
    best = None
    for scale in (.94, .97, 1.0, 1.03, 1.06):
        sx, sy = (x - span / 2) * scale + span / 2, (y - span / 2) * scale + span / 2
        for dy in range(-4, 5):
            yy = np.clip(np.rint(sy + dy).astype(int), 0, span - 1)
            for dx in range(-4, 5):
                xx = np.clip(np.rint(sx + dx).astype(int), 0, span - 1)
                error = float(np.minimum(np.abs(image[yy, xx] - anchor), 75).mean())
                candidate = (error, scale, dx, dy)
                if best is None or candidate < best:
                    best = candidate
    return best


def mapped_feature(name, center, shape, project, normal_scale=1., source_center=None):
    x, y = center
    sx, sy = source_center or center
    return {"feature": name, "polygon": shape, "target_points": [(x, y), (x + 1., y)],
            "source_points": [project(sx, sy), project(sx + 1., sy)], "normal_scale": normal_scale}


def make_features(profile, kind, project, adjustments):
    mx, my = profile["mouth"]
    mouth = [(mx - 8, my - 2), (mx - 4, my - 4), (mx + 7, my - 5), (mx + 9, my - 1),
             (mx + 6, my + 3), (mx + 1, my + 4), (mx - 7, my + 3)]
    dx, dy = adjustments.get("mouth", (0., 0.))
    result = [mapped_feature("mouth", (mx, my), mouth, project, .85, (mx + dx, my + dy))]
    result[0]["match_boundary_colour"] = True
    for index, eye in enumerate(profile["eyes"]):
        (lx, ly), (rx, ry) = eye
        x, y = (lx + rx) / 2, (ly + ry) / 2
        label = "near" if index == 0 else "far"
        if kind in ("angry", "hurt", "sad", "determined", "tsun"):
            # Keep the lower half of the patch clear of the lash line.
            brow_y = y - (9.7 if index == 0 else 8.7)
            if index == 0:
                brow = [(lx + 1, brow_y + 1), (x - 2, brow_y - 4), (rx - 4, brow_y - 5),
                        (rx - 1, brow_y + 1), (rx - 1, brow_y + 3), (lx + 1, brow_y + 3)]
            else:
                brow = [(lx, brow_y), (x - 1, brow_y - 4), (rx - 3, brow_y - 4),
                        (rx - 2, brow_y), (x + 2, brow_y + 2), (lx + 1, brow_y + 3)]
            dx, dy = adjustments.get(f"{label} brow", (0., 0.))
            result.append(mapped_feature(f"{label} brow", (x, brow_y), brow, project, .67,
                                         (x + dx, brow_y + dy)))
        if kind in ("shy", "tsun"):
            blush = [(lx + 3, y + 4), (x, y + 4), (rx - 1, y + 3), (rx - 2, y + 8),
                     (x, y + 9), (lx + 4, y + 8)]
            result.append(mapped_feature(f"{label} blush", (x, y + 6), blush, project))
        if kind == "tired":
            # The shape is optional per eye. Jaw, eye corners and nose are
            # protected separately, so eyelid edits cannot move the eye span.
            lid = [(lx + 1, ly), (lx + 4, ly - 5), (x, y - 6), (rx - 3, ry - 4),
                   (rx - 1, ry), (rx - 2, ry + 4), (x, y + 5), (lx + 3, ly + 4)]
            dx, dy = adjustments.get(f"{label} lids", (0., 0.))
            result.append(mapped_feature(f"{label} lids", (x, y), lid, project, 1., (x + dx, y + dy)))
    return [f for f in result if f["feature"] not in adjustments.get("skip", [])]


def protection(family, profile, kind):
    # The lower face retains its own actual jaw, even in families whose chin
    # sits higher than the shared 135px lower-body boundary.
    result = [rectangle(0, 135, 287, 211)]
    if family in LOWER_FACE_EDGES:
        border = LOWER_FACE_EDGES[family]
        result.append([(border[0][0], 140), *border, (border[-1][0], 140)])
    mx, my = profile["mouth"]
    result.append(rectangle(mx - 5, my - 13, mx + 2, my - 6))
    for (lx, ly), (rx, ry) in profile["eyes"]:
        if kind == "tired":
            result.extend([rectangle(lx - 1, ly - 1, lx + 1, ly + 2),
                           rectangle(rx - 1, ry - 1, rx + 1, ry + 2)])
        else:
            result.append(rectangle(lx - 2, min(ly, ry) - 2, rx + 2, max(ly, ry) + 3))
    return result


def preview(family, items, profile):
    step_x, step_y = 226, 182
    x = round(profile["mouth"][0]) - 32
    crop = (x, 92, x + 64, 143)
    canvas = Image.new("RGB", (step_x * 5, step_y * 2 * ((len(items) + 4) // 5)), "#252C35")
    text = ImageDraw.Draw(canvas)
    for index, (record, original, edited) in enumerate(items):
        column, group = index % 5, index // 5
        for row, im in enumerate((original, edited)):
            px, py = column * step_x, (group * 2 + row) * step_y
            caption = f'#{record["number"]} {record["expression"]} {"old" if row == 0 else "new"}'
            text.text((px + 8, py + 4), caption, fill="white")
            face = im.crop(crop).resize((192, 153), Image.Resampling.NEAREST)
            canvas.paste(face, (px + 8, py + 22), face)
    canvas.save(ART / "previews" / f"{family}-before-after.png", optimize=True)


def main():
    spec = json.loads((ART / "family-grid-spec.json").read_text(encoding="utf-8"))
    corrections_path = ART / "family-feature-review.json"
    corrections = json.loads(corrections_path.read_text(encoding="utf-8")) if corrections_path.is_file() else {}
    measurements = {}
    report = []
    before = ART / "before" / "families"
    before.mkdir(exist_ok=True)
    for family, row in spec.items():
        base_path, template_path = ROOT / row["runtime_base"], ROOT / row["sheet"]
        if digest(base_path) != row["runtime_base_sha256"] or digest(template_path) != row["sheet_hash"]:
            raise ValueError(f"The review reference for {family} has changed")
        base = load_image(base_path)
        reference = load_image(ART / "references" / f"{family}-tile.png")
        sheet_path = ART / "masters" / f"{family}-grid.png"
        sheet = load_image(sheet_path)
        columns, rows = row["columns"], row["rows"]
        tile_w, tile_h = sheet.width / columns, sheet.height / rows
        span = row["native_sheet_span"]
        native_x, native_y = row["native_sheet_origin"]
        revisions = []
        for i, job in enumerate(row["entries"]):
            expression = job["expression"]
            tile_x, tile_y = i % columns * tile_w, i // columns * tile_h
            part = sheet.crop((round(tile_x), round(tile_y), round(tile_x + tile_w), round(tile_y + tile_h)))
            error, scale, dx, dy = locate_cell(reference, part, span)

            def project(x, y):
                cx = ((x - native_x - span / 2) * scale + span / 2 + dx) * tile_w / span
                cy = ((y - native_y - span / 2) * scale + span / 2 + dy) * tile_h / span
                return (tile_x + cx, tile_y + cy)

            profile = row["profile"]
            features = make_features(profile, expression, project, corrections.get(job["number"], {}))
            locked = protection(family, profile, expression)
            edited, changed = assemble(base, sheet, features, locked)
            key = Path(job["asset"]).name
            output = ART / "portraits" / key
            edited.save(output, optimize=True)
            backup = before / key
            if not backup.exists():
                source = ROOT / job["asset"]
                if digest(source) != job["current_sha256"]:
                    raise ValueError(f"The expression {key} changed after the numbered audit")
                backup.write_bytes(source.read_bytes())
            revisions.append((job, load_image(backup), edited))
            report.append({"number": job["number"], "sprite": key, "sha256": digest(output), "changed_pixels": changed})
            measurements[job["number"]] = {"family": family, "face_sheet": sheet_path.relative_to(ROOT).as_posix(),
                                           "face_sheet_sha256": digest(sheet_path), "alignment": [error, scale, dx, dy],
                                           "features": features, "protected": locked}
        preview(family, revisions, row["profile"])
    # Daily shy and its legacy flustered tag are the same image by design.
    shy = ART / "portraits" / "er_expr_lydia_daily_shy.png"
    alias = ART / "portraits" / "er_expr_lydia_daily_flustered.png"
    alias.write_bytes(shy.read_bytes())
    report.append({"number": "102", "sprite": alias.name, "sha256": digest(alias), "same_image_as": "095"})
    (ART / "family-registration.json").write_text(json.dumps(measurements, indent=2) + "\n", encoding="utf-8")
    (ART / "portraits" / "family-registration-report.json").write_text(json.dumps(report, indent=2) + "\n",
                                                                      encoding="utf-8")
    for family in spec:
        fit = [m["alignment"] for m in measurements.values() if m["family"] == family]
        print(family, len(fit), f"mean hair fit error {np.mean([m[0] for m in fit]):.2f}",
              "transforms", sorted(set(tuple(m[1:]) for m in fit)))


if __name__ == "__main__":
    main()
