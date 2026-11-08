"""Read-only extraction of story facts from ES3's nonstandard JSON.

No inventory, player names, trading history, or full save is copied. Only a
strict whitelist of this mod's state and native Eleanor rescue facts is kept.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

p=argparse.ArgumentParser()
p.add_argument('save',type=Path)
p.add_argument('output',type=Path)
a=p.parse_args()
raw=a.save.read_bytes()
text=raw.decode('utf-8-sig')
state={k:v for k,v in re.findall(r'"eleanor_romance:(er\.[a-z0-9_.]+)"\s*:\s*"([^"\\]*)"',text)}
native={}
for key in ('eleanorArrested','isEleanorDead','eleanorBailChoice','eleanorArrestDay'):
    match=re.search(r'"'+key+r'"\s*:\s*(true|false|-?\d+)',text)
    if match:native[key]=json.loads(match[1])
day=re.search(r'"dayCounter"\s*:\s*(\d+)',text)
game=re.search(r'"saveVersion"\s*:\s*"([^"]*)"',text)
fixture={'source_sha256':hashlib.sha256(raw).hexdigest(),'source_kind':'real-local-save-read-only',
    'published_mod_version':'not recorded by the historical save; matching callback shape is independently tested',
    'day':int(day[1]) if day else None,'game_save_version':game[1] if game else None,
    'state':state,'native':native,
    'serialized_mod_client_ids':sorted(set(re.findall(r'"identifier"\s*:\s*"(er_story_[^"]*)"',text)))}
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_text(json.dumps(fixture,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'Extracted {len(state)} story facts; day={fixture["day"]}; serialized mod clients={len(fixture["serialized_mod_client_ids"])}. Original save unchanged.')
