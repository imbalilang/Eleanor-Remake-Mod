"""Calibrate source-image keying/resampling against the user-supplied finished sprite."""
from pathlib import Path
import argparse
import json
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont


def compare(source,reference,output):
    output.mkdir(parents=True,exist_ok=True)
    raw=Image.open(source).convert('RGBA')
    rgb=np.asarray(raw)[:,:,:3].astype(np.int16)
    # The supplied source uses a highly saturated magenta key, not alpha.
    key=(rgb[:,:,0]>160)&(rgb[:,:,2]>160)&(rgb[:,:,1]<90)&(np.minimum(rgb[:,:,0],rgb[:,:,2])-rgb[:,:,1]>95)
    alpha=Image.fromarray(np.uint8(~key)*255)
    raw.putalpha(alpha)
    source_bbox=alpha.getbbox()
    fg=raw.crop(source_bbox)
    target=Image.open(reference).convert('RGBA')
    bbox=target.getchannel('A').getbbox()
    width,height=bbox[2]-bbox[0],bbox[3]-bbox[1]
    target_alpha=np.asarray(target.getchannel('A'))>127
    interior=np.asarray(target.getchannel('A').filter(ImageFilter.MinFilter(5)))>127
    trgb=np.asarray(target)[:,:,:3].astype(float)
    reports=[];images=[]
    for name in ['NEAREST','BOX','BILINEAR','HAMMING','BICUBIC','LANCZOS']:
        small=fg.resize((width,height),getattr(Image.Resampling,name))
        mask=small.getchannel('A').point(lambda a:255 if a>=128 else 0)
        small.putalpha(mask)
        canvas=Image.new('RGBA',target.size,(0,0,0,0))
        canvas.paste(small,(bbox[0],bbox[1]))
        pixels=np.asarray(canvas);ca=pixels[:,:,3]>127;common=interior&ca
        diff=np.abs(pixels[:,:,:3].astype(float)-trgb)
        report={'method':name,'silhouette_iou':float((ca&target_alpha).sum()/(ca|target_alpha).sum()),
                'interior_RGB_MAE':float(diff[common].mean()),'visible_RGB_MAE':float(diff[ca&target_alpha].mean()),
                'exact_RGB_fraction_interior':float((diff[common].max(axis=1)==0).mean())}
        reports.append(report);images.append((name,canvas))
        canvas.save(output/(name.lower()+'.png'))
    result={'source_bbox':list(source_bbox),'target_bbox':list(bbox),'calibration':'Fit foreground to target silhouette bounding box; no automatic colour adjustment, palette reduction, sharpening or extra blur.',
            'methods':sorted(reports,key=lambda r:r['interior_RGB_MAE']),
            'warning':'Lowest error is a reproduction candidate, not proof of the original author\'s algorithm.'}
    (output/'resampling-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    panels=[('AUTHOR TARGET',target)]+images
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
    sheet=Image.new('RGB',(288*2*4,212*2*2+40*2),(35,36,42));draw=ImageDraw.Draw(sheet)
    for i,(label,image) in enumerate(panels):
        x=(i%4)*576;y=(i//4)*464
        draw.text((x+12,y+8),label,font=font,fill=(240,240,240))
        zoom=image.resize((576,424),Image.Resampling.NEAREST)
        sheet.paste(zoom,(x,y+40),zoom)
    sheet.save(output/'comparison-2x.png')
    print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source',type=Path,required=True)
    p.add_argument('--reference',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();compare(a.source,a.reference,a.output)
