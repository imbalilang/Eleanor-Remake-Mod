"""Current-runtime Chinese reading edition. No game or generated content writes."""
from pathlib import Path
import csv
import hashlib
import html
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
VERSION = ET.parse(ROOT/'manifest.xml').findtext('ModVersion')
OUT = ROOT / 'docs'
NAME = '本MOD全剧情阅读版_含实际选项影响_2026-11-09'
loc = {k:v.replace('\\n','\n') for k,v in csv.reader((ROOT/'Localization/zh.csv').open(encoding='utf-8-sig'))}
base = json.loads((ROOT/'Content/story_v06.json').read_text(encoding='utf8'))
maid = json.loads((ROOT/'Content/maid_v05.json').read_text(encoding='utf8'))
dual = json.loads((ROOT/'Content/dual_romance_v04.json').read_text(encoding='utf8'))
blocks = {b['id']: b for ch in base['chapters'] for b in ch['blocks']}
blocks.update({b['id']: b for b in maid['blocks']})
for name in ['interaction_v1', 'relationship_choices_v1']:
    blocks.update({b['id']: b for b in json.loads((ROOT/f'Content/{name}.json').read_text(encoding='utf8'))['blocks']})
blocks.update({b['id']: b for s in dual['scenes'] for b in s['blocks']})
used_blocks, used_keys, used_choices = set(), set(), set()
rows = []
speakers = {'player':'玩家','eleanor':'埃莉诺','lydia':'莉迪亚','mother':'埃莉诺的母亲',
            'uncle':'自称叔叔的男人','thug':'黑市打手','officer':'治安员','narrator':'旁白',
            'news':'新闻','securitynotice':'治安部通知','guest':'晚会来宾','manager':'采购行管事','upperbuyer':'上层买家','buyer':'上层买家'}


def text(key, *args):
    assert key in loc, 'Missing reading localization: '+key
    used_keys.add(key)
    value = loc[key]
    if args:
        value = value.format(*args)
    assert not re.search(r'\{\d+\}',value), 'Unresolved reading variable: '+key
    return value


def h(title, level=3): rows.append(('heading',level,title))
def p(value): rows.append(('paragraph',value))
def effect(value): rows.append(('effect',value))
def speak(name,value): rows.append(('speech',speakers.get(name.lower(),name),value))
def ui(key,speaker='Eleanor',args=()): speak(speaker,text(key,*args))


def b(bid, when='', start=0, stop=None):
    assert bid in blocks, 'Missing runtime block '+bid
    block=blocks[bid]; used_blocks.add(bid)
    label=when or block.get('label','')
    if label and label not in {'叙述','共同对白','共同后续','到店','离店'}: rows.append(('condition',label))
    dual_ids={b['id'] for s in dual['scenes'] for b in s['blocks']}
    prefix='er_v06_'+bid.replace('.','_') if '.' in bid else ('er_dual_' if bid in dual_ids else 'er_maid_')+bid
    for i,line in enumerate(block['lines'],1):
        assert loc.get(prefix+f'_{i:02d}')==line['text'], 'Content/localization differ: '+bid
    for line in block['lines'][start:stop]:
        speak(str(line['speaker']),line['text'])


def many(*ids):
    for bid in ids:b(bid)


def choice(key, impact, condition='', args=(), desc=None):
    assert impact.strip(), 'A choice must describe its actual impact: '+key
    used_choices.add(key)
    rows.append(('choice',text(key,*args),condition,impact,text(desc) if desc else ''))


def event(title, timing='', impact='本段不额外改变现金、声望或关系分数；完成对白后推进下一节点。'):
    h(title)
    if timing:p('出现条件／时间：'+timing)
    effect(impact)


def trade(kind, budget=None, tracking=False, extra=''):
    s='玩家'+('向来客出售商品' if kind=='buy' else '收购来客商品')+'，信用点与物品按实际成交价交换；讲价、批发及交易结束沿用原生流程。'
    if budget is not None:s+=f'来客基础预算{budget}信用点，适用原版修正；不保证一定花完。'
    if tracking:s+='本次来访按实际最差交易行为记录一次公平／让利／宰客／压价，详见前面的交易判定表。'
    effect(s+extra)


def group(prefix, overrides=None, exclude=()):
    overrides=overrides or {}
    for bid in blocks:
        if bid.startswith(prefix) and bid not in exclude:b(bid,overrides.get(bid,''))


def intro():
    h('本MOD全剧情阅读版',1)
    p(f'版本：{VERSION}、后宫设计v0.5；同步人物动机与行文修订、晚会终章关门邀请／次日留白及原生顾客走入演出。本文按现行文本和实际逻辑整理，收录当前使用的剧情及仍可触发的旧档分支。')
    p('共用前段只读一次，随后分别阅读普通恋爱／朋友、侍从、后宫及研究结果分支。标为条件或选择的段落互斥，不会在同一周目全部连播。各类日常、筹资轮换、重复经营、分红和补领放在全文最后。')
    p('“实际影响”包含选项真正提交的金钱、物品、资格、关系和后续变化；对白里提及某项帮助不等于游戏另发一份奖励。原生保释等复用事件以机制摘要衔接，本MOD自行追加的对白完整列出。')
    h('阅读前：日程、交易和路线判定',2)
    p('普通开局的第一次来访最早在第23天；选择0点数中立特性“旧日回想／提前相遇”后，全部目标日提前11天。它不是独立开局路线。章节按前置完成后安排，延迟会顺延；常规调度每天只排一次主要MOD来访，原生紧急事件优先。原普通合作介绍可直接带出采购客，后宫接管后则按独立安全槽处理。后宫30／60／90及侍从筹资均可等待，不承诺在某个固定日全部结束。')
    effect('埃莉诺卖货给玩家：实际付款低于物品基础价值65%算压价；高于基础价值算让利；其余算公平。玩家卖货给她：高于基础价值125%算宰客；低于65%算让利；其余算公平。单次来访中，压价优先于宰客，宰客优先于让利，让利优先于公平。')
    effect('在明确调用关系交易记录的事件中，每次来访最终公平：公平记录+1、好感+1；让利：让利记录+1、好感+2；宰客：宰客记录+1、好感−1；压价：压价记录+1、好感−2。未成交只记一次未成交来访。该规则适用于共同前段的常规埃莉诺交易及侍从的对应拾荒售货；不自动套用到莉迪亚、母亲、普通买家和所有经营事件。')
    p('普通告白资格：母亲安全，未进入丙／丁或锁定状态；公平+让利记录至少3次，并且曾举报假叔叔、成功营救或记录威胁之一。好感数值高本身不能替代这些事实。后宫入口先核对这套资格；侍从告白使用已经选定的侍从关系逻辑。')
    p('研究结果与感情路线分开：甲为归还研究换母亲安全；乙为先落实保护再公开研究；丙为保护不足时仍公开，母亲遇害；丁为交盘、卖盘、擅自公开或明确退出。甲／乙再分别承接普通、侍从、后宫或朋友关系。')
    p('通用选择提示：'+text('er_maid_prompt')+'。除特别注明的旧档兼容外，新流程没有无期限感情暂缓；正常不进货、返回菜单和技术性满仓补领仍可用。')


