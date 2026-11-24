"""Compile the 24 independent Chinese chapter scripts. Also validate the whole sheet with --check-sheet."""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MARK = re.compile(r'^\*\*(.+?)\*\*(?:　(.*))?$')
HEAD = re.compile(r'^## (.+?) `([a-z_0-9]+)`$', re.M)
NATIVE = {'love_a2_01', 'love_a3_04', 'dv08', 'dv10', 'dv13', 'dual_final_shop', 'v2_a0c2', 'v2_a4c3', 'v2_a4c4', 'v2_r3c1', 'v2_r4c4', 'rev_final_second_knock'}


def chapters():
    for path in sorted((ROOT / 'Content/CG章节').glob('E*.md')):
        source = path.read_text(encoding='utf-8-sig')
        matches = list(HEAD.finditer(source))
        if len(matches) != 9:
            raise ValueError(f'{path.name}: expected nine chapters')
        for index, head in enumerate(matches):
            end = matches[index + 1].start() if index + 1 < len(matches) else len(source)
            yield head[2], head[1], source[head.end():end].strip()


def caption(text):
    match = re.search(r'^\*\*[^\n*]+ · [^\n*]+\*\*[　\n]+(.+?)(?:\n\n|\Z)', text, re.M | re.S)
    if not match:
        raise ValueError('missing fixed caption: ' + text[:60])
    return match[1].strip()


def clean(text):
    text = re.sub(r'〔(?:当晚至此，次晨续|回营业|结束|当晚结束，次晨正常开门)〕', '', text)
    text = text.replace('。结束〕', '。〕').replace('〔', '').replace('〕', '')
    return text.strip().strip('（）') if text.startswith('（') and text.endswith('）') else text.strip()


def parse(scene, title, text):
    sections = {k: [] for k in ('intro', 'yes', 'no', 'picture')}
    labels = []
    current = 'intro'
    last_caption = caption(text)
    text = text.replace(last_caption, '', 1)
    for line in text.splitlines():
        if not line.strip() or line.startswith('>'):
            continue
        mark = MARK.fullmatch(line.rstrip('　'))
        if mark:
            label, line = mark[1], mark[2] or ''
            if ' · ' in label:
                continue
            if '亮图' in label:
                current = 'picture'
            elif label in ('成交', '未成交'):
                current = 'yes' if label == '成交' else 'no'
            else:
                labels.append(label)
                current = 'yes' if len(labels) == 1 else 'no'
        if clean(line):
            sections[current].append(clean(line))
    if '_trade_' in scene:
        assert sections['no'] and sections['yes'] and not labels, scene
    elif scene != 'maid_stage_soundcheck':
        assert len(labels) == 2 and sections['yes'] and sections['no'], scene
    return dict(id=scene, title=title, labels=labels, caption=last_caption, **sections)


def check():
    import openpyxl
    source = list(chapters())
    w = openpyxl.load_workbook(ROOT / 'CG规划.xlsx', read_only=True, data_only=True)
    master = {r[5]: (r[1], r[6]) for r in list(w['投放总表'].values)[7:43]}
    assert len(source) == len(master) == len(set(s for s, _, _ in source)) == 36
    for scene, name, text in source:
        assert (name, caption(text)) == master[scene], scene
        assert text.count(caption(text)) == 1, scene
    from io import BytesIO
    from PIL import Image, ImageChops
    import csv
    editable = openpyxl.load_workbook(ROOT / 'CG规划.xlsx', data_only=True)
    sheet = editable['投放总表']
    row = next(c.row for c in sheet['F'] if c.value == 'love_a2_01')
    images = [i for i in sheet._images if i.anchor._from.row == row - 1]
    assert len(images) == 1, 'Sophia must have one reference image on her master row'
    with Image.open(BytesIO(images[0]._data())) as reference, Image.open(ROOT / 'Textures/Items/er_event_love_water_cut_cg.png') as game:
        assert reference.size == game.size and ImageChops.difference(reference.convert('RGB'), game.convert('RGB')).getbbox() is None, 'Development CG differs from the approved stove reference'
    editable.close()
    with (ROOT / 'Localization/zh.csv').open(encoding='utf-8-sig', newline='') as f:
        zh = dict(csv.reader(f))
    assert zh['er_story_cg_love_water_cut_text'] == master['love_a2_01'][1]
    assert zh['er_cg_knock_caption'].replace(r'\n', '\n') == master['rev_final_second_knock'][1] == '埃莉诺！\n我在。'
    sophia = next(text for scene, _, text in source if scene == 'love_a2_01')
    assert '勺' in sophia
    forbidden = {
        'love_a2_01': ('上楼', '台阶', '楼梯', '半杯水', '葱香'),
        'love_night_stay': ('戒指',),
        'dual_night_bath': ('上楼', '上次', '上回在石阶'),
        'maid_trade_shopkeeper': ('搭给', '赠送', '附赠'),
        'dv10': ('石阶晒得发暖',),
        'dual_night_fireworks': ('天空里只剩', '震得窗玻璃直颤'),
        'rev_night_jacket': ('坐在星光底下',),
        'rev_final_second_knock': ('先晒到', '天刚亮', '风掠起她的长发'),
    }
    for scene, _, text in source:
        assert not any(word in text for word in forbidden.get(scene, ())), scene
    native_wrap = ' '.join(value for key, value in zh.items() if key.startswith('er_love_la2_s1_common_'))
    assert '我送您回家' in native_wrap and not any(word in native_wrap for word in forbidden['love_a2_01'])
    for key in ('er_cg_knock_intro', 'er_cg_knock_rail'):
        assert not any(word in zh[key] for word in forbidden['rev_final_second_knock']), key
    print('36/36 chapter titles, IDs and captions match. Sophia RGB pixels and both final localized captions match.')


def build():
    items = [parse(i, t, s) for i, t, s in chapters() if i not in NATIVE]
    assert len(items) == 24
    quote = lambda v: json.dumps(v, ensure_ascii=False)
    arr = lambda v: 'new string[] { ' + ', '.join(quote(x) for x in v) + ' }'
    body = ['// Generated from Content/CG章节 by tools/build_cg_chapters.py.', 'namespace EleanorRomance', '{', '    internal static class ChapterScripts', '    {', '        internal static readonly CgChapter[] All =', '        {']
    for v in items:
        body.extend(['            new CgChapter(' + quote(v['id']) + ', ' + quote(v['title']) + ',', '                ' + ',\n                '.join(arr(v[k]) for k in ('intro', 'labels', 'yes', 'no', 'picture')) + ',', '                ' + quote(v['caption']) + '),'])
    body.extend(['        };', '    }', '}', ''])
    (ROOT / 'src/ChapterScripts.g.cs').write_text('\n'.join(body), encoding='utf-8')

if __name__ == '__main__':
    args = argparse.ArgumentParser()
    args.add_argument('--check-sheet', action='store_true')
    if args.parse_args().check_sheet:
        check()
    build()
