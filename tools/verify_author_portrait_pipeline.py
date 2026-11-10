"""Reproduce the author's golden image and export isolated art-review candidates."""
from pathlib import Path
import hashlib
import json
import math
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

from author_portrait_pipeline import (render, exact_area_sample, prepare_master,
                                     remove_magenta_fringe, enhance_details, bleed_transparent_rgb)
from portrait_resampling import render_portrait
from export_eleanor_beauty import PROFILES as ELEANOR
from export_lydia_portraits import FRAMING, PROFILES as LYDIA

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/author-pipeline-validation-2026-11-10'
FONT = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 15)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def label(path):
    return path.relative_to(ROOT).as_posix()


def comparison(candidate, reference):
    a, b = np.asarray(candidate), np.asarray(reference)
    am, bm = a[:, :, 3] == 255, b[:, :, 3] == 255
    common = am & bm
    diff = np.abs(a[:, :, :3].astype(np.float64) - b[:, :, :3])
    interior = np.asarray(reference.getchannel('A').filter(ImageFilter.MinFilter(3))) == 255
    return {
        'rgba_identical': bool(np.array_equal(a, b)),
        'different_rgba_pixels_including_transparent': int(np.any(a != b, axis=2).sum()),
        'alpha_different_pixels': int((am != bm).sum()),
        'silhouette_iou': float((am & bm).sum() / (am | bm).sum()),
        'visible_rgb_different_pixels': int(np.any(a[:, :, :3] != b[:, :, :3], axis=2)[common].sum()),
        'exact_visible_rgb_fraction': float(np.all(a[:, :, :3] == b[:, :, :3], axis=2)[common].mean()),
        'visible_rgb_mae': float(diff[common].mean()),
        'interior_rgb_mae': float(diff[interior & common].mean()),
        'visible_max_channel_error': int(diff[common].max()),
    }


def sheet(rows, filename, zoom=2, backgrounds=None):
    """Each row contains (label, image), on native pixels before integer zoom."""
    columns = max(map(len, rows))
    result = Image.new('RGB', (288 * columns, 240 * len(rows)), (37, 44, 53))
    draw = ImageDraw.Draw(result)
    for y, row in enumerate(rows):
        for x, (title, im) in enumerate(row):
            background = backgrounds[x] if backgrounds else (37, 44, 53)
            panel = Image.new('RGBA', (288, 212), (*background, 255))
            panel.alpha_composite(im)
            result.paste(panel.convert('RGB'), (x * 288, y * 240 + 28))
            draw.text((x * 288 + 5, y * 240 + 5), title, fill=(240, 240, 245), font=FONT)
    result.save(OUT / filename)
    if zoom != 1:
        result.resize((result.width * zoom, result.height * zoom), Image.Resampling.NEAREST).save(
            OUT / filename.replace('.png', f'-{zoom}x.png'))


def numeric_probes():
    # A single coloured source pixel with magenta RGB in the transparent pixel:
    # 35% visible area is admitted, 34.9% is rejected, without RGB contamination.
    p = np.array([[[12, 80, 160, 255], [255, 0, 255, 0]]], dtype=np.uint8)
    a, coverage = exact_area_sample(p, (1, 1), 1, -0.65, 0)
    assert abs(float(coverage[0, 0]) - .35) < 1e-12
    assert tuple(a[0, 0]) == (12, 80, 160, 255)
    b, _ = exact_area_sample(p, (1, 1), 1, -.651, 0)
    assert not b.any()
    # Half values round upward rather than Python's ties-to-even rounding.
    p = np.array([[[2, 4, 6, 255], [3, 5, 7, 255]]], dtype=np.uint8)
    a, _ = exact_area_sample(p, (1, 1), .5, 0, 0)
    assert tuple(a[0, 0]) == (3, 5, 7, 255)
    # Independently integrate literal 2D intersections to check the optimized
    # separable summation at fractional coordinates and beyond source bounds.
    rng = np.random.default_rng(4726)
    src = rng.integers(0, 256, (5, 7, 4), dtype=np.uint8)
    src[:, :, 3] = np.where(src[:, :, 3] > 110, 255, 0)
    checks = []
    for s, tx, ty in ((.37, .31, -.27), (.81, -.93, .19), (1.3, .12, -.23)):
        expected = np.zeros((6, 8, 4), dtype=np.uint8)
        for y in range(6):
            for x in range(8):
                xa, xb = (x - tx) / s, (x + 1 - tx) / s
                ya, yb = (y - ty) / s, (y + 1 - ty) / s
                weight = 0.0
                total = np.zeros(3, dtype=np.float64)
                for j in range(5):
                    for i in range(7):
                        if src[j, i, 3] < 128:
                            continue
                        w = max(0, min(xb, i + 1) - max(xa, i)) * max(0, min(yb, j + 1) - max(ya, j))
                        weight += w
                        total += w * src[j, i, :3]
                if weight > 0 and weight / ((xb - xa) * (yb - ya)) >= .35:
                    expected[y, x, :3] = np.floor(total / weight + .5).astype(np.uint8)
                    expected[y, x, 3] = 255
        actual, _ = exact_area_sample(src, (8, 6), s, tx, ty)
        assert np.array_equal(actual, expected), 'Literal overlap check failed'
        checks.append({'s': s, 'tx': tx, 'ty': ty, 'identical': True})
    return {'coverage_035': True, 'transparent_rgb_excluded': True,
            'half_up_rounding': True, 'literal_2d_integral': checks}


