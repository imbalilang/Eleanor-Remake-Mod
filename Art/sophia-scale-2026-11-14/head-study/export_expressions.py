"""Reassemble Sophia's expression artwork on the two approved head studies."""
import importlib.util
import json
from hashlib import sha256
from pathlib import Path

import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parent
ROOT = ART.parents[2]
SOURCE = ROOT / "Art/expression-expansion-2026-11-14"
spec = importlib.util.spec_from_file_location("expression_export", SOURCE / "export_faces.py")
expression_export = importlib.util.module_from_spec(spec)
spec.loader.exec_module(expression_export)
faces = expression_export.export

# Visible eyelid endpoints and mouth centres read from the new 288×212 bases.
# Keep the hair between the eyes, the nose highlight and the jaw outside the masks.
OUTFITS = {
    "sophia": {
        "study": "asset_validation.json",
        "target": "er_portrait_mother_female_beauty",
        "eyes": [[[123, 113.8], [138.5, 113.5]], [[150.5, 114.1], [165, 113.5]]],
        "mouth": [144.6, 132],
        "eye_bounds": [
            [[127, 104], [138, 104], [141, 111], [140, 116], [136, 121], [124, 120], [120, 116], [121, 112]],
            [[150, 105], [158, 106], [166, 109], [169, 113], [165, 119], [155, 121], [150, 118], [148, 113]],
        ],
        "mouth_bounds": [[135, 130], [140, 128], [153, 129], [155, 132], [151, 135], [139, 135], [135, 133]],
    },
    "sophia_home": {
        "study": "home_asset_validation.json",
        "target": "er_portrait_mother_home_female_beauty",
        "eyes": [[[124, 116], [138.5, 115.5]], [[149.5, 112.4], [165, 110.6]]],
        "mouth": [146, 131.7],
        "eye_bounds": [
            [[125, 105], [138, 105], [141, 112], [140, 118], [136, 122], [125, 122], [120, 119], [120, 115]],
            [[149, 104], [157, 104], [166, 106], [169, 110], [165, 115], [154, 119], [149, 117], [147, 112]],
        ],
        "mouth_bounds": [[135, 130], [145, 128], [154, 128], [156, 131], [150, 135], [141, 135], [136, 133]],
    },
}


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def main():
    landmarks = read(SOURCE / "landmarks.json")["sophia"]
    previous = read(ART.parent / "asset_validation.json")["files"]
    old_expressions = {item["key"]: item for item in read(SOURCE / "asset_validation.json")["expression_art"]}
    faces.ART = ART
    files = {}
    for family, profile in OUTFITS.items():
        study = read(ART / profile["study"])
        source = ROOT / study["portrait"]["file"]
        assert digest(source) == study["portrait"]["sha256"], source
        with Image.open(source) as neutral:
            base = neutral.convert("RGBA")
        assert base.size == faces.SIZE
        assert set(base.getchannel("A").get_flattened_data()) == {0, 255}
        assert not np.any(np.asarray(base)[0])
        target = f"Textures/Items/{profile['target']}.png"
        files[target] = {
            "source": source.relative_to(ROOT).as_posix(),
            "sha256": digest(source),
            "previous_sha256": previous[target]["output_sha256"],
        }
        variants = [("neutral", base)]
        for expression in faces.EXPRESSIONS:
            key = f"er_expr_{family}_{expression}"
            source = SOURCE / f"masters/er_expr_sophia_{expression}.png"
            source_hash = digest(source)
            assert source_hash == old_expressions[key]["source_sha256"], source
            with Image.open(source) as master:
                scale = np.array(master.size) / 1000
                points = [(np.asarray(group) * scale).tolist() for group in landmarks[expression]]
                portrait, count = faces.export_portrait(
                    base, master.convert("RGBA"), profile, points, patch_box=(117, 103, 170, 136),
                )
            output = ART / f"portraits/{key}.png"
            portrait.save(output, optimize=True)
            variants.append((expression, portrait))
            target = f"Textures/Items/{key}.png"
            files[target] = {
                "source": output.relative_to(ROOT).as_posix(),
                "sha256": digest(output),
                "previous_sha256": previous[target]["output_sha256"],
                "master_sha256": source_hash,
                "changed_facial_pixels": count,
            }
        faces.preview(family, variants)

    assert len(files) == 20
    report = {"size": [288, 212], "mode": "RGBA", "files": files}
    (ART / "expression_validation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("Verified 2 neutral portraits and exported 18 expression PNGs.")


if __name__ == "__main__":
    main()