def common():
    h('第一部　共用前段：来访、箱子与八百信用点',2)
    for n,day in [(1,23),(2,25),(3,27)]:
        chapter=base['chapters'][n-1]
        event(f'共用{n:02d}　'+chapter['title'],f'最早第{day}天；提前特性减11天。')
        trade('sell' if n==3 else 'buy',300 if n==1 else 220 if n==2 else None,True)
        p({1:'本次接受食物。',2:'本次接受家居日用品。',3:'她带来二手高级瓶装水、卫生棉与神经活性香水，按实际采购处理。'}[n])
        for block in chapter['blocks']:b(block['id'])
        if n==1:p('开场“问母亲/只谈货”是对话选择；两条都进入原生食品交易，实际成交价决定 c01.b003–b006 的反应。')
        if n==3:p('这里出售的是她的日用品；交易后才是吊坠二选一，不会把母亲的吊坠卖给玩家。')
    event('共用04　她的“叔叔”：接收、检查、举报','最早第28天。','只有成功拘捕才提交“举报成功”；接受箱子或口头怀疑都不能代替这一事实。')
    b('c04.n001')
    choice('er_op55_c04_opt1','生成带赃物标记和免费特征的奢华补给箱，记录接受／持有／已检查状态，并允许对该人走原生拘捕流程。')
    b('c04.n003','选择收下后的过夜与编号')
    choice('er_op55_c04_opt2','与“收下”走同一段接收代码，也写入已检查状态；登记文本后仍走原生举报。')
    b('c04.n002','选择登记后')
    choice('er_op55_c04_opt3','不生成箱子，叔叔把礼盒带走；本次来访直接推进。')
    b('c04.n004','拒绝后')
    effect('实际举报／拘捕成功：记下已举报，撤销持箱状态。只有原生拘捕成功回调才生效；单击接收不是自动举报。箱子日后已不在掌控中时会按已售出处理。')
    b('c04.b003','治安员实际拘捕该人时')
    b('c04.n005','拘捕后：警官带走礼盒并通知埃莉诺')
    ui('er_line_uncle_done','Narrator')
    event('共用05　把箱子的事告诉她','最早第29天；插在接箱之后。')
    ui('er_line_warning_prompt','Player')
    choice('er_choice_tell_crate','记录已经警告，并清除原生箱子追查热度；根据是否确实知道陷阱播放对应回复。')
    b('c04.n006','已经举报成功：新文本')
    b('c04.n007','谢礼句')
    choice('er_choice_gift_purifier','实际交付原生便携式净水器，当前原版为175耐久；只可与香水二选一。满仓不消耗领取资格，警告章可恢复。','确实掌握陷阱信息，尚未领取回礼。')
    choice('er_choice_gift_perfume','实际交付原生费洛蒙香水，当前原版为5次使用；不是后期的自充盈香水。与净水器互斥。','同上。')
    choice('er_choice_no_gift','不拿物品，直接继续；不写已领物品标记。','本次回礼菜单出现时。')
    b('c04.n008','谢礼后的离店')
    b('c04.b004','未举报/无陷阱分支开头')
    b('c04.b006','知道或接受过箱子，但未完成举报；与上一段互斥')
    b('c04.b008','确实掌握陷阱信息时的共同后续')
    b('c04.b007','既未接箱，也没有确认信息；与前两段互斥')
    b('c04.b009','无陷阱/旧分支的结束')
    choice('er_choice_keep_quiet','不警告、不消除箱子风险，继续下一章。')
    ui('er_line_keep_quiet','Player')
    event('插入事件　赃箱陷阱与原生保释','第13运行章以前、尚未选研究方向；箱子仍是玩家所有的赃物，且接受过、未举报、未警告、未售出。','把箱子放到埃莉诺的交易柜台后会触发拘捕；已检查却未警告会留下“明知陷阱未提醒”的旧伤。剧情暂停，先处理救援，成功后返回被中断的节点。')
    b('c05.b007','埃莉诺因赃物被治安员拘捕')
    effect('复用原生调度：拘捕后D+2治安员来访；可付500信用点，或治安部声望至少40保释。声望路径不扣40点声望。MOD另加“稍后处理”，它保留待救状态；明确放弃则结束本MOD故事。成功办理后D+3释放来访。此处复用的原生正文不冒充MOD新文案。')
    choice('er_choice_rescue_later','当次离场，保留原生保释机会和被中断章节；不扣款、不算救援成功。')
    b('c05.b009','成功释放后：之前并不知道箱子有问题')
    b('c05.b010','成功释放后：之前已知道却没有提醒；与上一段互斥')
    event('共用06　她最不愿意开口的事','最早第30天；若救援插入则顺延。')
    b('c05.n001')
    choice('er_op55_c05_opt1','进入援助节点：可继续用原生食物交易零价/低价援助，或选择原生50信用点现金援助。')
    choice('er_op55_c05_opt2','先播放 c05.n003，再进入同一个援助节点。')
    choice('er_op55_c05_opt3','播放 c05.n004 后直接推进；不进入援助节点，不开放本次交易/现金援助。')
    b('c05.n003','选项2：说清楚')
    b('c05.n005','援助节点')
    trade('buy',40,False,'接受食物、零食和饮料。任一实际成交价不高于基础价值的三分之一（整数向下取整），会记为援助。')
    p('本次交易的原版声望基础系数设为2.0，最终增减仍按原版成交规则计算；不是对白直接送固定声望点。')
    effect('还可使用原生一次性50信用点现金援助；它替代本次物品援助的表现。援助成功后的收束记录一次让利并好感+2；没有援助只增加未成交来访。不会因为只是打开选择就给分。')
    for i in range(1,6):ui(f'er_line_cash_aid_{i}','Player' if i in (1,4) else 'Eleanor')
    p('上面五句仅在选择50信用点现金援助并实际成功后播放，替代物品援助回复。')
    b('c05.n002','提供了低价／免费食物等物品援助后的共同段')
    b('c05.b004','opt1/opt2 没有满足援助条件');b('c05.b006')
    b('c05.n004','选项3：只问，不给，直接推进')
    for n,prefix,day in [(7,'c06',31),(8,'c07',33)]:
        event(f'共用{n:02d}　'+next(x['title'] for x in base['chapters'] if x['id']==prefix),f'最早第{day}天。')
        b(prefix+'.b101')
        trade('sell',tracking=True,extra='实际支付给她的钱逐笔加入调查储蓄，储蓄最多记到800；原生砍价/加价按钮可用。')
        if prefix=='c06':
            b('c06.b003','公平收购');b('c06.b004','溢价收购（实际多付）');b('c06.b005','明显压价');b('c06.b006','没有买下')
            b('c06.b106','成交后的共同后续');b('c06.b107','不卖后的共同后续')
        else:
            b('c07.b006','公平收购');b('c06.b004','溢价收购（实际多付）');b('c07.b007','明显压价');b('c06.b006','没有买下')
            b('c07.b106','共同后续：码头与培养槽')
        effect('成交后按实际最差交易行为记录一次公平／让利／压价；未成交只记未成交来访。公平好感+1，让利+2，压价-2。付款逐笔加入调查储蓄，最多800。')
    event('共用09　不是拿来卖的','最早第35天。','这次是废金属回礼；领取／付款实际成功，或明确婉拒后才继续剧情，不额外加减好感。')
    b('c08.b101')
    ui('er_op55a2_c08_prompt','Player')
    choice('er_op55a2_c08_opt1','成功交付一件废金属，不扣款；放不下则返回菜单，不推进章节，仍可重新选择。');b('c08.b102','实际收到回礼后')
    choice('er_op55a2_c08_opt2','成功交付一件废金属并净支付20信用点；选择时再次核验余额。交付失败会立即退回预留款项，保留菜单，不推进章节。','至少20信用点。');b('c08.b103','付款和交付成功后')
    choice('er_op55a2_c08_opt3','不交付物品、不扣款，照常推进。');b('c08.b104','婉拒')
    b('c08.b105','收下／付款后的共同段');b('c08.b106','婉拒后的共同段')
    b('c08.b005','公平+让利记录至少3次时的后续');b('c08.b006','不足3次；与上一段互斥')
    p('交付未成功时的提示：');ui('er_return_gift_retry','Narrator')
    p('成功领取、付款或婉拒会保存本次选择；中断后重进直接续接对应回应，不重复发物、扣款或提供另一种领取方式。旧档已完成本章时不回补。')
    event('共用10　旧圈子的人：莉迪亚与手工镯','最早第36天。','手镯使用原生收购与砍价；按实际成交价至少为基础价值90%记录“莉迪亚认可的公道价”，低于90%不记录；未买则不记录。不按埃莉诺交易表增加好感或公平次数。')
    b('c09.b101');b('common09_add_added','共同追加：她第一次见到店主')
    b('c09.b102')
    trade('sell')
    b('c09.b002','买下且实付至少基础价值90%');b('common09_add_high_price','公道价追加')
    b('c09.b003','买下但低于90%');b('c09.b004','本次未买')
    b('c09.b106','工作与拒绝');b('common09_add_after','共同追加：她自己的话');b('c09.b107','离店')
    event('共用11　这回是她先说','最早第37天。','本段三个回答只改变对白，不改变现金、好感或后续判定。')
    b('c10.b101')
    ui('er_op55a2_c10_prompt','Player')
    choice('er_op55a2_c10_opt1','只改变本段对白和她的回应，不改变现金、好感或后续判定。',desc=None)
    choice('er_op55a2_c10_opt2','只改变本段对白和她的回应，不改变现金、好感或后续判定。',desc=None)
    choice('er_op55a2_c10_opt3','只改变本段对白和她的回应，不改变现金、好感或后续判定。',desc=None)
    b('c10.b102','选项1后续');b('c10.b103','选项2后续');b('c10.b104','选项3后续')
    b('c10.b105','共同回应：拒绝工作');b('common10_add_family','共同追加：莉迪亚家的背景');b('c10.b106','离开')
    event('共用12　八百信用点与侍从提议','储蓄已到800时最早第39天；未齐则最迟第42天由她补齐，提前特性整体减11天。','新版本由侍从入口事件接管本节点。调查储蓄与玩家预付800是两笔不同的事实；不会把她已经挣够的800当成玩家已付款。')
    many('m11_b001','m11_b002','m11_b003','m11_b004')
    p('前两段到店开场互斥：已挣够用第一段，逾期自行补齐用第二段。自行补齐且此前正面交易不足2次会记录她已卖掉母亲吊坠，影响后续实物赠礼；已售才追加第三段。')
    choice('er_maid_entry_normal','不付800，不建立侍从约定，继续原调查与普通关系路线。');b('c11.b002','选择不以预付工钱介入')
    choice('er_maid_entry_offer','进入她开条件的商谈，本身不扣款、不锁恋爱，也不立即开始称呼或女仆装。',desc='er_maid_entry_offer_desc')
    many('m11_b006','m11_b007','m11_b008')
    choice('er_maid_entry_confirm','实际扣玩家800信用点一次，建立侍从合作约定；此后仍需救母与逐步建立关系。','玩家实际持有至少800。',desc='er_maid_entry_confirm_desc')
    b('m11_b011','付款成功才播放；中断后续接，不重复付款')
    choice('er_maid_entry_back','不扣钱，不建立约定，继续普通调查／恋爱路线。');b('m11_b012','商谈后收回提议')


def protection_choices():
    p('保护菜单提示：'+text('er_prompt_protection'))
    choice('er_choice_protect_sec','扣300信用点；记录治安部渠道和保护落实，维持乙线。','治安部声望至少30，且已举报叔叔或记录威胁，并付得起300。',args=(30,300))
    choice('er_choice_protect_ul','扣400信用点；记录上层渠道和保护落实，维持乙线。','上层声望至少30，研究盘已有去向，并付得起400。',args=(30,400))
    choice('er_choice_protect_case','扣200信用点；凭举报案件落实保护，维持乙线。','已举报叔叔，并付得起200。',args=(200,))
    choice('er_choice_protect_fallback','不扣保护费，改走甲线归还研究救母，记录保护未落实；不是自动把母亲判死。')


def locket_options(maid_route=False):
    p('领取菜单提示：'+text('er_prompt_locket'))
    choice('er_choice_take_locket','实际交付母亲吊坠一次，标为重要纪念物并结束她持有的状态。满仓保留领取菜单，实际交付成功后才进入成功回复。','她仍持有、未卖、未送出；且关系已确认。')
    choice('er_choice_refuse_locket','不发实物，保留她佩戴的叙事；不扣好感，不解除关系。')


def perfume_options():
    p('领取菜单提示：'+text('er_maid_reward_prompt'))
    choice('er_maid_reward_claim','尝试交付原生自充盈神经活性香水一次；成功才写已领标记，满仓则仍可重试。')
    choice('er_maid_reward_later','保留香水待领并继续收束；不是感情暂缓。若该分支未实际播放乙线回执，也不能算已经提交回执。')
    p('选择暂存后的回复：');ui('er_maid_reward_keep')


def order_options():
    ui('er_biz_order_offer')
    choice('er_biz_order_accept','登记可选采购单：2件工具+1件材料，共600信用点采购预算；D+2开始收货，可分批，预算与数量跨来访保留；不直接发600现金。',desc='er_biz_order_terms')
    choice('er_biz_order_decline','不建立采购单，照常继续本章及恋爱／朋友关系；不因此扣好感或关闭路线。')
    ui('er_biz_order_after')


