"""Render the revised, bilingual runtime blocks without overwriting frozen drafts."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
data=json.loads((ROOT/'Content/maid_v05.json').read_text(encoding='utf-8'))
blocks={b['id']:b for b in data['blocks']}
en={}
for p in sorted((ROOT/'Content').glob('maid_v05_en_part*.json')):
    en.update(json.loads(p.read_text(encoding='utf-8')))
extra=json.loads((ROOT/'Content/relationship_choices_v1.json').read_text(encoding='utf-8'))
for b in extra['blocks']:
    blocks[b['id']]=b;en[b['id']]=[line['en'] for line in b['lines']]
sections=[
 ('旧档序章 / Legacy continuation',['legacy_b002','legacy_b004','legacy_b005','legacy_b006','legacy_b008','m11_b007','legacy_b011','legacy_b012','legacy_b013']),
 ('11 入线与试做 / Beginning cooperation',['m11_b001','m11_b002','m11_b003','m11_b004','m11_b006','m11_b007','m11_b008','m11_b011','m11_b012']),
 ('12 资料与旧伤 / Findings and the old injury',['m12_b001','m12_b004','choicefix_b001','m12_b002','m12_b003','m12_b006','m12_b007','m12_b008','m12_b009']),
 ('13 黑市传话 / The messenger',[b for b in blocks if b.startswith('m13_')]),
 ('14 核验与争执 / Checking the offer',[b for b in blocks if b.startswith('m14_')]+['choicefix_b003']),
 ('15 执行与求助 / Following through',[b for b in blocks if b.startswith('m15_')]),
 ('16 救母结果 / The rescue result',[b for b in blocks if b.startswith('m16_')]),
 ('17 母亲观察 / Her mother visits',[b for b in blocks if b.startswith('m17_')]),
 ('18 工作与依赖 / Work and growing reliance',[b for b in blocks if b.startswith('m18_')]),
 ('19 新衣与称呼 / The outfit and the address',['m19_b001','m19_b002','choicefix_b002','m19_b005','m19_b006','m19_b008','m19_b007']),
 ('20 侍从恋爱内的回应 / Attendant confession responses',['m20_b001','m20_b005','m20_b006','m20_b008','choicefix_b004','choicefix_b005','m20_b009']),
 ('丙丁线相关承接 / Related tragic and exit scenes',['tragic_b012','exit_b005','exit_b006','exit_b008']),
 ('28 独立后的再选择 / Choosing again',[b for b in blocks if b.startswith('m28_')]),
]
for lang in ('zh','en'):
    intro=(
        '侍从线：当前运行文案（0.75后本地修订，未发布）\n\n以下按阅读章排列；同章的路线、选项与旧档变体分别标出，并非全部连续播放。第12章可承诺坦诚后商量保管人，或先把盘交还她并说清隐瞒，两者继续约定。第14章表达顾虑先讨论，只有顾虑子菜单里明确退出才解除约定。第19章m19_b002仅为尚未处理旧伤的旧档兜底，两种修复方式都继续关系；m19_b006／008为新称呼，m19_b007为已建立称呼的旧档。第19章开场后有原版估价经营，后半段在业务结束时播放。第20章三种回答共同进入m20_b009，均确认侍从恋爱。第28章三个回答均承接她自愿留下。\n'
        if lang=='zh' else
        'Attendant route: current runtime text (local revision after 0.75, unpublished)\n\nOrdered by reading chapter. Alternatives and legacy variants are separate, not continuous dialogue. Both chapter 12 repair approaches continue the agreement; returning the original fixes custody with Eleanor. Chapter 14 concerns open a discussion, with withdrawal only as an explicit nested decision. Chapter 19 unresolved-injury repair is a legacy fallback; either approach continues the relationship. m19_b006/008 establish the address, while m19_b007 preserves an existing one. Native appraisal separates the opening and wrap-up. All chapter 20 responses join m20_b009 and confirm the attendant romance. All chapter 28 responses accept her voluntary choice to stay.\n')
    out=[intro,('仅列本轮修订涉及的章节及必要上下文，未改章节略。' if lang=='zh' else 'Covers revised chapters and their necessary context; unchanged chapters are omitted.')]
    for title,ids in sections:
        out.append('\n'+'='*60+'\n'+title+'\n'+'='*60)
        for bid in ids:
            b=blocks[bid]
            out.append(('\n['+bid+'] '+(b.get('label','') if lang=='zh' else '')).rstrip())
            for i,line in enumerate(b['lines']):
                text=line['text'] if lang=='zh' else en[bid][i]
                out.append(line['speaker']+': '+text)
    dividends=json.loads((ROOT/'Content/dividend_ui.json').read_text(encoding='utf-8'))
    out.append('\n'+'='*60+'\n'+('分红反馈（第25章起，变量由游戏填入）' if lang=='zh' else 'Dividend feedback (from chapter 25; values filled by the game)')+'\n'+'='*60)
    for key in ['er_div_first','er_div_established','er_div_visit','er_div_receipt','er_div_receipt_end']:
        out.append('\n['+key+']\nEleanor: '+dividends[key][0 if lang=='zh' else 1])
    choice_ui=json.loads((ROOT/'Content/relationship_choice_ui.json').read_text(encoding='utf-8'))
    existing_ui=json.loads((ROOT/'Content/maid_ui.json').read_text(encoding='utf-8'))
    out.append('\n'+'='*60+'\n'+('关系选项说明与风险确认' if lang=='zh' else 'Relationship choices and risk confirmations')+'\n'+'='*60)
    for key in ['er_maid_confess_yes','er_maid_final_1','er_maid_final_2','er_maid_final_3','er_maid_choice_route_exit','er_maid_choice_route_exit_desc','er_maid_confirm_leak']:
        out.append('\n['+key+']\n'+existing_ui[key][0 if lang=='zh' else 1])
    for key,pair in choice_ui.items():out.append('\n['+key+']\n'+pair[0 if lang=='zh' else 1])
    out[0]=out[0].replace('（0.75后本地修订，未发布）','（0.76）').replace('(local revision after 0.75, unpublished)','(0.76)')
    target=ROOT/'docs'/('MAID_MOTIVATION_READING.'+lang+'.txt')
    target.write_text('\n'.join(out)+'\n',encoding='utf-8')
    print(target)
