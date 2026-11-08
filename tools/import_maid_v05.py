#!/usr/bin/env python3
"""Import the frozen v0.7 reading draft, retaining source line numbers for review.

Only spoken lines and explicitly selected narration become runtime content;
design instructions never become dialogue. Regeneration also writes the
old-block -> new-block map used to carry English translations and code refs
across the v0.7 renumbering.
"""
from pathlib import Path
import difflib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'docs/全流程攻略_纯阅读版_大小姐侍从恋爱线_设计稿_v0.7.txt'
SPEAKERS = {'埃莉诺':'Eleanor','玩家':'Player','母亲':'Mother','莉迪亚':'Lydia',
            '黑市打手':'Thug','上层区采购商':'Buyer','新闻':'News'}
SECTIONS = [('旧约承接序章','legacy'), *[(f'第{i}章 ',f'm{i}') for i in range(11,29)],
            ('王尔德的筹备清单','projects'), ('乙线独立事件  ','world'),
            ('筹备期间  ','wait'), ('第60天以后  ','daily'), ('路线丙  ','tragic'),
            ('约定终止、','exit'), ('原版游戏结束时的','ending'), ('关键行为与分支速查','end')]

def parse_blocks():
    lines = SOURCE.read_text(encoding='utf-8-sig').splitlines()
    blocks=[]; section=None; block=None; count=0; i=0
    while i<len(lines):
        line=lines[i].strip()
        found=next((sid for prefix,sid in SECTIONS if line.startswith(prefix)),None)
        if found:
            section=found; count=0; block=None; i+=1; continue
        if section is None or section=='end': i+=1; continue
        # Named sub-branches delimit independent content blocks too.
        label = re.fullmatch(r'【(.+)】',line)
        sub = line in ('玩家代为保管：','她自行保管：','正常成交：','未成交：','成交后：','接受：','婉拒实物、请她继续佩戴：')
        if label or sub:
            count+=1
            block={'id':f'{section}_b{count:03}', 'label':label[1] if label else line[:-1], 'source_line':i+1,'lines':[]}
            blocks.append(block)
            i+=1; continue
        speaker=next((v for k,v in SPEAKERS.items() if line==k+'：'),None)
        if speaker:
            start=i+1; text=[]; i+=1
            while i<len(lines) and lines[i].strip():
                text.append(lines[i].strip()); i+=1
            if block is None: raise ValueError((section,start))
            if text: block['lines'].append({'speaker':speaker,'text':'\n'.join(text),'source_line':start+1})
            continue
        # Explicit narration is reviewed below; all other prose is instruction.
        if block and block['label'] in ('叙述','结尾叙述') and line:
            text=[]; start=i
            while i<len(lines) and lines[i].strip(): text.append(lines[i].strip()); i+=1
            narration='\n'.join(text)
            if not narration.startswith(('通过现有','她借柜台')):
                narration=narration.replace('此段只是工作叙述，不生成或移动游戏物品。','').replace('此段为叙述，不生成或移动游戏物品。','')
                block['lines'].append({'speaker':'Narrator','text':narration,'source_line':start+1})
            continue
        if section=='ending' and block and line and not line.startswith(('使用原完整','所有关系版本','按原路线','只取已播放')):
            # Curated below to exclude conditional instructions.
            if not line.startswith('='):
                block['lines'].append({'speaker':'Narrator','text':line,'source_line':i+1})
        i+=1
    blocks=[b for b in blocks if b['lines']]
    for block in blocks:
        if block['id']=='tragic_b004':
            block['lines'][0]['text']=block['lines'][0]['text'].replace('按原剧情得到确认','已经得到确认')
        if block['id']=='exit_b006':
            block['lines'][0]['text']=block['lines'][0]['text'].replace('之后的世界结果由实际交易对象与既有路线决定。','')
        if block['id']=='ending_b002':
            for item in block['lines']: item['text']=item['text'].split('：',1)[1]
        if block['id']=='ending_b004':
            block['lines'][1]['text']=block['lines'][1]['text'].strip('（）')
        if block['id']=='ending_b005': block['lines']=block['lines'][:1]
    return blocks