def ordinary():
    h('第二部　普通调查、普通恋爱与朋友路线',2)
    p('承接共用12选择普通协助或收回侍从提议的存档。后宫接受入口前仍先完成母亲来访；一旦双恋接管，同一个原节点不会再额外播放一遍。')
    event('普通13　密码是她的生日','最早第41天；实际受八百调查款节点影响。')
    many('c12.b001','c12.b002')
    choice('er_choice_custody_player','研究盘由玩家保管，继续合作；后续有权选择交盘或卖盘，也因此存在真正背叛的可能。');b('c12.b003')
    choice('er_choice_custody_eleanor','研究盘由她贴身带着，玩家继续协助；玩家持盘限定的交盘／卖盘／偷偷公开选项不开放。');b('c12.b004')
    choice('er_choice_custody_exit','明确结束调查协作，交还保管责任，进入丁·退出分支；锁定本MOD感情后续，但不凭退出就捏造母亲死亡。');b('c12.b005')
    event('普通14　三百信用点的好处','最早第43天。')
    b('c13.b001','假叔叔此前被捕时');b('c13.b002','同上追加')
    b('c13.b003','未举报假叔叔时的替代开场');b('c13.b004')
    choice('er_choice_threat_leave','只让对方离开；不增加威胁记录，继续决定救母／公开。');b('c13.b006')
    choice('er_choice_threat_report','记录威胁，不扣声望、不扣钱。','治安部声望至少30，且已有叔叔举报或威胁记录。',args=(30,))
    choice('er_choice_threat_upper','同样记录可信威胁／接洽，不扣声望或钱。','上层声望至少30，研究盘已有保管去向。',args=(30,));b('c13.b005','以上任一可信接洽成立')
    choice('er_choice_threat_handover','实际获得300，交出研究，进入丁·交盘；关系锁定，后续母亲作为实际获释事实处理。','原件在玩家手中。',args=(300,));ui('er_line_threat_drive_taken','Narrator')
    choice('er_choice_threat_sell','实际获得600，失去研究，进入丁·卖盘；关系锁定，后续母亲遇害。','原件在玩家手中，且黑市声望至少20。',args=(600,));ui('er_line_threat_drive_sold','Narrator')
    event('普通15　先救人，还是先公开','最早第44天；以下世界方向互斥。')
    b('c14.b001');b('c14.b002','已具备可准备保护条件');b('c14.b003','暂不具备保护条件；与上一段互斥');b('c14.b004')
    p('可准备保护的条件为以下任一：治安部声望≥30且有举报／威胁记录；上层声望≥30且研究盘已有保管去向；或者已举报叔叔且调查储蓄达到800。选择乙线后还要在下一次来访真正付款落实。')
    choice('er_choice_route_a','选择归还研究换人，继续救母；此刻不额外收保护费。');b('c14.b005','甲：选择后的回应；玩家实际说出选项本身',start=1)
    choice('er_choice_route_b','选择先保护再公开；尚未自动付费或完成保护。','满足上面的可准备保护条件。');b('c14.b006','乙：选择后的回应',start=1)
    choice('er_choice_route_c','她同意在保护不足下公开，转入丙线；随后母亲遇害，但这与玩家背着她公开的丁线不同。');b('c14.b007','丙：选择后的回应',start=1);b('c14.b008')
    choice('er_choice_secret_leak','玩家擅自公开，进入丁·擅自公开，关系锁定、母亲遇害。','原件在玩家手中。');ui('er_line_secret_leak','Narrator')
    choice('er_choice_route_d','明确退出调查，进入丁·退出，保留盘／母亲的实际已知状态；不发钱、不自动判母亲死亡。');ui('er_line_exit_story','Narrator')
    event('普通16　把承诺办成事情','最早第45天。')
    b('c15.b005','甲线：直接核实交换')
    b('c15.b002','乙线：准备保护');b('c15.b003','正面交易至少3次时追加')
    protection_choices();b('c15.b004','任一保护费用实际支付后');b('c15.b006','改回甲线时，随后接前面的交换段')
    event('普通17　她说了“妈妈”','最早第46天。','母亲生还事实在本节点完成时提交；甲归还研究，乙保留公开研究并解除营养果种子限制。')
    b('c16.b001','甲：交换救母');b('c16.b002','乙：保护救母；与上一段互斥');b('c16.b003')
    b('c16.b004','乙线继续');b('c16.b005','乙线继续')
    choice('er_choice_comp_accept','甲线：实际入账300信用点，按交换结算，不额外加好感。');ui('er_line_comp_accept','Narrator')
    choice('er_choice_comp_family','甲线：不收300，好感+1。');ui('er_line_comp_family')
    choice('er_choice_comp_refuse','甲线：不收300，好感+1；母亲照常获救。');ui('er_line_comp_refuse')
    event('普通18　母亲来买东西','最早第48天；后宫入口必须等此次完成。')
    p('本次收家居用品；不把母亲当成埃莉诺的好感交易次数。')
    trade('buy',220);many('c17.b001','c17.b002');b('c17.b003','甲线离店反馈');b('c17.b004','乙线离店反馈；与上一段互斥');b('c17.b005')
    effect('实际结束来访后记录母亲已来过，不因买不买东西改变救母结果。')
    event('普通19　事情结束以后，她还是来了','最早第50天。')
    trade('sell',tracking=True);many('c18.b001','c18.b002')
    b('c18.b003','正面交易至少3次，或好感至少10');b('c18.b004','不满足上一条件；互斥')
    event('普通20　她没有把这件事算进账里','最早第52天；未被后宫接管时。')
    b('c19.b001','明知箱子有问题却未警告的旧伤尚需回应')
    b('c19.b002','满足原告白资格')
    choice('er_choice_confess_yes','确认普通恋人关系，继续合作与工作后续。');b('c19.b003','恋人回应；玩家说的是选项文本',start=1)
    choice('er_choice_confess_friends','确认朋友关系，继续救母后的经营与世界后续，不把朋友写成永久锁死。');b('c19.b005','朋友回应',start=1)
    b('c19.b006','不满足告白资格，明确收束为朋友，不停在无限等待')
    p('新流程没有“慢一点”的选择。旧档已经暂缓的情况，在后面的合法关系节点只再明确选择一次。')
    event('普通21　这回轮到她帮忙','最早第54天；后宫接管时合作介绍最早第58天。')
    many('c20.b001','c20.b004');b('c20.b005','已经是恋人');b('c20.b006','朋友／尚未确认恋爱；互斥')
    effect('介绍完成后安排一位真实上层采购客，基础预算600，购买奢侈品、艺术品和高档品；钱由实际售货赚取，不是对白直接奖600。该固定合作客不受后宫预算／小费饰品加成。')
    for key in ['er_line_coop_buyer_intro','er_line_coop_buyer']:ui(key,'Narrator')
    event('普通22　莉迪亚又来了一次','最早第55天。');trade('buy',500)
    p('本次偏好奢侈品、艺术品与高档品，不额外写埃莉诺公平／让利次数。')
    b('c21.b001');b('c21.b003','离店共同开头');b('c21.b002','首次手镯实付达到公道价时追加')
    b('c21.b004','已经恋人');b('c21.b005','朋友／旧暂缓；互斥');b('c21.b006')
    event('普通23　她可以去别处','最早第57天。','记录甲／乙的实际工作去向；可接一笔与恋爱无关的采购单，并按吊坠是否仍在、已卖、已送分支处理。')
    b('c22.b001','甲：母女生活');b('c22.b002','乙：研究与复查；互斥');b('c22.b003')
    ui('er_biz_order_lead');order_options()
    b('c22.b004','已经普通恋人，或旧暂缓档这次确认恋爱')
    choice('er_choice_future_confirm','仅旧暂缓档：明确成为恋人；不重付800，也不自动进入侍从或后宫。');b('c22.b005','旧暂缓确认回应',start=1)
    choice('er_choice_future_friends','仅旧暂缓档：明确成为朋友，继续世界后续，不锁剧情。');b('c22.b006','朋友或旧暂缓选择朋友')
    b('c22.b007','确认恋人且吊坠仍在她手里，尚未送出、未明确拒绝')
    locket_options();b('c22.b008','实物交付成功');b('c22.b009','婉拒实物');b('c22.b010','先前已卖掉吊坠，代替实物菜单')
    event('普通24　今天不为别的事','最早第59天。','提交普通主线核心完成；实际领到香水才记已领取。母亲安全且已确认恋人时开放填埋场协助，并记录真结局资格；不直接永久增加拾荒次数。')
    many('c23.b001','c23.b002');b('c23.b003','恋人');b('c23.b004','朋友；与上一段互斥')
    perfume_options();ui('er_line_end_perfume');ui('er_line_scav_assist')
    b('m23_b006','乙线、保护完成且公开研究、母亲生还，并且回执尚未提交；实际读完才开始7天复核等待')
    ui('er_line_core_end','Narrator')


