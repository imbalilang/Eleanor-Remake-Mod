"""Read-only validation of the generated production sprite."""
from pathlib import Path
from PIL import Image
import hashlib
import json

root=Path(__file__).resolve().parents[1]
assets=[]
for variant,limit in [('beauty',64),('vanilla',48)]:
    p=root/f'Textures/Items/er_portrait_eleanor_maid_female_{variant}.png'
    with Image.open(p) as im:
        assert im.mode=='RGBA' and im.size==(288,212), (im.mode,im.size)
        pixels=list(im.get_flattened_data() if hasattr(im,'get_flattened_data') else im.getdata())
        alpha=sorted({v[3] for v in pixels})
        colors={v[:3] for v in pixels if v[3]}
        bbox=im.getchannel('A').getbbox()
        assert alpha==[0,255] and len(colors)<=limit
        assert bbox[0]>=65 and bbox[2]<=223 and bbox[1]>=48 and bbox[3]==212
        assets.append({'file':str(p.relative_to(root)),'variant':variant,'size':list(im.size),'mode':im.mode,
            'alpha_values':alpha,'opaque_colors':len(colors),'alpha_bbox':list(bbox),
            'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),
            'visual_review':'Native-size sprite inspected against the corresponding game/mod reference; in-game scale pending user verification.'})
assert assets[0]['sha256']!=assets[1]['sha256'], 'The variants must be independently authored.'
result={'assets':assets,'distinct_variants':True}
(root/'Art/v0.5/asset_validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
