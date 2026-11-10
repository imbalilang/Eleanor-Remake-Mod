"""Regression check of actual jaw/neck pixels, independent of export masks."""
from pathlib import Path
import hashlib
import json

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/expression-chin-fix-2026-11-10'
REFS = ROOT / 'Art/expressions-and-endings-2026-11-10/references_local'
TEXTURES = ROOT / 'Textures/Items'
BASES = {
    'eleanor1': REFS / 'eleanor1_neutral.png',
    'eleanor_scav': REFS / 'eleanor_scav_neutral.png',
    'eleanor_maid': REFS / 'eleanor_maid_neutral.png',
    'lydia_daily': TEXTURES / 'er_portrait_lydia_female_beauty.png',
    'lydia_date': TEXTURES / 'er_portrait_lydia_date_female_beauty.png',
}
# Independently picked base-jaw and neck patches. In particular, the first
# scavenger patch includes the protrusion visible in the player's screenshot.
# Keep these tests independent of the exporter's polygon and feathering logic.
PATCHES = {
    'eleanor1': [(136, 136, 148, 143), (127, 131, 131, 135), (152, 131, 156, 135)],
    'eleanor_scav': [(130, 136, 142, 144), (122, 132, 126, 136), (145, 132, 149, 136)],
    'eleanor_maid': [(137, 138, 150, 145), (126, 133, 130, 137), (154, 132, 158, 136)],
    'lydia_daily': [(132, 137, 147, 144), (123, 133, 127, 137), (151, 132, 155, 136)],
    'lydia_date': [(132, 137, 147, 144), (123, 133, 127, 137), (151, 132, 155, 136)],
}


def rgba(path):
    return np.asarray(Image.open(path).convert('RGBA'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check():
    records = []
    for family, base_path in BASES.items():
        base = rgba(base_path)
        protected = np.zeros(base.shape[:2], bool)
        for x0, y0, x1, y1 in PATCHES[family]:
            protected[y0:y1, x0:x1] = True
        protected &= base[:, :, 3] == 255
        for path in sorted(TEXTURES.glob('er_expr_' + family + '_*.png')):
            current = rgba(path)
            assert current.shape == base.shape == (212, 288, 4), path.name
            assert np.array_equal(current[:, :, 3], base[:, :, 3]), path.name + ': alpha drift'
            errors = int((np.any(current != base, axis=2) & protected).sum())
            assert errors == 0, f'{path.name}: {errors} changed jaw/neck pixels'
            features = np.any(current[99:133, 110:171, :3] != base[99:133, 110:171, :3], axis=2)
            assert features.sum() >= 50, path.name + ': expression lost'
            record = {'file': path.name, 'family': family, 'sha256': sha(path),
                      'protected_jaw_neck_pixels': int(protected.sum()),
                      'jaw_neck_different_pixels': errors,
                      'expression_feature_changed_pixels': int(features.sum())}
            before_path = ART / 'before' / path.name
            if before_path.exists():
                before = rgba(before_path)
                record['previous_jaw_neck_different_pixels'] = int((np.any(before != base, axis=2) & protected).sum())
                # The fix must not change any already-generated eye/brow pixel.
                visible = base[95:123, 100:180, 3] == 255
                assert np.array_equal(before[95:123, 100:180][visible], current[95:123, 100:180][visible]), path.name + ': eyes changed'
                record['eyes_and_brows_unchanged_from_previous'] = True
            records.append(record)
    assert len(records) == 27, 'Review expression inventory if adding a family'
    original = json.loads((ART / 'before_inventory.json').read_text(encoding='utf-8'))
    for name, before_sha in original.items():
        if not name.startswith('er_expr_'):
            assert sha(TEXTURES / name) == before_sha, 'Out-of-scope artwork changed: ' + name
    affected = sum(r.get('previous_jaw_neck_different_pixels', 0) > 0 for r in records)
    reported = next(r for r in records if r['file'] == 'er_expr_eleanor_scav_soft.png')
    if 'previous_jaw_neck_different_pixels' in reported:
        assert reported['previous_jaw_neck_different_pixels'] > 0, 'Regression fixture must reproduce reported chin'
    result = {'expressions_checked': len(records), 'families_checked': len(BASES),
              'previously_changed_jaw_neck_patches': affected,
              'jaw_neck_errors_after': 0, 'non_expression_art_unchanged': True,
              'published': False, 'game_visual_test_performed': False, 'assets': records}
    (ART / 'regression_results.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k != 'assets'}))


if __name__ == '__main__':
    check()
