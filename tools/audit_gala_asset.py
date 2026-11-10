"""Read-only, reproducible comparison of the repaired gown and maid assets.

Writes review evidence and a native-pixel 3x preview under dist, never source art.
"""
from pathlib import Path
import argparse
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
GOWN=ROOT/'Textures/Items/er_portrait_eleanor_gala_female_beauty.png'
MAID=ROOT/'Textures/Items/er_portrait_eleanor_maid_female_beauty.png'
MASTER=ROOT/'Art/gala/masters/eleanor_gala_beauty_master.png'


def describe(path):
    path=path.resolve()
    with Image.open(path) as original:
        image=original.convert('RGBA')
        pixels=list(image.get_flattened_data())
        bounds=image.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
        return dict(file=str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),size=list(image.size),mode=original.mode,
                    bytes=path.stat().st_size,alpha_bbox=list(bounds),
                    figure_size=[bounds[2]-bounds[0],bounds[3]-bounds[1]],
                    alpha_values=sorted({p[3] for p in pixels}),
                    visible_colors=len({p[:3] for p in pixels if p[3]}),
                    sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def review(before,output):
    output.mkdir(parents=True,exist_ok=True)
    gown,maid=describe(GOWN),describe(MAID)
    assert gown['size']==maid['size']==[288,212]
    assert gown['mode']==maid['mode']=='RGBA'
    assert gown['alpha_values']==maid['alpha_values']==[0,255]
    assert gown['alpha_bbox']==[59,58,224,212]
    assert gown['visible_colors']>256
    validation=json.loads((ROOT/'Art/gala/asset_validation.json').read_text(encoding='utf-8'))
    assert gown['sha256']==validation['sha256']
    assert hashlib.sha256(MASTER.read_bytes()).hexdigest()==validation['source_sha256']
    assert validation['export']['resampling'].startswith('BOX area averaging in premultiplied alpha')
    assert validation['export']['palette']=='Full RGB; no colour quantization'
    assert validation['export']['dither']=='NONE'
    framing=validation['framing']
    assert framing['scaled_figure_size']==[171,209] and framing['position']==[58,58]
    assert framing['bottom_crop_pixels']==55
    assert framing['reference']['workshop']==3829696854
    report=dict(gown=gown,maid=maid,master=describe(MASTER),
                framing=framing,
                loader={'filter':'Point','pixels_per_unit':100,'mipmaps':False},
                visual_review='3x nearest-neighbour comparison; no in-game playthrough or deployment')
    panels=[]
    if before is not None:
        original=describe(before)
        assert original['size']==gown['size']
        report['before']=original
        panels.append(('调整前',before))
    panels.extend([('调整后',GOWN),('女仆装参照',MAID)])
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
    sheet=Image.new('RGB',(528*len(panels),576),(28,25,35))
    draw=ImageDraw.Draw(sheet)
    for index,(label,path) in enumerate(panels):
        draw.text((index*528+20,12),label,font=font,fill=(245,230,208))
        with Image.open(path) as im:
            # Identical crop, background and integer Point scaling for all assets.
            sprite=im.convert('RGBA').crop((56,36,232,212)).resize((528,528),Image.Resampling.NEAREST)
            sheet.paste(sprite,(index*528,44),sprite)
    sheet.save(output/'comparison-3x.png')
    # Compact integer zoom avoids the chat renderer shrinking a wide 3x sheet.
    compact=Image.new('RGB',(352*len(panels),396),(28,25,35))
    draw=ImageDraw.Draw(compact)
    for index,(label,path) in enumerate(panels):
        draw.text((index*352+14,8),label,font=font,fill=(245,230,208))
        with Image.open(path) as im:
            sprite=im.convert('RGBA').crop((56,36,232,212)).resize((352,352),Image.Resampling.NEAREST)
            compact.paste(sprite,(index*352,40),sprite)
    compact.save(output/'comparison-2x.png')
    (output/'asset-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False))
    print(output/'comparison-3x.png')


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before',type=Path)
    parser.add_argument('--output',type=Path,default=ROOT/'dist/gala-art-audit')
    args=parser.parse_args()
    review(args.before,args.output)