def signature(block):
    return [(line['speaker'],line['text']) for line in block['lines']]

def source_hash():
    import hashlib
    return hashlib.sha256(SOURCE.read_bytes()).hexdigest()

def build_map(previous, blocks):
    """Match old blocks to new ones by exact text, then by similarity.

    The positional block ids change when v0.7 inserts labels.  The map keeps
    unchanged English and code references reviewable; changed/new blocks are
    listed separately so they must be checked by hand.
    """
    result={}; changed=[]; deleted=[]; added=[]
    by_section_old={}
    for block in previous: by_section_old.setdefault(block['id'].split('_')[0],[]).append(block)
    used=set()
    for block in blocks:
        section=block['id'].split('_')[0]
        candidates=[old for old in by_section_old.get(section,[]) if old['id'] not in used]
        exact=next((old for old in candidates if signature(old)==signature(block)),None)
        best=None; ratio=0.0
        if exact is not None:
            best=exact; ratio=1.0
        else:
            for old in candidates:
                score=difflib.SequenceMatcher(None,signature(old),signature(block)).ratio()
                if score>ratio: best, ratio = old, score
        if best is not None and ratio>=0.5:
            used.add(best['id']); result[best['id']]=block['id']
            if ratio<1.0:
                changed.append({'old':best['id'],'new':block['id'],'similarity':round(ratio,3),
                                'old_lines':len(best['lines']),'new_lines':len(block['lines'])})
        else:
            added.append(block['id'])
    for old in previous:
        if old['id'] not in used:
            deleted.append(old['id'])
    return {'source':str(SOURCE.relative_to(ROOT)),'source_sha256':source_hash(),
            'generated_by':'tools/import_maid_v05.py',
            'map':dict(sorted(result.items())),'changed':changed,'added':added,'deleted':deleted}

def main():
    blocks=parse_blocks()
    old_path=ROOT/'Content/maid_v05.json'
    map_path=ROOT/'Content/maid_id_map_v07.json'
    previous=[]; previous_source=None
    if old_path.exists():
        old_data=json.loads(old_path.read_text(encoding='utf-8'))
        previous=old_data.get('blocks',[]); previous_source=old_data.get('source')
    mapping=None
    source_name=str(SOURCE.relative_to(ROOT))
    if map_path.exists() and previous_source==source_name:
        # Re-running the importer on the same frozen source must not erase the
        # original v0.6 -> v0.7 mapping used for English and code review.
        existing=json.loads(map_path.read_text(encoding='utf-8'))
        if existing.get('source')==source_name and existing.get('source_sha256')==source_hash():
            mapping=existing
    if mapping is None: mapping=build_map(previous,blocks)
    out={'version':7,'source':str(SOURCE.relative_to(ROOT)),'blocks':blocks}
    old_path.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (ROOT/'Content/maid_id_map_v07.json').write_text(
        json.dumps(mapping,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (ROOT/'docs/MAID_CONTENT_MAP.md').write_text(
        '# v0.7 文案索引\n\n由 `tools/import_maid_v05.py` 生成；源行号对应冻结阅读稿。\n\n'
        '| ID | 段落 | 源行 | 台词数 |\n|---|---|---:|---:|\n'
        +'\n'.join(f"| {b['id']} | {b['label']} | {b['source_line']} | {len(b['lines'])} |" for b in blocks)+'\n',
        encoding='utf-8')
    print(f"Imported {len(blocks)} blocks / {sum(len(b['lines']) for b in blocks)} lines; "
          f"mapped {len(mapping['map'])}, changed {len(mapping['changed'])}, "
          f"added {len(mapping['added'])}, deleted {len(mapping['deleted'])}")

if __name__=='__main__': main()
