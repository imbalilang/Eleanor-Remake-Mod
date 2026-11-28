"""Compile the 24 independent Chinese chapter scripts. Also validate the whole sheet with --check-sheet."""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MARK = re.compile(r'^\*\*(.+?)\*\*(?:　(.*))?$')
HEAD = re.compile(r'^## (.+?) `([a-z_0-9]+)`$', re.M)
NATIVE = {
    'love_a2_01': 'er_story_cg_love_water_cut_text', 'love_a3_04': 'er_cg_wedding_caption',
    'dv08': 'er_cg_dv08_text', 'dv10': 'er_story_cg_date_text', 'dv13': 'er_cg_dv13_text',
    'dual_final_shop': 'er_cg_shop_caption', 'v2_a0c2': 'er_cg_signature_text',
    'v2_a4c3': 'er_story_cg_gala_text', 'v2_a4c4': 'er_ending_maid_04',
    'v2_r3c1': 'er_story_cg_revolution_embrace_text', 'v2_r4c4': 'er_cg_reunion_text',
    'rev_final_second_knock': 'er_cg_knock_caption',
}
WINDOW_KEYS = {
    'love_a2_01': ['er_love_la2_s1_common_07', 'er_love_la2_s1_common_08'],
    'dual_final_shop': ['er_cg_shop_inside', 'er_cg_shop_out'],
    'rev_final_second_knock': ['er_cg_knock_intro', 'er_cg_knock_rail', 'er_cg_knock_farewell'],
}


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


def parse(scene, title, text):
    body, marker, _ = text.partition('**图上 · ')
    assert marker, scene
    sections = {'intro': [], 'sold': [], 'unsold': []}
    section = 'intro'
    bedtime = None
    for line in body.splitlines():
        if not line.strip() or line.startswith('>'):
            continue
        mark = MARK.fullmatch(line)
        if mark and mark[1] == '睡前':
            bedtime_line = mark[2] or ''
            who, _, speech = bedtime_line.partition('　')
            assert not sections['intro'] and bedtime is None, scene
            assert who in ('旁白', '店主') and 0 < len(speech) <= 32, (scene, line)
            bedtime = bedtime_line
            continue
        if mark:
            assert '_trade_' in scene and mark[1] in ('成交', '未成交'), (scene, line)
            section = 'sold' if mark[1] == '成交' else 'unsold'
            line = mark[2] or ''
        who, space, speech = line.partition('　')
        assert space and who in ('埃莉诺', '店主', '莉迪亚', '索菲娅', '客人', '旁白'), (scene, line)
        assert 0 < len(speech) <= 32, (scene, speech)
        sections[section].append(line)
    if scene not in NATIVE:
        assert sections['intro'], scene
    if '_trade_' in scene:
        assert sections['sold'] and sections['unsold'], scene
    assert bool(bedtime) == ('_night_' in scene or scene in ('rev_ride', 'dual_final_shop')), scene
    return dict(id=scene, title=title, caption=caption(text), bedtime=bedtime, **sections)


def check():
    import openpyxl
    source = list(chapters())
    w = openpyxl.load_workbook(ROOT / 'CG规划.xlsx', read_only=True, data_only=True)
    master = {r[5]: (r[1], r[6]) for r in list(w['投放总表'].values)[7:43]}
    w.close()
    assert len(source) == len(master) == len(set(s for s, _, _ in source)) == 36
    for scene, name, text in source:
        parse(scene, name, text)
        assert (name, caption(text)) == master[scene], scene
        assert text.count(caption(text)) == 1, scene
    from hashlib import sha256
    from io import BytesIO
    from PIL import Image, ImageChops
    import csv
    editable = openpyxl.load_workbook(ROOT / 'CG规划.xlsx', data_only=True)
    sheet = editable['投放总表']
    row = next(c.row for c in sheet['F'] if c.value == 'love_a2_01')
    images = [i for i in sheet._images if i.anchor._from.row == row - 1]
    assert len(images) == 1, 'Sophia must have one reference image on her master row'
    with Image.open(BytesIO(images[0]._data())) as reference, Image.open(ROOT / 'Textures/Items/er_event_love_water_cut_cg.png') as game:
        reference, game = reference.convert('RGB'), game.convert('RGB')
        same_master = reference.size == game.size and ImageChops.difference(reference, game).getbbox() is None
        # 49647ed stored the 960x540 in-game export alongside the workbook master.
        # Recognize that specific pair, without accepting arbitrary resized or recolored art.
        recorded_export = (reference.size == (1672, 941) and game.size == (960, 540)
                           and sha256(reference.tobytes()).hexdigest() == '8a84fe4579d0e03c6749e8fb5948aa68e0fc83907ca81cd2bebbecb71d228a06'
                           and sha256(game.tobytes()).hexdigest() == '1ac9faa5adeea6c0fb9d83e1a11b0f2360148d14ee02420708d64bb4f8f3aee3')
        assert same_master or recorded_export, 'Development CG differs from the stove master and its recorded export'
    editable.close()
    with (ROOT / 'Localization/zh.csv').open(encoding='utf-8-sig', newline='') as f:
        zh = dict(csv.reader(f))
    for scene, key in NATIVE.items():
        assert zh[key].replace(r'\n', '\n') == master[scene][1], (scene, key)
    for scene, title, text in source:
        if scene in WINDOW_KEYS:
            chapter = parse(scene, title, text)
            if scene == 'dual_final_shop':
                assert chapter['bedtime'] == '店主　' + zh['er_cg_shop_sign'], scene
            intro = chapter['intro']
            assert [line.split('　', 1)[1] for line in intro] == [zh[k] for k in WINDOW_KEYS[scene]], scene
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
    print('36/36 chapter titles, IDs and captions match, including 12 localized runtime captions. Sophia stove image verified.')


def build():
    items = [parse(i, t, s) for i, t, s in chapters() if i not in NATIVE]
    assert len(items) == 24
    quote = lambda v: json.dumps(v, ensure_ascii=False)
    arr = lambda v: 'new string[] { ' + ', '.join(quote(x) for x in v) + ' }'
    body = ['// Generated from Content/CG章节 by tools/build_cg_chapters.py.', 'namespace EleanorRomance', '{', '    internal static class ChapterScripts', '    {', '        internal static readonly CgChapter[] All =', '        {']
    for v in items:
        ending = quote(v['caption']) + (', ' + quote(v['bedtime']) if v['bedtime'] else '')
        body.extend(['            new CgChapter(' + quote(v['id']) + ', ' + quote(v['title']) + ',', '                ' + ',\n                '.join(arr(v[k]) for k in ('intro', 'sold', 'unsold')) + ',', '                ' + ending + '),'])
    body.extend(['        };', '    }', '}', ''])
    (ROOT / 'src/ChapterScripts.g.cs').write_text('\n'.join(body), encoding='utf-8')

if __name__ == '__main__':
    args = argparse.ArgumentParser()
    args.add_argument('--check-sheet', action='store_true')
    if args.parse_args().check_sheet:
        check()
    build()
