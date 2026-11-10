"""Read-only validation of the generated production sprite."""
from pathlib import Path
from PIL import Image
import hashlib
import json

root=Path(__file__).resolve().parents[1]
assets=[]
for variant,limit in [('beauty',None),('vanilla',48)]:
    p=root/f'Textures/Items/er_portrait_eleanor_maid_female_{variant}.png'
    with Image.open(p) as im:
        assert im.mode=='RGBA' and im.size==(288,212), (im.mode,im.size)
        pixels=list(im.get_flattened_data() if hasattr(im,'get_flattened_data') else im.getdata())
        alpha=sorted({v[3] for v in pixels})
        colors={v[:3] for v in pixels if v[3]}
        bbox=im.getchannel('A').getbbox()
        assert alpha==[0,255]
        if variant=='beauty':
            manifest=json.loads((root/'Art/v0.5/beauty_export_validation.json').read_text(encoding='utf-8'))
            assert list(bbox)==manifest['alpha_bbox']==[66,52,220,212]
            assert len(colors)==manifest['opaque_colors'] and len(colors)>256
            assert hashlib.sha256(p.read_bytes()).hexdigest()==manifest['sha256']
            master=root/manifest['source']
            assert hashlib.sha256(master.read_bytes()).hexdigest()==manifest['source_sha256']
            assert manifest['framing']['reference']['workshop']==3829696854
        else:
            assert len(colors)<=limit and bbox==(89,54,199,212)
            assert hashlib.sha256(p.read_bytes()).hexdigest()=='4a1c8e92438543712f3ad07c0ccc8c037cc1b9fb77f6a7ced1f6c5296aaa80ed'
        assets.append({'file':str(p.relative_to(root)),'variant':variant,'size':list(im.size),'mode':im.mode,
            'alpha_values':alpha,'opaque_colors':len(colors),'alpha_bbox':list(bbox),
            'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),
            'visual_review':'Native-size sprite inspected against the corresponding game/mod reference; in-game scale pending user verification.'})
assert assets[0]['sha256']!=assets[1]['sha256'], 'The variants must be independently authored.'
result={'assets':assets,'distinct_variants':True}
(root/'Art/v0.5/asset_validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