def boundary_probes():
    checks = {}
    # Preserve an existing alpha channel byte-for-byte, including partial alpha.
    source = np.zeros((4, 5, 4), dtype=np.uint8)
    source[:, :, :3] = (190, 30, 200)
    source[:, :, 3] = np.arange(20).reshape(4, 5) * 13
    clean, _ = prepare_master(Image.fromarray(source), magenta_key=False)
    assert np.array_equal(np.asarray(clean), source)
    checks['existing_alpha_and_rgb_preserved'] = True
    source = np.array([[[13, 90, 190, 128], [255, 0, 255, 127]]], dtype=np.uint8)
    source = np.repeat(source, 2, axis=0)
    result, _ = exact_area_sample(source, (1, 1), .5, 0, 0)
    assert tuple(result[0, 0]) == (13, 90, 190, 255)
    checks['source_alpha_127_excluded_128_included'] = True
    # Internal key-colour holes are removed; distant weak-purple material is kept.
    source = np.full((21, 25, 4), 255, dtype=np.uint8)
    source[:, :, :3] = (95, 110, 80)
    source[10, 10, :3] = (255, 0, 255)
    source[10, 12, :3] = (145, 60, 145)
    source[10, 20, :3] = (145, 60, 145)
    clean, counts = prepare_master(Image.fromarray(source), magenta_key=True)
    a = np.asarray(clean)
    assert not a[10, 10, 3] and not a[10, 12, 3] and a[10, 20, 3] == 255
    assert counts['strong'] == 1 and counts['weak_near_strong'] == 1
    assert np.array_equal(a[:, :, :3], source[:, :, :3])
    checks['strong_weak_keying_and_internal_holes'] = True
    # True opaque purple right at a cropped canvas boundary is not a fringe.
    solid = np.zeros((11, 11, 4), dtype=np.uint8)
    solid[:] = (100, 35, 105, 255)
    corrected, count = remove_magenta_fringe(solid, 4)
    assert count == 0 and np.array_equal(corrected, solid)
    checks['cropped_canvas_edge_is_not_transparency'] = True
    solid[0, 0, 3] = 0
    corrected, _ = remove_magenta_fringe(solid, 4)
    assert tuple(corrected[4, 4, :3]) == (48, 46, 50)
    assert np.array_equal(corrected[5, 5], solid[5, 5])
    assert np.array_equal(corrected[:, :, 3], solid[:, :, 3])
    checks['source_fringe_4px_eight_neighbours_alpha_fixed'] = True
    corrected, _ = remove_magenta_fringe(solid, 1)
    assert tuple(corrected[1, 1, :3]) == (48, 46, 50)
    assert np.array_equal(corrected[2, 2], solid[2, 2])
    checks['target_fringe_1px_radius'] = True
    probe = np.full((3, 3, 4), 255, dtype=np.uint8)
    probe[:, :, :3] = 80
    probe[1, 1, :3] = 86
    enhanced, _ = enhance_details(probe)
    assert np.array_equal(enhanced, probe)
    probe[1, 1, :3] = 100
    enhanced, _ = enhance_details(probe)
    assert tuple(enhanced[1, 1, :3]) == (107, 107, 107)
    assert np.array_equal(enhanced[0], probe[0])
    checks['detail_threshold_6_single_gain_and_border'] = True
    probe[0, 1, 3] = 0
    enhanced, _ = enhance_details(probe)
    assert np.array_equal(enhanced, probe)
    checks['detail_requires_five_opaque_pixels'] = True
    for centre, neighbours, expected in ((250, 0, 255), (5, 255, 0)):
        probe[:, :, 3] = 255
        probe[:, :, :3] = neighbours
        probe[1, 1, :3] = centre
        enhanced, _ = enhance_details(probe)
        assert tuple(enhanced[1, 1, :3]) == (expected,) * 3
    checks['detail_clamps_to_8bit'] = True
    rng = np.random.default_rng(1491)
    probe = rng.integers(0, 256, (8, 9, 4), dtype=np.uint8)
    probe[:, :, 3] = 255
    a, _ = enhance_details(probe)
    b, _ = enhance_details(np.flip(probe, axis=(0, 1)))
    assert np.array_equal(a, np.flip(b, axis=(0, 1)))
    checks['detail_reads_frozen_copy_no_order_accumulation'] = True
    probe = np.zeros((7, 7, 4), dtype=np.uint8)
    probe[:, :, :3] = (255, 0, 255)
    probe[3, 3] = (17, 88, 140, 255)
    filled = bleed_transparent_rgb(probe)
    assert np.array_equal(filled[:, :, 3], probe[:, :, 3])
    assert np.all(filled[:, :, :3] == (17, 88, 140))
    checks['rgb_bleed_fills_holes_without_changing_alpha'] = True
    empty = np.zeros((7, 7, 4), dtype=np.uint8)
    assert np.array_equal(bleed_transparent_rgb(empty), empty)
    try:
        render(Image.fromarray(empty), 5, 3, 6)
    except ValueError as error:
        assert str(error) == 'Empty master'
    else:
        raise AssertionError('Empty master was silently accepted')
    checks['empty_master_rejected_and_empty_bleed_stable'] = True
    probe = empty.copy()
    probe[2, 2] = (20, 80, 130, 255)
    probe[4, 4] = (200, 90, 30, 255)
    output, _, _ = render(Image.fromarray(probe), 3, 3.5, 5, canvas_size=(7, 7),
                           add_top_row=False, fringe=False, detail=False)
    assert np.array_equal(np.asarray(output)[:, :, 3], probe[:, :, 3])
    checks['isolated_hair_jewellery_pixels_not_deleted'] = True
    return checks