def attendant():
    h('第三部　侍从恋爱：从预付工钱到自愿留下',2)
    p('承接共用12真正支付800并建立约定。下面的侍从场景替代普通13～24对应节点，不把两套对白连播。母亲、研究和原有交易仍按实际选择变化；新称呼与女仆装不是付钱后立即全部交付。')
    event('侍从12　资料与旧伤','接入后下一合法来访；对应运行第13节点。')
    b('m12_b001');b('m12_b004','曾明知箱子是陷阱却未提醒，而且尚未承诺坦诚／完成和解')
    choice('er_maid_reconcile_yes','承诺不再隐瞒，继续协作；不增加一笔付款，也不结束约定。救母后再兑现信任。',desc='er_maid_repair_promise_desc')
    choice('er_maid_repair_handback','原件先交给她，标记这次主动交还与坦诚承诺；继续约定，不收回800。',desc='er_maid_repair_handback_desc')
    b('choicefix_b001','主动交还原件修复旧伤时');many('m12_b002','m12_b003')
    choice('er_maid_choice_custody_keep','研究由玩家保管，继续第一件差事；以后可选的危险交盘／卖盘仍需要二次明确确认。');b('m12_b006')
    choice('er_maid_choice_custody_carry','让她贴身带着，玩家继续帮忙；不走持盘限定的交盘／卖盘。');b('m12_b007')
    many('m12_b008','m12_b009')
    event('侍从13　黑市传话','对应运行第14节点。')
    b('m13_b001');b('m13_b002','假叔叔此前被捕');b('m13_b003');b('m13_b006','原件由她贴身带着')
    choice('er_maid_choice_threat_leave','只让对方离开，继续核验救母办法，不创建可信威胁记录。');b('m13_b008')
    choice('er_maid_choice_threat_report','建立威胁记录，不扣声望或钱。','治安部声望至少30，且已举报叔叔或已有威胁记录。',args=(30,))
    choice('er_maid_choice_threat_upper','建立可信接洽记录，不扣声望或钱。','上层声望至少30，盘已有保管去向。',args=(30,));b('m13_b007','上述可信接洽之一')
    choice('er_choice_threat_handover','先进入明确风险确认；未确认前不交盘、不收300。','原件在玩家手中。',args=(300,))
    choice('er_maid_handover_confirm','获得300、交出研究、解除侍从约定，进入丁·交盘；后续关系锁定，母亲按获释事实处理。',desc='er_choice_threat_handover_desc')
    ui('er_maid_handover_warning','Narrator');ui('er_line_threat_drive_taken','Narrator')
    choice('er_choice_threat_sell','先进入风险确认；未确认前不卖盘、不收600。','原件在玩家手中且黑市声望至少20。',args=(600,))
    choice('er_maid_sell_confirm','获得600、失去研究并解除约定，进入丁·卖盘；母亲遇害，停止新的筹备与分红。',desc='er_maid_sell_risk_desc')
    ui('er_maid_sell_warning','Narrator');ui('er_line_threat_drive_sold','Narrator')
    choice('er_maid_reconsider','返回当前商量菜单；不跨天暂缓，不交盘、不收款、不改变路线。',desc='er_maid_reconsider_desc')
    event('侍从14　核验与争执','对应运行第15节点。')
    many('m14_b001','m14_b002');b('m14_b003','可准备保护');b('m14_b004','暂无法准备保护；互斥');b('m14_b005')
    choice('er_choice_route_a','走甲线核实交换，继续约定。');b('m14_b007')
    choice('er_choice_route_b','走乙线准备保护；稍后还需实际付款。','满足前面的保护资格。');b('m14_b008')
    choice('er_choice_route_c','进入保护不足仍公开的二次确认；仅打开菜单不改变世界。');b('m14_b009')
    choice('er_maid_confirm_leak','她知情同意公开，进入丙线；母亲随后遇害，停止母亲生还的归来篇，但保留双方信任的悲剧后续。',desc='er_maid_public_risk_desc');b('m14_b010')
    choice('er_choice_secret_leak','先显示擅自公开的风险；未最终确认不改路线。','原件在玩家手中。');ui('er_maid_secret_warning','Narrator')
    choice('er_maid_secret_confirm','背着她公开，解除约定、锁定关系，进入丁·擅自公开，母亲遇害。',desc='er_maid_secret_risk_desc');ui('er_line_secret_leak','Narrator')
    choice('er_maid_route_difficulty','先进入能力不足的讨论，不直接退出、不退回普通恋人。',desc='er_maid_route_difficulty_desc');b('choicefix_b003')
    choice('er_maid_choice_route_exit','只在顾虑子菜单明确退出后解除约定；归还保管责任，进入丁·退出；已花的钱不退，不凭退出虚构母亲死亡。',desc='er_maid_choice_route_exit_desc');b('m14_b011')
    b('m14_b013','甲／乙／经她同意的丙分支共同收束，不与丁退出段连播')
    event('侍从15　执行与求助','对应运行第16节点。')
    b('m15_b001');b('m15_b002','甲：核实交换');b('m15_b003','乙：保护办理中')
    protection_choices();b('m15_b004','乙改回甲，接甲的核实交换');many('m15_b005','m15_b006')
    p('侍从保护付款会在选择时重验资格。已付渠道在读档中断后直接续接，不再次扣钱。')
    event('侍从16　她救回来了','对应运行第17节点，最早第46天。','母亲安全，甲归还研究／乙公开并解锁种子；甲在此仅好感+1，不额外给300现金。若先前承诺坦诚，救母后真正完成和解。')
    b('interaction_b007','甲线成功复用早晨开门来访时，替代普通到店报喜开头')
    b('interaction_b008','乙线早晨开门来访；与甲互斥')
    p('早晨入口只改变呈现，原生紧急事件优先，无法呈现时回落普通来访。已在早晨说过的报喜问答会从后文略去，不重演一次。')
    b('m16_b001','甲线普通到店版本');b('m16_b002','甲追加');b('m16_b003','乙普通到店版本，替代甲')
    many('m16_b004','m16_b005','m16_b006');b('m16_b007','有坦诚承诺且此前未完成和解')
    event('王尔德筹备：四个现金项目','侍从约定有效、母亲安全后按已完成剧情逐项可见。','购买不是新增恋爱选择。每项只结算一次；声望达标只降价、不消耗声望。原版难度／派系／开局修正后，王尔德恩惠优先抵扣，剩余才扣现金。')
    p('安顿：基准1000，无本MOD声望半价。救母后可准备，结果最早付款次日在安全来访交付。它不改店铺货架，也不替代原版房屋升级。')
    p('通行与正装：基准3000；下城区声望≥30时基准1500。基础侍从收束完成、恋爱明确且安顿结果已交付后开放；付款后才排归来第24章。')
    p('第一单：基准5000；上层声望≥40时基准2500。归来第24章完成后开放；付款后才排第25章。')
    p('自己的事业：基准8000；上层与下城区声望均≥50时基准4000。第26章真实采购客结束后开放；付款后才排第27章。')
    effect('资金不够可继续营业，没有利息或催缴，不因未买立即解除恋爱。新档每项只有现金方案；旧人脉支付记录与其结果对白保留，不重新追缴。明确解除约定后不再开放新项目，已交付成果和真实支出不倒回、不退款。')
    for i in range(1,5):p(text(f'er_maid_project_{i}')+'：'+text(f'er_maid_project_desc_{i}'))
    event('侍从17　母亲观察','对应运行第18节点。');trade('buy',220)
    p('母亲购买家居用品，本次实际收入与她的预算有关，不直接加埃莉诺好感。')
    b('m17_b001');b('m17_b003','新档实际预付800');b('m17_b004','旧档验证后豁免，替代上一句');b('m17_b005')
    b('m17_b006','甲');b('m17_b007','乙；互斥');b('m17_b008','真实交易结束后')
    effect('完成离店记录母亲来访，不因本次未成交撤销救母事实。')
    event('侍从18　工作与依赖','对应运行第19节点。');trade('sell',tracking=True)
    many('m18_b002','m18_b003','m18_b004');b('m18_b005','本次实际成交');b('m18_b006','未成交；互斥');many('m18_b007','m18_b008')
    b('m18_b009','安顿已付款且付款日已过去、结果尚未交付时，附在安全收束；也可能在别的安全来访播放一次');b('m18_b010')
    event('侍从19　新衣与称呼','对应运行第20节点，最早目标第52天。','普通对白直接接续关系剧情；不生成待估螺丝刀、不进入估价服务，也不发估价费。旧档和解的两种回应仍继续关系。')
    b('m19_b001')
    b('m19_b002','旧档此前明知陷阱却未警告、仍未和解时的兜底')
    choice('er_maid_reconcile_yes','承诺坦诚、修复旧伤，继续本次关系推进；不结束约定。');ui('er_maid_repair_ack')
    choice('er_maid_repair_records','把经过与记录摊开，标记开放记录、完成和解，继续约定。',desc='er_maid_repair_records_desc');b('choicefix_b002')
    b('m19_b005');b('m19_b006','尚未建立私下称呼的新档');b('m19_b008','新称呼后的后续')
    b('m19_b007','旧档已经建立称呼：替代首次称呼段，不重问');effect('此节点完成后才记完整女仆装初次登场。未启用《少女当铺》使用本MOD原版风女仆立绘；启用时选兼容画风，缺图回退不阻断剧情。')
    event('侍从20　她先开口','对应运行第21节点。','三种回应都确认侍从恋爱、保留约定，并预约一位真实上层采购客；不会因选温柔或拌嘴就退回平等恋人／朋友。')
    many('m20_b001','m20_b005','m20_b006')
    choice('er_maid_confess_yes','确认侍从恋爱。',desc='er_maid_confess_continue_desc');b('m20_b008')
    choice('er_maid_confess_close','同样确认侍从恋爱，不新建退出状态。',desc='er_maid_confess_continue_desc');b('choicefix_b004')
    choice('er_maid_confess_tease','同样确认侍从恋爱，不扣好感或减少后续奖励。',desc='er_maid_confess_continue_desc');b('choicefix_b005')
    b('m20_b009','上述三个回应之后的共同收束')
    event('插入采购客　她介绍的人','最早预约次日的安全槽。');trade('buy',600)
    ui('er_maid_buyer_intro','UpperBuyer');ui('er_maid_buyer_close','UpperBuyer')
    b('m20_b003','采购客确实买过东西后的安全来访反馈');b('m20_b004','采购客未成交；互斥')
    effect('客人接受高档品、家居、材料与工具，钱来自真实成交；无论成交与否都记本次来访已结束，反馈只播放一次，不替代付费项目。')
    event('侍从21　莉迪亚又来了','对应运行第22节点。');trade('buy',500)
    p('偏好奢侈品、艺术品与高档品；交易收益与是否延续已确认侍从恋爱无关。')
    b('m21_b001');b('m21_b002','交易结束');b('m21_b003','新档实付800');b('m21_b004','旧档豁免；互斥')
    b('m21_b005','前段手镯按公道价收购');b('m21_b006','已确认侍从恋爱');b('m21_b007')
    event('侍从22　工作、采购单与吊坠','对应运行第23节点。')
    b('m22_b001','甲');b('m22_b002','乙；互斥');b('m22_b003');b('m22_b004','已确认侍从恋爱；特定旧暂缓迁移已听过重复信息时略去')
    order_options();b('m22_b005','吊坠仍在她手里且未卖、未送');locket_options(True)
    b('m22_b006','成功领取');b('m22_b007','婉拒实物');b('m22_b008','已经卖掉吊坠，代替实物赠礼')
    event('侍从23　回去之前','对应运行第24节点。','实际提交基础侍从收束，归来篇开始等项目条件；不因为只到此处就算完整自愿续约结局。')
    many('m23_b001','m23_b002');perfume_options();many('m23_b003','m23_b005')
    b('m23_b006','乙线需要递交复核回执时；必须实际读完才提交')
    many('m23_b007','m23_b008','m23_b009','m23_b010')
    event('归来24　通行与正装','完成基础侍从收束至少2天，且项目二付款次日之后。','提供正常采购，读完结果才记录归来24完成，随后开放项目三。拒绝进货仍可完成本章。')
    b('m24_b001');ui('er_biz_supply_offer')
    choice('er_biz_supply_tools','提供螺丝刀与剪线钳，按正常采购价和原生批发规则收购，不免费赠送。')
    choice('er_biz_supply_materials','提供金属锭与废金属，按正常采购价收购；不发一笔固定收入。')
    choice('er_biz_supply_no','不进货，不扣钱，直接进入后半剧情；不损失恋爱、称呼或项目成果。')
    for key in ['er_biz_supply_trade','er_biz_supply_resume']:ui(key)
    b('m24_b002');b('m24_b003','付款时达到下城区半价，或旧人情付款');b('m24_b004','纯现金、未用本MOD声望优惠；互斥');b('m24_b005')
    b('m24_b006','甲');b('m24_b007','乙，仍在复查');ui('er_maid_one_step','Player');b('m24_b008','乙，已经正式撤案；替代复查段')
    many('m24_b009','m24_b010')
    event('归来25　第一单','归来24完成至少3天，且项目三付款次日之后。','已付第一单可领取免费样品螺丝刀一次；旁边普通货物仍正常收费。不拿样品也推进，之后可补取；完成本章才从下一游戏日起按每天200累计分红。')
    many('m25_b001','m25_b002');b('m25_b003','已经通过真实0价样品交易领取；未领时由保管对白替代');ui('er_biz_sample_later')
    b('m25_b004');b('m25_b005','项目三使用声望优惠／历史人情');b('m25_b006','纯现金；互斥');b('m25_b007','甲');b('m25_b008','乙；互斥');b('m25_b009');ui('er_div_first')
    event('归来26　采购行的管事','归来25完成至少2天；本节点不另收项目费。');trade('buy',600)
    p('本次购买高档品与家居用品。')
    b('m26_b001');choice('er_maid_procure_no1','改变当场回答，不改变关系、预算或结局，也不会把玩家认成过去的主人。');b('m26_b003')
    choice('er_maid_procure_no2','同样仅改变回复；交易与后续继续。');b('m26_b004');b('m26_b005','两项之后共同后续');b('m26_b007','实际结束交易')
    effect('交易结束才记录归来26完成、开放项目四。之后在安全来访附一次采购反馈；没有对白直接给600。');ui('er_maid_procurement_feedback')
    event('归来27　自己的事业','归来26完成至少3天，且项目四付款次日之后。','这里玩家是卖方：她带900采购预算补货，按实际售货取得现金。完整结果后日分红升到600；先结算旧费率天数，不把过去所有天数改成新费率。')
    ui('er_biz_restock_intro');b('m27_b002');b('m27_b005','甲');b('m27_b003','乙且母亲已正式撤案、能够出席');b('m27_b004','乙仍在复查；互斥');b('m27_b006')
    many('m27_b007','m27_b008');p('上面两段仅在项目四当时享受两区声望优惠，或旧人情付款时播放。');b('m27_b009','纯现金替代段')
    ui('er_div_established');many('m27_b010','m27_b011')
    event('归来28　她自己留下','归来27完成至少2天。','三个回答都完成旧约、接受她自愿续留，继续侍从恋爱、分红和晚会资格；无额外付款，不再次提供横跳退线。')
    many('m28_b002','m28_b003')
    for i,bid in [(1,'m28_b005'),(2,'m28_b006'),(3,'m28_b007')]:
        choice(f'er_maid_final_{i}','自愿续约成立，侍从恋爱保留；差异在回应语气与后面的对白。',desc='er_maid_final_continue_desc');b(bid)
        if i==3:b('m28_b008','第三项追加')
    many('m28_b010','m28_b011')
    event('乙线后续　复核、新闻与迟到的出席','回执实际提交7天后尝试发原生新闻；重要新闻可顺延。','新闻实际激活才算正式撤案，不在只付款或只是口头说了时提前判定。母亲后续闲聊一次，不再收调查费。')
    b('world_b003','正式新闻');b('world_b004','新闻之后下一个安全来访，普通恋爱／朋友也可使用');b('world_b005','归来27已完成且当时母亲没能出席')
    gala()


