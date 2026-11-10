"""Render the v0.5 reading copies from the same JSON/CSV as the runtime."""
from pathlib import Path
import csv
import json

ROOT = Path(__file__).resolve().parent.parent


def render():
    data = json.loads((ROOT / 'Content/dual_romance_v04.json').read_text(encoding='utf8'))
    blocks = {b['id']: b for s in data['scenes'] for b in s['blocks']}
    groups = [
        ('入口：明确选择后宫；旧档兼容入口是替代开场', 'Entry: explicit harem choice; compatibility opening is an alternative', ['dr01_open', 'dr01_notice', 'v05_entry_notice', 'dr01_accept', 'dr01_decline', 'v05_legacy_entry']),
        ('G1：声望50后交付，未达标继续营业', 'G1: award at reputation 50; keep trading while waiting', ['v05_reward50']),
        ('dr03：真实原版售货后；未成交同样可以正常离开', 'dr03: after the actual native sale; no purchase also permits departure', ['dr03_open', 'dr03_plaque', 'dr03_no_plaque']),
        ('dr04：莉迪亚自己的事', 'dr04: something of Lydia’s own', ['v05_dr04_feedback', 'dr04_consult_open', 'dr04_consult']),
        ('dr05：恋人／朋友互斥；旧暂缓只再确认一次', 'dr05: lovers/friends alternatives; old deferred saves resolve once', ['dr05_open', 'dr05_confess', 'dr05_friend', 'v05_resolve_open']),
        ('dr06：三人约饭', 'dr06: a meal for three', ['dr06_open', 'dr06_meal', 'dr06_after']),
        ('dr07：闺蜜来访，不再预约任务', 'dr07: a friend’s visit, without another commission', ['v05_dr07_open', 'dr07_confirmed']),
        ('dr08：工作；甲流放／乙待复核／乙撤案的母亲事实按状态择一', 'dr08: work; route-A exile / route-B pending / route-B withdrawal alternatives', ['dr08_job_a', 'dr08_job_b', 'v05_dr08_existing', 'dr08_mother_a', 'dr08_mother_cleared', 'dr08_mother_pending', 'dr08_common', 'dr08_lover']),
        ('G2／dr09：声望100后交付，再谈她自己的生活', 'G2/dr09: award at reputation 100, then her own life', ['v05_reward100', 'dr09_open']),
        ('dr10：发夹与衣装，两种回答并列', 'dr10: the hair clip and outfit; alternative replies', ['dr10_open', 'dr10_option1', 'dr10_option2', 'dr10_common']),
        ('dr11／12：原合作买家真实结算后；复用香水与乙线回执', 'dr11/12: after actual cooperation buyer settlement; shared perfume and route-B receipt', ['dr11_open', 'dr12_open']),
        ('G3／dr13：声望150后交付，随后退让', 'G3/dr13: award at reputation 150, then she pulls away', ['v05_reward150', 'dr13_open', 'dr13_door']),
        ('dr14：坦白', 'dr14: speaking plainly', ['dr14_open', 'dr14_accept']),
        ('dr15：告诉埃莉诺；两个回答都承担关系', 'dr15: telling Eleanor; both answers take responsibility', ['dr15_open', 'dr15_wait']),
        ('dr16：莉迪亚的答复', 'dr16: Lydia’s answer', ['dr16_open']),
        ('dr17：三方确认后才写入关系', 'dr17: relationship commits after all three agree', ['dr17_open', 'dr17_accept']),
        ('dr18：分别约会；衣装／缺图回退择一', 'dr18: a date; outfit/fallback alternatives', ['dr18_open', 'dr18_outfit', 'dr18_fallback', 'dr18_embrace', 'dr18_talk', 'dr18_common']),
        ('dr19：埃莉诺的约会', 'dr19: Eleanor’s date', ['dr19_open']),
        ('dr20：共同晚饭；铭牌陈列按实际状态择一', 'dr20: dinner together; actual plaque-display variant', ['dr20_arrival', 'dr20_gathering', 'dr20_plaque', 'dr20_no_plaque', 'dr20_closing']),
        ('尾声：按实际阶段、甲乙及复核／撤案状态择一', 'Epilogue: actual stage, route and case-review/withdrawal status', ['v05_ending_complete', 'v05_ending_confirmed', 'v05_ending_discussing', 'dr_epilogue_safe', 'dr_epilogue_route_a', 'dr_epilogue_route_b_unsubmitted', 'dr_epilogue_route_b', 'dr_epilogue_route_b_acquitted', 'dr_epilogue_plaque_listed', 'dr_epilogue_plaque_held', 'dr_epilogue_plaque_none', 'dr_epilogue_departure', 'dr_epilogue_arrested', 'dr_epilogue_death']),
        ('门槛等待日常：每阶段两段，每4天轮换，不提前确认关系', 'Checkpoint dailies: two per stage, at least four days apart, no early relationship', [f'wait_{s}_{v}' for s in range(1,4) for v in range(1,3)]),
        ('已完成后宫后的七组日常', 'Seven daily visits after the full route', [f'dr_daily_{i}_open' for i in range(1,8)]),
        ('技术补领', 'Recovery claims', ['v05_claim_open']),
    ]
    for lang in ('zh', 'en'):
        out = ['后宫线 v0.5 完整阅读稿' if lang == 'zh' else 'Harem route v0.5 reading copy',
               '状态：本地实现，待实机验收。各标题下的条件分支为并列变体，不会全部连播。' if lang == 'zh' else
               'Status: locally implemented, awaiting playtesting. Conditional branches are alternatives, not one continuous reading.', '']
        for zh, en, ids in groups:
            out += ['=' * 48, zh if lang == 'zh' else en, '']
            for bid in ids:
                if bid not in blocks:
                    raise ValueError('Reading references a missing runtime block: ' + bid)
                out += ['[' + bid + ']']
                for line in blocks[bid]['lines']:
                    out.append(line['speaker'] + '：' + line['text' if lang == 'zh' else 'en'])
                out.append('')
        out += ['=' * 48, '运行选项 / Runtime choices']
        for scene in data['scenes']:
            for choice in scene.get('choices', []):
                out += [choice['label_' + lang], choice['description_' + lang], '']
        # Include the reused native fact/gift branches rather than silently omitting them.
        loc = dict(csv.reader((ROOT / f'Localization/{lang}.csv').open(encoding='utf-8-sig')))
        out += ['=' * 48, '共用原节点：售货开场、合作介绍、吊坠及核心奖励' if lang == 'zh' else
                'Shared original nodes: sales opening, cooperation, locket and core rewards']
        base = json.loads((ROOT / 'Content/story_v06.json').read_text(encoding='utf8'))
        keep = {'c18.b001', 'c18.b002', 'c20.b001', 'c20.b004', 'c20.b005', 'c20.b006'} | {f'c22.b{i:03d}' for i in range(7,11)}
        for chapter in base['chapters']:
            for b in chapter['blocks']:
                if b['id'] not in keep:
                    continue
                out.append('[' + b['id'] + ']')
                for i, line in enumerate(b['lines'],1):
                    key = 'er_v06_' + b['id'].replace('.', '_') + f'_{i:02d}'
                    out.append(str(line.get('speaker', 'Narrator')) + '：' + loc[key])
        for key in ['er_line_end_perfume', 'er_line_scav_assist', 'er_dual_plaque_desc', 'er_dual_patron_desc', 'er_dual_gratitude_desc']:
            out.append(loc[key])
        (ROOT / f'docs/后宫线_v0.5_完整阅读稿.{lang}.txt').write_text('\n'.join(out)+'\n',encoding='utf8')


if __name__ == '__main__':
    render()