def main():
    reference_dir = OUT / 'reference'
    reference_dir.mkdir(parents=True, exist_ok=True)
    for filename in ('author-source.png', 'author-sprite.png'):
        destination = reference_dir / filename
        if not destination.exists():
            shutil.copy2(ROOT / 'dist/reference-pipeline-20261110/supplied-reference' / filename, destination)
    source_path, target_path = reference_dir / 'author-source.png', reference_dir / 'author-sprite.png'
    source = Image.open(source_path).convert('RGBA')
    target = Image.open(target_path).convert('RGBA')
    exact, golden, stages = render(source, 150, 144.5, 211, magenta_key=True)
    repeated, _, _ = render(source, 150, 144.5, 211, magenta_key=True)
    assert np.array_equal(np.asarray(exact), np.asarray(repeated))
    for stage, image in stages.items():
        image.save(reference_dir / f'{stage}.png')
    golden['comparison'] = comparison(exact, target)
    assert golden['clean_bbox'] == [280, 15, 1174, 1070]
    assert golden['output_bbox'] == [81, 62, 208, 212]
    assert golden['comparison']['rgba_identical'], 'Author golden image is not reproduced exactly'
    assert not np.asarray(exact)[0].any(), 'Top padding row must be zero RGBA'
    golden['source_sha256'], golden['target_sha256'] = digest(source_path), digest(target_path)
    golden['decoded_output_sha256'] = hashlib.sha256(exact.tobytes()).hexdigest()
    golden['repeat_identical'] = True
    basic, _ = render_portrait(stages['clean_master'], (127, 150), (81, 62))
    golden['previous_box_comparison'] = comparison(basic, target)
    ablations = {}
    for name, change in (('without_detail', {'detail': False}), ('without_fringe', {'fringe': False}),
                         ('coverage_050', {'coverage_threshold': .5})):
        variant, _, _ = render(source, 150, 144.5, 211, magenta_key=True, **change)
        variant.save(reference_dir / f'{name}.png')
        ablations[name] = comparison(variant, target)
    golden['ablations'] = ablations
    sheet([[('Author reference', target), ('Exact B/detail', exact), ('Previous BOX', basic)]],
          'reference-comparison.png', zoom=3)
    (reference_dir / 'report.json').write_text(json.dumps(golden, indent=2) + '\n', encoding='utf8')

    profiles = []
    for name in ('maid', 'gala'):
        p = ELEANOR[name]
        profiles.append((name, ROOT / p['source'], ROOT / p['file'], p['size'][1],
                         p['position'][0] + p['size'][0] / 2, p['position'][1] + p['size'][1] - 1))
    for name in LYDIA:
        frame = FRAMING[name]
        src = ROOT / 'Art/dual-romance-v0.3/masters' / f'lydia_{name}.png'
        im = Image.open(src).convert('RGBA')
        bbox = im.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
        width = round((bbox[2] - bbox[0]) * frame['height'] / (bbox[3] - bbox[1]))
        profiles.append(('lydia_' + name, src, ROOT / 'Textures/Items' / LYDIA[name][0],
                         frame['height'], (288 - width) // 2 + width / 2, frame['top'] + frame['height'] - 1))
    rev = ROOT / 'Art/revolution-route-2026-11-10'
    profiles.append(('revolution_v3', rev / 'masters/eleanor_revolution_undersuit_beauty_v3.png',
                     rev / 'portraits/er_portrait_eleanor_revolution_undersuit_female_beauty_v3.png', 220, 139, 281))
    protected = {path: digest(path) for _, src, current, *_ in profiles for path in (src, current)}
    panels, project_reports = {}, []
    for name, source_path, current_path, height, cx, bottom in profiles:
        image = Image.open(source_path).convert('RGBA')
        current = Image.open(current_path).convert('RGBA')
        strict, record, _ = render(image, height, cx, bottom)
        keep_edge, _, _ = render(image, height, cx, bottom, fringe=False)
        directory = OUT / 'candidates' / name
        directory.mkdir(parents=True, exist_ok=True)
        strict.save(directory / 'author_strict.png')
        keep_edge.save(directory / 'preserve_coloured_edges.png')
        shutil.copy2(current_path, directory / 'previous.png')
        record.update(name=name, source=label(source_path), source_sha256=digest(source_path),
                      previous=label(current_path), previous_sha256=digest(current_path),
                      strict_sha256=digest(directory / 'author_strict.png'),
                      strict_vs_previous=comparison(strict, current),
                      fringe_effect=comparison(strict, keep_edge))
        assert record['alpha_values'] == [0, 255]
        assert np.array_equal(np.asarray(image), np.asarray(Image.open(source_path).convert('RGBA')))
        (directory / 'report.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf8')
        project_reports.append(record)
        panels[name] = [(name + ' / previous', current), ('Author strict', strict), ('Keep coloured edges', keep_edge)]
        if name in ('maid', 'gala', 'revolution_v3'):
            mattes = [(255, 255, 255), (0, 0, 0), (37, 44, 53)]
            sheet([[('Strict / ' + title, strict) for title in ('white', 'black', 'grey')],
                   [('Keep edges / ' + title, keep_edge) for title in ('white', 'black', 'grey')]],
                  f'{name}-matte-review.png', zoom=3, backgrounds=mattes)
    sheet([panels[name] for name in ('maid', 'gala', 'revolution_v3')], 'eleanor-comparison.png')
    sheet([panels['lydia_' + name] for name in LYDIA], 'lydia-comparison.png')
    assert all(digest(path) == before for path, before in protected.items()), 'Input/production asset changed'
    result = {'algorithm_document': 'Art/图片生成提示词.txt',
              'algorithm_document_sha256': digest(ROOT / 'Art/图片生成提示词.txt'),
              'implementation_sha256': digest(ROOT / 'tools/author_portrait_pipeline.py'),
              'verification_script_sha256': digest(Path(__file__)),
              'golden': golden, 'numeric_probes': numeric_probes(), 'boundary_probes': boundary_probes(),
              'project_candidates': project_reports,
              'protected_inputs_unchanged': True,
              'boundary_interpretation': 'Colour-fringe distances use actual transparent pixels, not the cropped canvas exterior. Only area sampling treats outside as transparent.',
              'scope': 'Isolated artwork validation; production portraits and runtime files unchanged.'}
    (OUT / 'verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print(json.dumps({'golden': golden['comparison'], 'previous_box': golden['previous_box_comparison'],
                      'candidates': [{k: p[k] for k in ('name', 'source_fringe_pixels', 'target_fringe_pixels')} for p in project_reports],
                      'protected_inputs_unchanged': True}, ensure_ascii=False))


if __name__ == '__main__':
    main()