def gala():
    event('侍从线最后一章　关门后的邀请','侍从自愿续约完成至少3天；当天最后一个顾客离开、队列为空，玩家点击关门按钮后。','邀请不进入营业顾客队列，不提前出现。埃莉诺沿普通顾客路径从左侧走向柜台，到达后开始对白。答应后在门口短暂收束，收店并黑屏进入次日清晨，跳过自由夜间；不叙述晚会经过，正常经营与夜间结算照常。次日报纸侧写赴宴，紧急新闻与母亲复核新闻优先。')
    b('gala_b001',stop=2)
    p('第三段外观描述按实际画像替换：')
    p('未开启《少女当铺》：');ui('er_gala_gown_vanilla','Narrator')
    p('开启《少女当铺》且礼服资源可用：');ui('er_gala_gown_beauty','Narrator')
    b('gala_b001','外观之后共同开场',start=3)
    p('未开启《少女当铺》时使用原作埃莉诺上层形象；开启时使用本MOD礼服。开场外观描写随实际画像切换，礼服只在本次场景显示。')
    choice('er_gala_accept','保存答应赴约，在门口写下名字、牵手离开；立即收店，跳过夜间自由行动，调用原生结算进入次日。无赴宴费，服务费、物品周期、健康和每日事件仍正常处理。',desc='er_gala_free');b('gala_b002','只有门口收束，没有晚会正文')
    choice('er_gala_not_attend','明确谢绝本次，照常收店并保留夜间；次日安全来访回应她独自出席。新档不再另约下一场，不扣关系或收益。',desc='er_gala_not_attend_desc');b('gala_b010','新档拒绝／旧预约取消时的当场回应')
    b('gala_b005','已谢绝，次日安全来访的独自出席回应')
    ui('er_gala_news_title','News');ui('er_gala_news_body','News');b('gala_b006','晚会新闻已实际发布后的安全来访，仅一次')
    p('以下仅为旧存档已延期／已预约的续接。旧排队916从营业队列移除，保留阶段；旧延期到期／第二邀请也统一等空店关门后再询问。第二邀请使用同样的答应／拒绝菜单，答应后进入次日，不再另约一个排队访客。')
    b('gala_b007','旧档延期满7天后的那一轮关门邀请，或第二邀请已开始')
    b('gala_b008','旧预约当天的确认');b('gala_b009','旧预约已过期，改为确认下一次可去的晚会；替代上一段')
    choice('er_gala_go','履行旧预约，门口收束后收店、跳过夜间并进入次日；不重复付款。')
    choice('er_gala_cancel','取消这次旧预约，进入独自出席回应；关系、分红与已付款成果保留。')
    p('旧档正在播放晚会（Phase 4）不再恢复晚会正文；在关门时接门口离开并进入次日。若已完成跨日，只补完成／新闻状态，不再跳一天。已读完开场但尚未回答的旧游标可能使用简短续接：');ui('er_gala_wait_answer')


def harem():
    h('第四部　后宫线：埃莉诺与莉迪亚',2)
    p('这是共用救母后明确选择的扩展，不和侍从约定并行。母亲来访必须完成、母亲安全、甲／乙世界结果、原告白资格成立；朋友或锁定存档不开放新入口。已经普通恋人的旧档不自动进入。')
    effect('上层声望30／60／90依次激活，不扣声望、不卖付费通道。只在对应前置完成后检查当时声望；达标一次即锁存，后来下降不撤销。目标未达只暂停主线，原生顾客和对应阶段日常继续。不是早期摸到过90就一次领取全部奖励。')
    event('后宫01　莉迪亚的提议','最早普通第49天，提前第38天；之后日期为可顺延的目标。')
    many('dr01_open','dr01_notice','v05_entry_notice')
    choice('er_dual_choice_dr01_accept_label','明确接受后宫剧情，写下本人选择；这时还未告白、未确认三人关系，也不直接赠三件饰品。',desc='er_dual_choice_dr01_accept_desc');b('dr01_accept')
    choice('er_dual_choice_dr01_decline_label','拒绝扩展，继续原普通调查／恋爱／朋友后续，已发生世界事实保留。');b('dr01_decline')
    event('后宫02／G1　开始荐店','入口完成后，上层声望达到30。','获得荐店铭牌资格，单独交付；满仓不消耗达标资格、不拦住感情剧情，后续可补领。')
    b('v05_reward50')
    p(text('er_dual_plaque_desc'))
    event('后宫03　卖完货之后','替代原19售货，不额外再播放一场原19。');trade('sell',tracking=True)
    many('c18.b001','c18.b002');b('dr03_open','实际结束交易以后，不要求一定买了全部货')
    b('dr03_plaque','铭牌实际陈列');b('dr03_no_plaque','没有陈列；互斥')
    event('后宫04　她自己的事');many('v05_dr04_feedback','dr04_consult_open','dr04_consult','dr04_lesson','dr04_shop')
    event('后宫05　埃莉诺告白','替代原20告白；两种关系选择在这里明确。')
    b('dr05_open')
    choice('er_dual_choice_dr05_confess_label','确认与埃莉诺恋爱、提交原告白事实；后宫主线继续，不再播一场旧告白。');b('dr05_confess')
    choice('er_dual_choice_dr05_friend_label','明确成为朋友，后宫接管结束，归还未完成的原流程；不是永久锁定、不会丢母亲／交易／已领铭牌事实。');b('dr05_friend')
    effect('新入口已经预查原告白资格；若兼容旧状态仍不合格，使用原本明确朋友收束，不用返回空场景无限卡住。没有新的“慢一点”。')
    event('后宫06　三个人吃饭');many('dr06_open','dr06_meal','dr06_after')
    event('后宫07　闺蜜的反馈','替代原22；不是再次预约一笔任务。');many('v05_dr07_open','dr07_confirmed')
    event('后宫08　工作与未来','替代原23，工作和吊坠仍按真实世界事实处理。')
    b('dr08_job_a','甲线且尚未建立工作');b('dr08_job_b','乙线且尚未建立工作；互斥')
    b('v05_dr08_existing','已经有工作，只承接事实，不重新演一次应聘')
    b('dr08_mother_a','甲：母亲仍按流放登记在下层区生活');b('dr08_mother_cleared','乙母亲已正式撤案；互斥');b('dr08_mother_pending','乙仍在复核；互斥');many('dr08_common','dr08_lover')
    effect('复用前面原23的吊坠菜单：未卖、未送、未拒绝且仍由她持有时可以实际接收或婉拒；已经卖掉走已售对白，不复制吊坠。完成该分支才写工作节点已处理。')
    event('后宫09／G2　她自己的生活','工作与原节点奖励处理完成，埃莉诺恋爱明确；当前上层声望达到60。','取得上层宾客珐琅座饰；实际陈列后才有预算效果，单纯拿在背包里不生效。')
    many('v05_reward100','dr09_open');p(text('er_dual_patron_desc'))
    event('后宫10　衣服与发夹');b('dr10_open')
    choice('er_dual_choice_dr10_suit_label','改变当场回应；同样继续，不扣好感、不收筹备费。');b('dr10_option1')
    choice('er_dual_choice_dr10_eleanor_label','同样继续，仅回复不同；不是退回单恋。');b('dr10_option2');b('dr10_common','两项之后共同后续')
    event('原合作　她介绍的采购客','介绍最早普通第58天；与真实买家分开完成，各占安全来访槽。','复用原合作介绍及基础600预算采购客；必须实际结束买家来访才进入新的核心收束。不是把章节号“到了21”当作已经买过。')
    p('完整介绍和买家对白见前面“普通21”；这里只有一份实际发生的合作，不因分支再排一份。')
    event('后宫11～12　她留出来的日子、另一把椅子','原合作及真实买家已处理，随后收束原核心。','香水走真实领取／满仓待领，乙线回执实际读过才提交；旧世界事实不会被跳号伪造。三方关系此时尚未确认。')
    many('dr11_open','dr12_open');perfume_options()
    p('原核心的香水、适用的乙线复核回执与恋人拾荒资格共用一套处理，不再额外发第二瓶香水或侍从分红。乙线未提交回执时先补真实回执，不提前进入后宫坦白。')
    event('后宫13／G3　荐店与退让','真实原核心完成后，上层声望达到90，在莉迪亚坦白之前放行。','取得名媛致意银饰。此后关系讨论不再中途插一道新的经营门槛。')
    many('v05_reward150','dr13_open','dr13_door');p(text('er_dual_gratitude_desc'))
    event('后宫14　她说出来了');b('dr14_open')
    for key in ['dr14_accept','dr14_together']:
        choice('er_dual_choice_'+key+'_label','两项共用同一段后续，都写入正在讨论这段关系；玩家和莉迪亚将分别向埃莉诺表达，不承诺共同出场，也没有隐藏退出效果。')
    b('dr14_accept','两项共同后续')
    event('后宫15　告诉埃莉诺');b('dr15_open')
    choice('er_dual_choice_dr15_wait_label','承认要等她们谈完，关系讨论继续；不产生跨天暂缓状态。')
    choice('er_dual_choice_dr15_steady_label','明确承担答应双方的事，同样继续；不另加费用、不扣资格。')
    b('dr15_wait','两项共同回应')
    event('后宫16　莉迪亚的答复');b('dr16_open')
    event('后宫17　三方确认','只有三份关系表态真正完成才提交。','本段读完确认三人关系；转移未来拾荒协助资格，不再生成新的填埋场协助事件。当天已经激活的事件自然结束，旧成果不清空。')
    b('dr17_open');choice('er_dual_choice_dr17_confirm_label','继续独立同意对白，读完后才记为三人交往；不是单击选项立刻确认。');b('dr17_accept')
    event('后宫18　莉迪亚的约会','确认关系后，才可使用约会形象。');b('dr18_open')
    b('dr18_outfit','成功使用本MOD约会像');b('dr18_fallback','约会图缺失，实际回退日常像；互斥')
    choice('er_dual_choice_dr18_embrace_label','改变约会回应，保持同一三人关系，不发额外数值。');b('dr18_embrace')
    choice('er_dual_choice_dr18_talk_label','改变约会回应，关系同样保留，无隐藏退线。');b('dr18_talk');b('dr18_common','两种回应之后')
    event('后宫19　埃莉诺的约会');b('dr19_open')
    event('后宫20　共同晚饭','目标第67天只是下限安排；门槛等待、entry单独领奖和真实合作买家都可能顺延。','完成后才算完整共同生活收束；约3天后尾声，此后每4天可安排完成后的日常。')
    many('dr20_arrival','dr20_gathering');b('dr20_plaque','铭牌陈列');b('dr20_no_plaque','没有陈列；互斥');b('dr20_closing')
    p('本节点完成提示：'+text('er_dual_notice_ending'))
    event('后宫尾声　之后的安排','共同晚饭后约3天，仅一次。')
    b('dr_epilogue_safe');b('dr_epilogue_route_a','甲')
    b('dr_epilogue_route_b_unsubmitted','乙，尚未提交复核；与下两段互斥')
    b('dr_epilogue_route_b','乙，已提交复核、尚未公布撤案消息')
    b('dr_epilogue_route_b_acquitted','乙，撤案消息已真实公布')
    b('dr_epilogue_plaque_listed','铭牌实际陈列');b('dr_epilogue_plaque_held','已经拿到但未摆出；互斥');b('dr_epilogue_plaque_none','未领取；互斥')


