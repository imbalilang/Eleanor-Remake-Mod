"""Deterministic runtime export of the AI-authored beauty gown, never redraw art."""
from pathlib import Path
from PIL import Image
import hashlib,json

root=Path(__file__).resolve().parents[1]
master=root/'Art/gala/masters/eleanor_gala_beauty_master.png'
image=Image.open(master).convert('RGBA')
alpha=image.getchannel('A').point(lambda x:255 if x>=128 else 0)
assert alpha.getbbox(), 'Empty master'
image.putalpha(alpha)
image=image.crop(alpha.getbbox())
height=158
width=round(image.width*height/image.height)
assert width<=158,(width,height)
image=image.resize((width,height),Image.Resampling.NEAREST)
alpha=image.getchannel('A')
rgb=image.convert('RGB').quantize(colors=64,method=Image.Quantize.MEDIANCUT).convert('RGB')
image=rgb.convert('RGBA');image.putalpha(alpha)
canvas=Image.new('RGBA',(288,212),(0,0,0,0))
canvas.alpha_composite(image,((288-width)//2,54))
output=root/'Textures/Items/er_portrait_eleanor_gala_female_beauty.png'
canvas.save(output)
pixels=list(canvas.get_flattened_data() if hasattr(canvas,'get_flattened_data') else canvas.getdata())
colors={p[:3] for p in pixels if p[3]}
assert len(colors)<=64 and {p[3] for p in pixels}=={0,255}
report={'source':str(master.relative_to(root)),'file':str(output.relative_to(root)),
    'size':list(canvas.size),'alpha_bbox':list(canvas.getchannel('A').getbbox()),'opaque_colors':len(colors),
    'sha256':hashlib.sha256(output.read_bytes()).hexdigest(),
    'vanilla':'Uses native eleanor1. No new vanilla sprite.'}
(root/'Art/gala/asset_validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
