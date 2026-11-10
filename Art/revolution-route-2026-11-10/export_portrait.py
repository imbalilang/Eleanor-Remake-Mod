"""Export the revolutionary outfit design without changing runtime assets."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from portrait_resampling import render_portrait


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export(reference=None, revision='v3'):
    profile_path = HERE / f'framing_{revision}.json'
    profile = json.loads(profile_path.read_text(encoding='utf-8'))
    source = HERE / profile['source']
    with Image.open(source) as opened:
        master = opened.convert('RGBA')
    assert list(master.size) == profile['source_size'], 'Recalibrate a changed master'
    bbox = master.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
    assert list(bbox) == profile['source_alpha128_bbox'], 'Recalibrate changed framing'
    sw, sh = bbox[2] - bbox[0], bbox[3] - bbox[1]
    height = profile['scaled_figure_height']
    size = (round(sw * height / sh), height)
    position = tuple(profile['position'])
    canvas, _ = render_portrait(master, size, position, canvas_size=tuple(profile['canvas_size']))
    repeated, _ = render_portrait(master, size, position, canvas_size=tuple(profile['canvas_size']))
    assert canvas.tobytes() == repeated.tobytes(), 'Non-repeatable export'
    pixels = list(canvas.get_flattened_data())
    assert canvas.mode == 'RGBA' and canvas.size == (288, 212)
    assert {p[3] for p in pixels} == {0, 255}
    assert all(p[:3] == (0, 0, 0) for p in pixels if not p[3])
    assert canvas.getchannel('A').getbbox()[3] == canvas.height
    output = HERE / profile['portrait']
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)

    previews = HERE / 'previews'
    previews.mkdir(exist_ok=True)
    bg = Image.new('RGBA', canvas.size, (34, 32, 40, 255))
    bg.alpha_composite(canvas)
    enlarged = bg.resize((576, 424), Image.Resampling.NEAREST).convert('RGB')
    enlarged.save(previews / f'revolution_undersuit_{revision}_2x.png')

    report = {
        'source': profile['source'], 'source_sha256': sha256(source),
        'prompt_sha256': sha256(HERE / profile['generation']['prompt']),
        'profile_sha256': sha256(profile_path),
        'output': profile['portrait'], 'output_sha256': sha256(output),
        'size': list(canvas.size), 'mode': canvas.mode,
        'source_size': list(master.size), 'source_alpha128_bbox': list(bbox),
        'scaled_figure_size': list(size), 'position': list(position),
        'native_alpha_bbox': list(canvas.getchannel('A').getbbox()),
        'bottom_crop_pixels': position[1] + height - canvas.height,
        'projected_landmarks_approx': {
            key: [round(position[0] + (p[0] - bbox[0]) * size[0] / sw, 2),
                  round(position[1] + (p[1] - bbox[1]) * size[1] / sh, 2)]
            for key, p in profile['source_landmarks_approx'].items()
        },
        'alpha_values': [0, 255],
        'opaque_colours': len({p[:3] for p in pixels if p[3]}),
        'repeat_export_identical': True,
        'export': profile['export'], 'generation': profile['generation'],
        'visual_review': 'Master and integer-2x reference comparison inspected: waist-up master, complete head, matched facial scale, clear costume silhouette.',
        'status': 'Art candidate only; not registered in gameplay; in-game review pending.'
    }
    if reference is not None:
        with Image.open(reference) as opened:
            ref = opened.convert('RGBA')
        assert ref.size == canvas.size
        ref_bg = Image.new('RGBA', ref.size, (34, 32, 40, 255))
        ref_bg.alpha_composite(ref)
        sheet = Image.new('RGB', (1152, 468), (34, 32, 40))
        sheet.paste(ref_bg.resize((576, 424), Image.Resampling.NEAREST).convert('RGB'), (0, 44))
        sheet.paste(enlarged, (576, 44))
        font_path = Path('C:/Windows/Fonts/segoeui.ttf')
        font = ImageFont.truetype(str(font_path), 19) if font_path.exists() else ImageFont.load_default()
        draw = ImageDraw.Draw(sheet)
        draw.text((16, 12), "Maiden's Pawnshop / revolutionary reference", font=font, fill=(235, 235, 239))
        draw.text((592, 12), f'New tactical bodysuit / {revision}', font=font, fill=(235, 235, 239))
        sheet.save(previews / f'revolution_undersuit_{revision}_comparison_2x.png')
        report['reference_sha256'] = sha256(reference)
    (HERE / f'validation_{revision}.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'portrait': str(output), 'size': list(canvas.size),
                      'alpha_bbox': report['native_alpha_bbox'],
                      'projected_landmarks': report['projected_landmarks_approx'],
                      'repeat_export_identical': True}, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', choices=['v2', 'v3'], default='v3', help='Artwork revision; default is the current black textile design')
    parser.add_argument('--reference', type=Path, help='Optional local reference for a comparison preview; never copied into runtime assets')
    args = parser.parse_args()
    export(args.reference, args.revision)