def outcomes():
    h('第五部　丙线、丁线与最终结局',2)
    event('丙01　研究交付','与她商量后决定无充分保护仍公开，目标第46天。','研究公开；不会再回甲／乙救母后的生活主线。')
    b('rc.b001','未建立侍从约定的版本');b('tragic_b002','有侍从历史时替代上段')
    event('丙02　种子与代价','目标第48天。','母亲遇害，调用原版营养果种子解禁。侍从约定结束，停止母亲生还的王尔德归来项目；已支付工钱不退款。')
    b('rc.b002','普通版本')
    b('tragic_b004','侍从历史的共同前段',stop=10)
    b('tragic_b004','新档实际付过800的后两句',start=10)
    b('tragic_b005','旧档豁免，替代上一组实付两句',stop=2)
    b('tragic_b005','新档／旧档的共同结束',start=2)
    event('丙03　遗物与食物','目标第50天。');trade('buy',40)
    b('rc.b003','普通版本的到店');b('rc.b004','普通版本实际成交过食物')
    ui('er_line_no_food','Narrator');p('上一句只在普通版本无食物成交时使用，替代成交回复。')
    b('tragic_b006','侍从历史版本替代到店');b('tragic_b007','她仍持有吊坠');b('tragic_b008','吊坠此前已售；互斥');ui('er_maid_tragic_food_close')
    effect('这章按40基础预算实际买食物；未成交仍继续，不把低库存再判为一条新的失败路线。提起遗物不等于此处自动送出吊坠。')
    event('丙04　加入革命军','目标第52天。','记录她加入革命军的工作去向，继续后续来访。')
    b('rc.b005','普通版本');b('tragic_b009','侍从历史替代版本')
    event('丙05　武器需求','目标第54天。');trade('buy',350)
    b('rc.b006','普通版本');b('rc.b007','实际成交武器');b('rc.b008','没有适合库存，替代成交段');b('rc.b009','共同结束')
    b('tragic_b010','侍从历史的到店');b('tragic_b012','侍从历史的收束，无论成交与否继续')
    event('丙06　莉迪亚单独来访','目标第55天。');b('rc.b010','普通版本');b('tragic_b013','侍从历史替代')
    event('丙07　离开前的谈话','目标第57天。');b('rc.b011','普通版本')
    b('rc.b012','普通旧档已经恋人或旧暂缓时');b('rc.b013','普通档没有上述关系时；互斥');b('tragic_b014','侍从历史替代整个普通段')
    event('丙08　出发','目标第59天。');trade('buy',120)
    many('rc.b014','rc.b015');ui('er_line_leak_perfume','Narrator');many('tragic_b015','tragic_b016')
    effect('普通与侍从历史版本各用自己一套出发对白；结束尝试交付自充盈香水一次并收束故事。此处直接交付，当前没有甲／乙那套满仓领取菜单；腾出空间能避免礼物交付失败。不会因没卖食物阻断离开。')
    event('丁线　生意做到这里','来自交盘／卖盘／擅自公开／明确退出之一，按实际来源择一。','世界结果与关系锁定在这里收束，不继续后宫或侍从归来项目。已发生交易和物品不会凭空回滚。')
    b('rd.b001','普通：交盘300');b('rd.b002','交盘后母亲确实获释的说明；侍从历史也复用这一段')
    b('rd.b003','普通：卖盘600，母亲遇害');b('rd.b004','普通：擅自公开，母亲遇害');b('rd.b005','明确退出，保留未知／实际母亲状态，不捏造死亡')
    many('exit_b005','exit_b006');p('上面两段只在侍从历史的交盘／卖盘背叛时替代普通到店对白。')
    b('exit_b008','侍从历史：擅自公开替代到店');ui('er_maid_betrayal_mother_died','Narrator')
    effect('普通丁·擅自公开当前只记录研究公开与母亲遇害，没有再次调用营养果种子解禁；侍从历史的同类退出会调用解禁。经她同意的丙公开两种版本都会调用解禁。')
    h('最终游戏结束时的追加文字',3)
    p('下面不是又一轮来访。原版死亡、被捕、离站等结果保留，本MOD按实际关系与最高完成阶段追加对应文字；同一角色不会把互斥尾声全部讲一遍。')
    p('普通核心尚未完成、也没有已确认侍从恋爱或已接受后宫时，通常只保留原版结算，不提前套一篇成功恋人尾声。已入后宫但未确认三方时使用讨论中版本；已确认侍从恋爱则按实际归来进度处理。')
    chart=(ROOT/'docs/route_map.html').read_text(encoding='utf8')
    index=json.JSONDecoder().raw_decode(chart.split('const DATA = ',1)[1])[0]
    p('E编号用于和项目路线图对照，保持E1～E14稳定；死亡／被捕／离站作为后缀，不另编新号。图上“完美”表示完整条件成立，已确认但未完成后半不能伪造完整生活。')
    for ending in index['endings']:
        p(ending['id']+'　'+ending['title']+'：'+ending['condition'])
    for bid,when in [('end.b001','普通恋人、母亲生还且平安留站'),('end.b002','普通恋人但离站'),('end.b003','普通被捕／失去自由'),('end.b004','普通死亡'),('end.b005','普通朋友且母亲生还／曾获释'),('end.b006','普通母亲遇害'),('end.b007','丁线关系中止')]:b(bid,when)
    ui('er_maid_ending_exit','Narrator')
    p('明确退出调查优先用上面这段结束语，避免把主动退出误写成背叛。')
    b('ending_b001','侍从恋人、母亲生还，归来篇未完成：基础尾声')
    for i,condition in enumerate(['安顿结果已交付','已完成归来24','已完成归来25','已完成归来26','已完成归来27']):
        b('ending_b002','只追加已经完成的最高阶段：'+condition,start=i,stop=i+1)
    b('ending_b003','归来28完整自愿续约后，替代基础尾声')
    b('ending_b004','乙已撤案：完整侍从终局追加',start=0,stop=1)
    b('ending_b004','乙已撤案，但没有完成该侍从终局：替代上一句',start=1)
    b('ending_b005','乙回执已提交，但尚未正式撤案；不编造历史提交')
    b('ending_b006','侍从恋人离站');b('ending_b007','侍从恋人被捕');b('ending_b008','侍从恋人死亡')
    b('ending_b010','母亲遇害，双方仍有信任的侍从历史悲剧后续；玩家死亡时由原版死亡结果处理')
    b('v05_ending_complete','三方关系确认且共同晚饭已完成')
    b('v05_ending_confirmed','已三方确认但尚未完成共同晚饭；不冒充完整结局')
    b('v05_ending_discussing','已明确入后宫，但关系讨论尚未完成')
    b('dr_epilogue_departure','后宫实际离站后缀');b('dr_epilogue_arrested','后宫实际被捕后缀');b('dr_epilogue_death','后宫实际死亡后缀')


