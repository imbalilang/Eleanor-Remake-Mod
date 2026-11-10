"""Export current Lydia portraits, respecting approved beauty replacements."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
from PIL import Image, ImageDraw, ImageFont
from portrait_resampling import render_portrait

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT/'Art/dual-romance-v0.3'
PROFILES = {
    'daily_vanilla': ('er_portrait_lydia_female_vanilla.png',(81,42,206,212)),
    'daily_beauty': ('er_portrait_lydia_female_beauty.png',(83,60,205,212)),
    'date_vanilla': ('er_portrait_lydia_date_female_vanilla.png',(83,42,204,212)),
    'date_beauty': ('er_portrait_lydia_date_female_beauty.png',(85,60,204,212)),
}
# Facial/body landmarks set the framing. Hair/shoulder extents do not determine
# face scale; the full resized figure may extend below the native canvas.
FRAMING_DATA = json.loads((ART/'framing_profiles.json').read_text(encoding='utf8'))
FRAMING = FRAMING_DATA['profiles']


def label(path):
    path=path.resolve()
    return str(path.relative_to(ROOT)).replace('\\','/') if path.is_relative_to(ROOT) else str(path)


def describe(path):
    with Image.open(path) as original:
        image=original.convert('RGBA')
        pixels=list(image.get_flattened_data())
        return {'file':label(path),'width':image.width,'height':image.height,'mode':original.mode,
                'alphaValues':sorted({p[3] for p in pixels}),
                'alpha_bbox':list(image.getchannel('A').getbbox()),
                'opaqueColors':len({p[:3] for p in pixels if p[3]}),
                'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}


def export_one(variant,art_only=False,output_dir=None):
    # Approved fusion artwork supersedes the old beauty masters/framing. Keep
    # the old exporter from reintroducing the retired face on a later export.
    fusion=ROOT/'Art/lydia-style-fusion-2026-11-10'
    if variant in ('daily_beauty','date_beauty') and (fusion/'approval.json').exists():
        filename=PROFILES[variant][0]
        is_daily=variant=='daily_beauty'
        native=fusion/('approved/er_portrait_lydia_female_beauty.png' if is_daily else
                      'portraits/er_portrait_lydia_date_female_beauty_fusion_v1.png')
        validation=json.loads((fusion/('validation_v4.json' if is_daily else 'validation_date_v1.json')).read_text(encoding='utf8'))
        if hashlib.sha256(native.read_bytes()).hexdigest()!=validation['output_sha256']:
            raise ValueError('Approved fusion portrait differs from its validation')
        destinations=([Path(output_dir).resolve()/filename] if output_dir else
                      [ART/'portraits'/filename] if art_only else
                      [ROOT/'Textures/Items'/filename,ART/'portraits'/filename])
        for destination in destinations:
            destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(native,destination)
        report=describe(destinations[0])
        report.update(variant=variant,source=label(fusion/validation['master']),
                      source_sha256=validation['master_sha256'],source_size=validation['source_size'],
                      source_alpha_bbox=validation['clean_bbox'],
                      framing_basis='User-approved Lydia fusion identity and fixed B/detail source-to-canvas transform',
                      pipeline=validation['parameters'],transform=validation['transform'],
                      reference='Art/lydia-style-fusion-2026-11-10/approval.json',
                      outputs=[label(p) for p in destinations])
        return report
    filename,old_bbox=PROFILES[variant]
    source=ART/'masters'/('lydia_'+variant+'.png')
    with Image.open(source) as original:
        image=original.convert('RGBA')
    source_bbox=image.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
    assert source_bbox
    sw,sh=source_bbox[2]-source_bbox[0],source_bbox[3]-source_bbox[1]
    frame=FRAMING[variant]
    size=(round(sw*frame['height']/sh),frame['height'])
    position=((288-size[0])//2,frame['top'])
    canvas,bbox=render_portrait(image,size,position)
    actual=canvas.getchannel('A').getbbox()
    assert actual and all(abs(a-b)<=1 for a,b in zip(actual,old_bbox)), 'Unexpected framing shift: '+variant
    pixels=list(canvas.get_flattened_data())
    assert {p[3] for p in pixels}=={0,255}
    assert all(p[:3]==(0,0,0) for p in pixels if p[3]==0)
    projected={name:[round(position[0]+(point[0]-bbox[0])*size[0]/sw,2),
                     round(position[1]+(point[1]-bbox[1])*size[1]/sh,2)]
               for name,point in frame['landmarks'].items()}
    reference=FRAMING_DATA['references'][frame['reference']]
    cheek_span=projected['right_cheek'][0]-projected['left_cheek'][0]
    differences={name:round(projected[name][1]-reference[key],2) for name,key in
                 [('eye_midpoint','eye_y'),('chin','chin_y'),('bust','bust_y'),('underbust','underbust_y')]}
    assert abs(cheek_span-reference['cheek_span'])<=3, 'Face scale differs from Eleanor: '+variant
    assert abs(differences['eye_midpoint'])<=3 and abs(differences['chin'])<=3, 'Face height alignment differs: '+variant
    assert abs(differences['bust'])<=6 and abs(differences['underbust'])<=5, 'Torso alignment differs: '+variant
    assert projected['waist'][1]>=reference['waist_lower_bound_y'], 'Waist should fall at/below crop: '+variant
    if output_dir:
        destinations=[Path(output_dir).resolve()/filename]
    elif art_only:
        destinations=[ART/'portraits'/filename]
    else:
        destinations=[ROOT/'Textures/Items'/filename,ART/'portraits'/filename]
    for p in destinations:
        p.parent.mkdir(parents=True,exist_ok=True)
    canvas.save(destinations[0])
    for p in destinations[1:]:
        shutil.copy2(destinations[0],p)
    report=describe(destinations[0])
    report.update(variant=variant,source=label(source),source_size=list(image.size),source_alpha_bbox=list(bbox),
                  source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                  scaled_figure_size=list(size),position=list(position),
                  bottom_crop_pixels=max(0,position[1]+size[1]-212),
                  framing_basis='Approximate face scale, eye/chin and bust/waist position against the matching Eleanor style; isotropic scaling only',
                  reference=reference,projected_landmarks_approx=projected,
                  cheek_span_approx=round(cheek_span,2),vertical_difference_from_reference_approx=differences,
                  outputs=[label(p) for p in destinations])
    return report


def write_art_inventory():
    inventory=[]
    for filename,_ in PROFILES.values():
        record=describe(ART/'portraits'/filename)
        record['file']='portraits/'+filename
        record['format']='Format32bppArgb'
        record['sha256']=record['sha256'].upper()
        inventory.append(record)
    plaque=describe(ART/'items/er_lydia_recommendation_plaque.png')
    plaque['file']='items/er_lydia_recommendation_plaque.png'
    plaque['format']='Format32bppArgb'
    plaque['sha256']=plaque['sha256'].upper()
    inventory.append(plaque)
    (ART/'asset_validation.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    sheet=Image.new('RGB',(1152,968),(43,38,49));draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
    titles=['Original - Daily','Beauty - Daily','Original - Date','Beauty - Date']
    for i,((filename,_),title) in enumerate(zip(PROFILES.values(),titles)):
        x=(i%2)*576;y=(i//2)*464
        draw.text((x+24,y+12),title,font=font,fill=(255,255,255))
        with Image.open(ART/'portraits'/filename) as image:
            image=image.convert('RGBA').resize((576,424),Image.Resampling.NEAREST)
            sheet.paste(image,(x,y+36),image)
    sheet.save(ART/'portrait_contact_sheet.png')


def export(variant='all',art_only=False,output_dir=None):
    variants=PROFILES if variant=='all' else [variant]
    reports=[export_one(v,art_only,output_dir) for v in variants]
    data={'date':'2026-11-10','scope':'candidate' if output_dir else 'art-only' if art_only else 'workspace production and art mirrors',
          'framing_configuration':label(ART/'framing_profiles.json'),
          'framing_configuration_sha256':hashlib.sha256((ART/'framing_profiles.json').read_bytes()).hexdigest(),
          'pipeline':{'resampling':'Verified author B/detail for approved beauty variants; legacy BOX only for unchanged vanilla variants',
                      'palette':'Full RGB; no colour quantization','dither':'NONE',
                      'alpha':'Beauty: source >=128, target coverage >=0.35, transparent RGB bleed; legacy vanilla: threshold 128, transparent RGB cleared',
                      'game_filter':'Point (unchanged)'},
          'assets':reports}
    out=Path(output_dir).resolve() if output_dir else ART
    out.mkdir(parents=True,exist_ok=True)
    (out/'portrait_export_validation.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    if not output_dir:
        write_art_inventory()
    print(json.dumps({'scope':data['scope'],'assets':[{k:r[k] for k in ['variant','width','height','alpha_bbox','opaqueColors','sha256']} for r in reports]},ensure_ascii=False))
    return data


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--variant',choices=['all',*PROFILES],default='all')
    dest=p.add_mutually_exclusive_group()
    dest.add_argument('--art-only',action='store_true',help='Update art exports only, preserving production Textures')
    dest.add_argument('--output-dir',type=Path,help='Write only review candidates and their manifest')
    a=p.parse_args();export(a.variant,a.art_only,a.output_dir)
