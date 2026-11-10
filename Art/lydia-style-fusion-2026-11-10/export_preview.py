"""Export the Lydia fusion candidates with the verified author B/detail recipe."""
from pathlib import Path
import argparse
import hashlib
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
from author_portrait_pipeline import render


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export(revision):
    master = HERE / ('lydia_date_master_v1.png' if revision == 'date_v1' else f'lydia_fusion_master_{revision}.png')
    with Image.open(master) as original:
        image = original.convert('RGBA')
    parameters = dict(visible_height=154, centre_x=144.5, bottom=215,
                      magenta_key=False, fringe=False)
    if revision in ('v3', 'v4', 'date_v1'):
        # Keep v2's source-to-canvas mapping. Changes to hair/shoulder silhouette
        # must not silently change facial scale in the comparison.
        previous = Image.open(HERE / 'lydia_fusion_master_v2.png').convert('RGBA')
        assert previous.size == image.size, 'Recalibrate after a source canvas size change'
        old = previous.getchannel('A').point(lambda n: 255 if n >= 128 else 0).getbbox()
        new = image.getchannel('A').point(lambda n: 255 if n >= 128 else 0).getbbox()
        scale = 154 / (old[3] - old[1])
        tx, ty = 144.5 - scale * (old[0] + old[2]) / 2, 215 - scale * old[3]
        parameters.update(visible_height=scale * (new[3] - new[1]),
                          centre_x=tx + scale * (new[0] + new[2]) / 2,
                          bottom=ty + scale * new[3])
    output, report, _ = render(image, **parameters)
    repeated, _, _ = render(image, **parameters)
    assert output.tobytes() == repeated.tobytes()
    assert output.size == (288, 212) and output.mode == 'RGBA'
    assert set(np.unique(np.asarray(output)[:, :, 3])) == {0, 255}
    assert not np.asarray(output)[0].any()
    portraits, previews = HERE / 'portraits', HERE / 'previews'
    portraits.mkdir(exist_ok=True)
    previews.mkdir(exist_ok=True)
    target = portraits / ('er_portrait_lydia_date_female_beauty_fusion_v1.png' if revision == 'date_v1' else
                          f'er_portrait_lydia_female_beauty_fusion_{revision}.png')
    output.save(target)
    references = [
        ('Eleanor / current beauty', Path('D:/Program Files (x86)/Steam/steamapps/workshop/content/4348910/3829696854/Textures/Items/cp_portrait_eleanor1.png')),
        ("Author's original Lydia", ROOT / 'Art/author-pipeline-validation-2026-11-10/reference/author-sprite.png'),
        ('Lydia / current project', ROOT / 'Textures/Items/er_portrait_lydia_female_beauty.png'),
        ('Lydia / fusion ' + revision, target),
    ]
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 16)
    sheet = Image.new('RGB', (1152, 240), (37, 44, 53))
    draw = ImageDraw.Draw(sheet)
    reference_hashes = {}
    for col, (title, path) in enumerate(references):
        portrait = Image.open(path).convert('RGBA')
        assert portrait.size == (288, 212)
        reference_hashes[str(path)] = digest(path)
        bg = Image.new('RGBA', portrait.size, (37, 44, 53, 255))
        bg.alpha_composite(portrait)
        sheet.paste(bg.convert('RGB'), (288 * col, 28))
        draw.text((288 * col + 7, 5), title, font=font, fill=(240, 240, 243))
    sheet.save(previews / f'comparison_{revision}_native.png')
    sheet.resize((2304, 480), Image.Resampling.NEAREST).save(previews / f'comparison_{revision}_2x.png')
    face = sheet.crop((0, 28 + 83, 1152, 28 + 147))
    face.resize((3456, 192), Image.Resampling.NEAREST).save(previews / f'faces_{revision}_3x.png')
    mattes = Image.new('RGB', (864, 240), (37, 44, 53))
    draw = ImageDraw.Draw(mattes)
    for col, (title, rgb) in enumerate((('White', (255,255,255)), ('Black', (0,0,0)), ('Grey', (37,44,53)))):
        bg = Image.new('RGBA', output.size, (*rgb, 255))
        bg.alpha_composite(output)
        mattes.paste(bg.convert('RGB'), (288 * col, 28))
        draw.text((288 * col + 7, 5), title, font=font, fill=(240,240,243))
    mattes.save(previews / f'mattes_{revision}_native.png')
    mattes.resize((2592, 720), Image.Resampling.NEAREST).save(previews / f'mattes_{revision}_3x.png')
    if revision == 'date_v1':
        daily = Image.open(HERE / 'approved/er_portrait_lydia_female_beauty.png').convert('RGBA')
        pair = Image.new('RGB', (576, 240), (37,44,53))
        draw = ImageDraw.Draw(pair)
        for col, (title, im) in enumerate((('Approved daily / V4', daily), ('Date outfit / V1', output))):
            bg = Image.new('RGBA', im.size, (37,44,53,255))
            bg.alpha_composite(im)
            pair.paste(bg.convert('RGB'), (288 * col, 28))
            draw.text((288 * col + 7, 5), title, font=font, fill=(240,240,243))
        pair.save(previews / 'daily-date-native.png')
        pair.resize((1152,480), Image.Resampling.NEAREST).save(previews / 'daily-date-2x.png')
    if revision == 'v3':
        old_path = portraits / 'er_portrait_lydia_female_beauty_fusion_v2.png'
        before = Image.open(old_path).convert('RGBA')
        balance = Image.new('RGB', (864, 240), (37, 44, 53))
        draw = ImageDraw.Draw(balance)
        for col, (title, im) in enumerate((('Eleanor / reference', Image.open(references[0][1]).convert('RGBA')),
                                            ('Lydia / before v2', before), ('Lydia / balance v3', output))):
            bg = Image.new('RGBA', im.size, (37,44,53,255))
            bg.alpha_composite(im)
            balance.paste(bg.convert('RGB'), (288 * col, 28))
            draw.text((288 * col + 7, 5), title, font=font, fill=(240,240,243))
        balance.save(previews / 'shoulder-balance-native.png')
        balance.resize((1728, 480), Image.Resampling.NEAREST).save(previews / 'shoulder-balance-2x.png')
    report.update(master=master.name, master_sha256=digest(master),
                  prompt=f'prompt_{revision}.txt', prompt_sha256=digest(HERE / f'prompt_{revision}.txt'),
                  output=target.relative_to(HERE).as_posix(), output_sha256=digest(target),
                  repeat_identical=True, reference_sha256=reference_hashes,
                  geometry_note=('Preserve the approved daily V4 source-to-canvas mapping for the date outfit; visual eye/chin/underbust alignment reviewed without scaling the entire figure to its new garment bounding box.' if revision == 'date_v1' else
                                 'Preserve v2/v3 source-to-canvas mapping. V4 increases clothed bust volume to lower the underbust toward Eleanor; shoulder/neck positions were intentionally retained, and all anatomical readings require visual review.' if revision == 'v4' else
                                 'Face scale and approximate eye/chin alignment only. Shoulder/underbust vertical positions remain too high against Eleanor; master anatomy refinement is pending. Isotropic sampling alone does not fix this.'),
                  generation_tool='image_gen.imagegen',
                  status=('User-approved final daily beauty design V4; frozen in approved/; runtime family migration pending.' if revision == 'v4' else
                          'Design candidate; no runtime replacement, Lydia expressions or harem CG produced.'))
    (HERE / f'validation_{revision}.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'revision':revision, 'output': str(target), 'size':report['output_size'],
                      'bbox': report['output_bbox'], 'repeat_identical':True}, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', choices=['v1','v2','v3','v4','date_v1'], default='v4')
    export(parser.parse_args().revision)