def legacy():
    h('第六部　只在旧存档出现的续接与关系收束',2)
    event('档案员　保留原版还是重开','存档已有原版埃莉诺进度，重制线处于兼容阻挡状态。')
    ui('er_old_prompt_1','档案员');ui('er_old_prompt_2','档案员')
    choice('er_old_prompt_keep','该存档继续原版埃莉诺故事，不启用本MOD重制调度；已得物品保持。',desc='er_old_prompt_keep_desc');ui('er_old_prompt_keep_line','Narrator')
    choice('er_old_prompt_reset','明确清除原版与MOD埃莉诺剧情进度，按新起点重新展开；已得到的物品不收回，不等于只把当前对话重播。',desc='er_old_prompt_reset_desc');ui('er_old_prompt_reset_line','Narrator')
    event('旧八百提议　被旧回调锁住的特定存档','只识别调查款800、尚未处置盘／母亲、停在对应旧节点且没有当前／排队冲突的特定历史形态。','不把所有普通恋人或朋友都改成侍从，也不通用洗白明确背叛。识别后次日安排一次专门来访。')
    b('legacy_b002');b('legacy_b004','本次真实售货有成交');b('legacy_b005','没有成交；互斥');b('legacy_b006')
    trade('sell',tracking=False)
    p('这次旧档来访本身不另生成固定货物；没有实际柜台库存时会走未成交回复，仍能进行下面的兼容选择。')
    choice('er_maid_legacy_help','再进入一次约定讨论，本身不扣钱。',desc='er_maid_legacy_help_desc');many('legacy_b008','m11_b007')
    choice('er_maid_legacy_confirm','按已验证旧档豁免进入侍从约定，新增实付为0；不补扣／虚构退还800，世界调查事实不重置。',desc='er_maid_legacy_confirm_desc');b('legacy_b011')
    choice('er_maid_legacy_back','收回新商谈，继续普通调查，不扣钱。',desc='er_maid_legacy_back_desc');b('legacy_b012')
    choice('er_maid_legacy_retract','不再提侍从，继续普通调查；不自动确认恋爱。',desc='er_maid_legacy_retract_desc');b('legacy_b013')
    event('普通旧暂缓　把没说清的事说清楚','合法工作／关系节点一次确认；已经到核心或完成核心却仍暂缓的异常旧档也处理。','玩家作出明确恋人／朋友选择后保留原世界进度、已付款与物品，不重新交调查款、不自动进入后宫或侍从。')
    b('v05_resolve_open')
    choice('er_choice_future_confirm','原资格仍满足时确认普通恋人；不回放已发生世界剧情。','原告白资格成立。')
    choice('er_choice_future_friends','明确成为朋友，不锁定整个故事；已发生事实保持。');ui('er_dual_resolution_ack')
    p('已明确建立侍从约定、只是旧版侍从告白被记成暂缓的存档，会按已选侍从恋爱修复，不重新收费或重问是否回普通恋人；已经明确转普通／朋友的旧结果保留。')
    event('旧后宫自动标记　一次兼容确认','仅旧版自动写过入口、但没有进一步实质接受证据的模糊存档。','自动入口标记及自动“告白完成”不作为本人同意；保留普通恋爱与世界事实，只问一次，不擅自塞回整段前期。')
    many('v05_legacy_entry','v05_entry_notice')
    choice('er_dual_choice_dr01_accept_label','明确接受后再续接未发生的后宫节点；已完成普通售货、工作、告白和旧结果不重播。')
    choice('er_dual_choice_dr01_decline_label','继续普通恋爱，保留原关系；不会因为拒绝后宫丢掉普通恋人。')
    effect('真正已接受、已三方确认的旧档保留关系；已经越过的剧情可续接豁免，不因此白发新饰品。已持铭牌保留效果；第二、三件和未新取得的奖励仍须真实达到相应声望。旧版跳过合作的存档只记历史续接豁免，不伪造真实成交。')


def daily():
    h('附录　日常、重复经营与正常补领（额外内容）',2)
    p('以下内容统一放在主线与结局之后。它们由当前关系、资金／项目阶段和安全来访槽决定；不存在的关系、未领物品和没发生的成交不会凭空补成事实。')
    event('普通恋人／朋友　核心后的来访','普通核心完成、母亲安全且未锁定；相隔至少8天。','恋人短聊没有新增金币或好感；朋友来访会带一批搜获货物，首次使用朋友段、之后为短交易问候，货款按正常收购结算。恋人依次最多4段，朋友最多5次；不是无限每天来访。侍从有效或后宫接管期间让位给各自后续。')
    for i in range(1,5):b(f'ep.b{i:03d}',f'普通恋人第{i}次后续')
    b('ep.b005','朋友版，按上述排期使用')
    event('侍从筹备　钱还没齐时','基础收束后还在等下一个王尔德项目；距最近闲聊及重要进展至少7天。','每个等待阶段至多安排一段，优先选未见且适合缺钱／缺声望的变体。只轮换闲聊与日期，不给钱、不扣钱、不取消关系、不完成项目。')
    for i in range(1,5):ui(f'er_maid_wait_project_{i}')
    b('wait_b001','钱不够的阶段');b('wait_b002','钱不够且尚未达到项目声望优惠时');b('wait_b003','她也在准备；通用变体')
    event('侍从完成后的四组日常','归来28之后，按至少7天的闲聊间隔，依次播放4组。','不改变最终关系；可以附当时已累计的分红或新闻回应。晚会邀请独立等空店关门，不再附在日间来访中。')
    for i in range(2,6):b(f'daily_b{i:03d}',f'第{i-1}组日常')
    b('daily_b006','第四组之后的共同结束')
    for stage in range(1,4):
        event(f'后宫等待G{stage}　上层声望未到{stage*50}','阶段已激活，角色日常相隔至少4天，以下两段轮换。','只更新日常轮换与日期；不完成主线、不加声望、不改变关系。达标主线优先，不靠一次日常重新开始等待。')
        if stage==2:p('第一段由埃莉诺本人来访，第二段由莉迪亚来访；沿用原有来客、对白和阶段轮换。')
        for variant in (1,2):b(f'wait_{stage}_{variant}',f'轮换第{variant}段')
        p(f'每段末尾的系统说明会显示“上层区声望：〈当前值〉／{stage*50}”和下一奖励；不会反复弹窗催收。')
    event('后宫完整收束后的七组日常','共同晚饭与后宫尾声后，通常每4天轮换，保留三方已确认关系。','没有另加侍从分红或永久拾荒BUFF；可顺带交付当时确实待领的饰品／纪念物。')
    for i in range(1,8):b(f'dr_daily_{i}_open',f'第{i}组日常')
    event('循环经营一　埃莉诺来卖货','正在等王尔德资金、存在安全营业空档，距离重要来访至少2天；经营轮换完成后间隔4天。','两件普通货：废金属和螺丝刀。玩家实际采购才扣钱，不自动帮她凑款，不加一笔固定好感；玩家没钱时轮换会避开这类卖货。')
    ui('er_biz_routine_sell');ui('er_biz_routine_end')
    event('循环经营二　配给站采购','同上，只在卖货／买货两段之间轮换。旧档已排的估价来访改为采购，保留原到期日。','她以固定剩余预算350收工具、材料和家居；赃物、重要物、旧版遗留的待估物及同单已付货不收。只按实际成交给现金，不把350全额当奖。')
    ui('er_biz_routine_buy');ui('er_biz_routine_end')
    event('可选采购单　分次收工具和材料','普通工作／侍从工作时实际接单；最早D+2首次收货，后面间隔2天。','2工具+1材料，共600预算。只收合法且非重要的普通货，实际成交数量及已花预算累计，不因读档重新给600；凑齐或预算用尽即收束。首次收货之后超过4天会结束未完需求，已付货款不回收，不伤恋爱／救母。')
    ui('er_biz_order_collect',args=('〈已交工具数〉','〈已交材料数〉','〈剩余预算〉'))
    ui('er_biz_order_partial');ui('er_biz_order_done');ui('er_biz_order_budget');p(text('er_biz_order_expired'))
    event('第一单样品　补取','项目三已实际支付、已经有样品资格，但还没通过真实交易领过。','免费样品螺丝刀一次；不取继续替玩家保管，之后隔7天可再问，不重新交筹备费。当前样品不会在再次读档或多次进菜单时复制。')
    ui('er_biz_sample_pickup');ui('er_biz_sample_close');ui('er_biz_sample_later')
    event('合作分红　每天累计、来访时领取','第一单结果完成后从下一游戏日起200／天；事业结果完成后从下一游戏日起600／天。','收益按游戏日累计，她没来不会丢失；专门结算通常每4天可安排，重要剧情优先。实际读到收款句才将待领余额转为玩家现金，重复对白不再领。也会附在她自己的经营／日常来访，其他采购客不替她发钱。旧完成档从升级时开账，不凭空补过去收入；母亲遇害或重大背叛锁定后停止新增计收益，历史普通／朋友档依真实已付合作、进展和母亲安全资格处理，不只按称呼判断。')
    ui('er_div_visit');ui('er_div_first');ui('er_div_established')
    ui('er_div_receipt',args=('〈本次可领取金额〉',));ui('er_div_receipt_end')
    p('现金接近原生整数上限时只发能容纳的部分，其余继续待领；第28章情感收束本身不插一次发薪。')
    event('回头客消息　她带来的好消息','第一单完成至少2天，恋爱与母亲安全，且尚未听过；插在已有安全来访。','仅一次剧情反馈，不凭这段消息额外加钱、声望或货物；当时真实待领分红仍可单独领取。')
    b('interaction_b006')
    event('填埋场随机事件　埃莉诺的协助','母亲生还、恋人核心完成并有协助资格；原生填埋场有可用拾荒次数时参与随机。','每天70%机会进入原生事件选择，持续出现当天，拾荒次数+2且不会受伤。原生固定事件优先，不保证每天出现。后宫在dr17确认后停止生成未来协助，当天已激活的自然结束。')
    p(text('er_scav_event_title'));p(text('er_scav_event_desc').replace('\\n','\n'));p(text('er_scav_event_attempts')+'；'+text('er_scav_event_safe'))
    event('铭牌引荐　通用上层买家','确实拥有并摆出已解锁铭牌，橱窗有合适可售精品；每可用营业日只抽一次50%，真实到访至少间隔D+2，最多一名已承诺待来访。','实际普通BUY交易，默认基础预算500、偏好奢侈品／艺术品／高档品；不把所有普通家居都算精品。不与恋爱确认变成75%。取下铭牌停止新引荐，已经排入的客人照约来；不会直接发500现金。')
    ui('er_dual_referral_intro','UpperBuyer');ui('er_dual_referral_close','UpperBuyer')
    p('珐琅座饰与银饰不要求同时摆铭牌：到店时按玩家所有的真实橱窗陈列作快照，普通上层买家最终预算+30%向下取整；额外小费为本次来访实际商品付款累计10%，每访最多150，只补差额。它们不扣买家预算，不把小费再计入小费或声望交易权重。')
    p('符合范围的是原生普通上层客和本MOD通用引荐客；具名剧情、固定合作采购、旧任务、服务、赠礼和其他MOD顾客不套效果。重复副本不叠加，取下／重放／读档不重乘当次预算。')
    event('旧预约第二买家　只兑现这一笔','只有v0.81实际已预约、尚未完成的便携净水器买家。','基础预算600，只收耐久至少150的便携净水器。实际成交一次即结束本单；未成交也结束本次，不新建第二轮任务或口碑升级。新档没有这份预约。')
    ui('er_dual_biz_second_intro','UpperBuyer',args=(600,150));ui('er_dual_referral_close','UpperBuyer')
    event('正常来访补领　满仓与真正遗失','确实已有领奖资格，或已完成核心但纪念物未交付。','先查受支持存储：仍持有不复制，扫描不明不补发。饰品已领后首次确认遗失，至少到次日再可补；反复扫描不把日期永远往后推。缺空间仍保留资格，可附在后续日常或补领来访。')
    b('v05_claim_open');ui('er_dual_claim_line',speaker='Lydia',args=('〈实际待领的饰品名称〉',));ui('er_dual_perfume_claim')
    p('后宫满仓或旧版已知漏发的香水，以及能证明没有提供过的吊坠，按真实领取／拒绝／已售状态处理。无法确认旧普通恋人是否曾拒绝吊坠时，不凭空重新赠送；重复选领取不会复制唯一奖励。')
    ui('er_maid_safe_visit');p('上面这句也可作为安顿、采购反馈或母亲新闻结果的安全短聊开场：只交付真正待反馈的结果，每项一次。')
    p('技术领取／付款未成功时的原文：');ui('er_maid_payment_retry','Narrator')
    p('读档后仍有未完对白时的续接提示：');ui('er_dual_entry_resume','Narrator')
    p('各常规商贸事件还可出现的统一成交回复，不另加关系分数：');ui('er_line_trade_accept','当前交易来客')


def write():
    source_files=[p for p in (ROOT/'src').glob('*.cs') if not p.name.endswith('.g.cs') and p.name!='DebugTools.cs']
    source='\n'.join(p.read_text(encoding='utf-8-sig') for p in source_files)
    constants={'B_'+bid.upper().replace('.','_'):bid for bid in blocks if '.' in bid}
    expected={constants[c] for c in re.findall(r'StoryV06Content\.(B_[A-Z0-9_]+)',source) if c in constants}
    expected|=set(re.findall(r'"([^"\n]+)"',source)) & set(blocks)
    expected|={f'dr_daily_{i}_open' for i in range(1,8)}|{f'wait_{s}_{v}' for s in range(1,4) for v in (1,2)}
    expected|={f'wait_b{i:03d}' for i in range(1,4)}|{f'daily_b{i:03d}' for i in range(2,7)}
    missing=sorted(expected-used_blocks)
    required_choices=set(re.findall(r'new StoryEngine\.Choice\(L\.Get\("([^"]+)"',source))
    required_choices|=set(re.findall(r'\bOption\("(er_[^"\n]+)"',source))
    required_choices|=set(re.findall(r'GuardedOption\([^,]+,"(er_[^"\n]+)"',source))
    required_choices|={'er_dual_choice_'+c['id']+'_label' for sc in dual['scenes'] for c in sc.get('choices',[])}
    required_choices|={f'er_maid_final_{i}' for i in range(1,4)}|{'er_choice_refuse_locket','er_maid_reward_later','er_maid_entry_back'}
    missing_choices=sorted(k for k in required_choices-used_choices if not k.endswith('_'))
    # Inspect direct runtime dialogue calls too, not just generated story blocks.
    inline=set()
    for match in re.finditer(r'(?:StoryEngine\.Text|\.SetText)\s*\(',source):
        i=match.end();start=i;depth=1;quoted=False;escaped=False
        while i<len(source) and depth:
            c=source[i]
            if escaped:escaped=False
            elif quoted and c=='\\':escaped=True
            elif c=='"':quoted=not quoted
            elif not quoted:
                if c=='(':depth+=1
                elif c==')':depth-=1
            i+=1
        inline|={k for k in re.findall(r'"(er_[^"\n]+)"',source[start:i]) if k in loc and not k.startswith('er_name_')}
    missing_inline=sorted(inline-used_keys)
    report={
        'edition':f'{VERSION} / harem design 0.5',
        'source':'runtime source, current Chinese localization; retired writing excluded',
        'reachableBlocks':len(expected),'renderedBlocks':len(used_blocks),
        'missingReachableBlocks':missing,'missingOptionImpacts':missing_choices,
        'missingInlineDialogue':missing_inline,
        'renderedChoiceImpacts':len(used_choices),'renderedUIKeys':len(used_keys),
        'notCalledBlocks':sorted(set(blocks)-expected),
        'sourceSHA256':{str(p.relative_to(ROOT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest()
                         for p in source_files+[ROOT/'Content/story_v06.json',ROOT/'Content/maid_v05.json',ROOT/'Content/dual_romance_v04.json',ROOT/'Content/interaction_v1.json',ROOT/'Content/relationship_choices_v1.json',ROOT/'Localization/zh.csv']},
    }
    (OUT/(NAME+'_覆盖核对.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    if missing or missing_choices or missing_inline:
        raise ValueError('Uncovered runtime content: '+str(missing)+'; choices: '+str(missing_choices)+'; inline: '+str(missing_inline))
    md=[];plain=[];body=[];toc=[];head_count=0
    for row in rows:
        kind=row[0]
        if kind=='heading':
            _,level,title=row;head_count+=1;anchor='s'+str(head_count)
            md.append('#'*level+' '+title+'\n');plain.append('\n'+'═'*(48 if level<=2 else 32)+'\n'+title+'\n'+'═'*(48 if level<=2 else 32))
            if level>1:toc.append((level,title,anchor))
            body.append(f'<h{level} id="{anchor}">{html.escape(title)}</h{level}>')
        elif kind=='speech':
            _,speaker,value=row;value=value.replace('\\n','\n')
            md.append(f'**{speaker}**：{value}\n');plain.append(f'{speaker}：{value}\n')
            escaped=html.escape(value).replace('\n','<br>')
            body.append('<p class="speech"><b>'+html.escape(speaker)+'</b><span>'+escaped+'</span></p>')
        elif kind=='choice':
            _,label,condition,impact,desc=row
            value='【选项】'+label
            if condition:value+='\n条件：'+condition
            value+='\n实际影响：'+impact
            if desc:value+='\n游戏内说明：'+desc
            md.append('> '+value.replace('\n','\n> ')+'\n');plain.append(value+'\n')
            body.append('<div class="choice"><strong>选项　'+html.escape(label)+'</strong>'
                        +('' if not condition else '<p>条件：'+html.escape(condition)+'</p>')
                        +'<p>实际影响：'+html.escape(impact)+'</p>'
                        +('' if not desc else '<p class="muted">游戏内说明：'+html.escape(desc)+'</p>')+'</div>')
        else:
            value=row[1];prefix='实际影响：' if kind=='effect' else '条件／分支：' if kind=='condition' else ''
            md.append(('> ' if prefix else '')+prefix+value.replace('\n','\n\n')+'\n');plain.append(prefix+value+'\n')
            body.append('<p class="'+kind+'">'+html.escape(prefix+value).replace('\n','<br>')+'</p>')
    # Stable contents list in both portable text formats.
    contents=['## 目录\n']+['    '*(level-2)+'- '+title for level,title,_ in toc]
    md.insert(4,'\n'.join(contents)+'\n')
    plain.insert(4,'目录\n'+'\n'.join('  '*(level-2)+title for level,title,_ in toc)+'\n')
    css='''
    :root{color-scheme:light;--ink:#24252b;--muted:#6b6770;--line:#dfd8cf;--paper:#fffdfa;--accent:#7a3c4d}
    *{box-sizing:border-box}body{margin:0;background:#f3eee7;color:var(--ink);font:17px/1.95 "Microsoft YaHei","Noto Sans CJK SC",sans-serif}
    aside{position:fixed;inset:0 auto 0 0;width:290px;overflow:auto;padding:24px 16px;background:#28232c;color:#ded6dd;font-size:13px;line-height:1.65}
    aside h2{font-size:17px;color:#f2d3ad;margin:0 0 14px}aside a{display:block;color:inherit;text-decoration:none;padding:5px 8px;border-radius:4px}aside a:hover{background:#483b48;color:white}.toc3{margin-left:12px}
    main{max-width:1160px;margin-left:290px;padding:44px 70px 100px;background:var(--paper);min-height:100vh}
    h1{font-size:32px;line-height:1.4;margin:0 0 28px;color:#573043}h2{font-size:25px;border-top:3px solid #c5ab9b;padding-top:24px;margin-top:60px}h3{font-size:20px;margin:42px 0 20px;color:#63384b;scroll-margin-top:24px}
    p{margin:14px 0}.speech{display:grid;grid-template-columns:100px 1fr;gap:12px;margin:15px 0}.speech b{color:#725660;font-size:14px;padding-top:3px}
    .effect{background:#eaf0ef;border-left:3px solid #71938d;padding:12px 18px;font-size:15px}.condition{font-size:14px;color:#7f6656;border-top:1px dashed var(--line);padding-top:12px;margin-top:28px}
    .choice{padding:14px 20px;border:1px solid #dbcbb1;border-radius:6px;background:#fff6e8;margin:24px 0;font-size:15px}.choice strong{color:#744d2a}.choice p{margin:6px 0}.muted{color:var(--muted);font-size:13px}
    #top{position:fixed;right:18px;bottom:18px;background:#573043;color:white;padding:9px 15px;border-radius:22px;text-decoration:none;font-size:13px}
    @media(max-width:900px){aside{position:static;width:auto;max-height:300px}main{margin:0;padding:28px 22px}.speech{grid-template-columns:75px 1fr;gap:8px}h1{font-size:27px}}
    @media print{aside,#top{display:none}main{margin:0;max-width:none;padding:0}body{background:white;font-size:11pt}h2{break-before:page}.choice{break-inside:avoid}}
    '''
    nav=''.join('<a class="toc'+str(level)+'" href="#'+anchor+'">'+html.escape(title)+'</a>' for level,title,anchor in toc)
    document='<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>本MOD全剧情阅读版</title><style>'+css+'</style><aside><h2>全剧情阅读版 · 目录</h2><p>'+html.escape(VERSION)+'／后宫v0.5<br>Ctrl+F 可搜索全文</p>'+nav+'</aside><main>'+''.join(body)+'</main><a id="top" href="#s1">回到开头</a></html>'
    (OUT/(NAME+'.md')).write_text('\n'.join(md)+'\n',encoding='utf8')
    (OUT/(NAME+'.txt')).write_text('\n'.join(plain)+'\n',encoding='utf-8-sig')
    (OUT/(NAME+'.html')).write_text(document,encoding='utf8')
    print(json.dumps({k:v for k,v in report.items() if k!='sourceSHA256' and k!='notCalledBlocks'},ensure_ascii=False))
    print('Wrote complete reading edition: Markdown / UTF-8 text / HTML')


def main():
    intro();common()
    # Further sections are below; the daily appendix is deliberately last.
    ordinary();attendant();harem();outcomes();legacy();daily();write()


if __name__=='__main__':main()
