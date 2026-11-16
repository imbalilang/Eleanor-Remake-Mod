"""One-time migration of the legacy route page; edit current HTML directly afterward."""
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
page = ROOT / 'docs/route_map.html'
html = page.read_text(encoding='utf-8')


def read_object(name):
    return json.JSONDecoder().raw_decode(html.split(f'const {name} = ', 1)[1])[0]


old_data, old_chart = read_object('DATA'), read_object('CHART')
if old_data.get('scope') == 'current_rewrite':
    raise SystemExit('Already migrated; edit docs/route_map.html and sync the site copy.')
archive = ROOT / 'docs/archive/route_map_legacy_1.05_2026-11-15.json'
if not archive.exists():
    archive.parent.mkdir(parents=True, exist_ok=True)
    archive.write_text(json.dumps({'reason': '作者要求从玩家攻略移除旧节点与过时结局；编号永久保留，不复用。',
                                  'data': old_data, 'chart': old_chart}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def act(key, title, day, early, scenes, summary, checks, source_ids=()):
    return dict(id=key, title=title, day=day, early=early, scenes=scenes,
                summary=summary, checks=checks, sourceIds=list(source_ids))


guides = [
    dict(id='common', name='共同剧情', intro='先看这里，再选关系路线。救母方向决定研究与母亲的结果，关系路线决定之后和谁、以什么关系相处。',
         entry=[
             '普通开局第23天、提前开局第12天开始。以下日期都是及时完成前一场的示例；被捕、手镯后续、主线插队与缺钱等待会使日程顺延。',
             '想走普通恋爱：在告白前累计至少3次有效的公平／让利记录，并完成“举报假叔叔、救出被捕的埃莉诺、保护路线正式记录威胁”中的至少一项。好感高不能代替这两个判定。',
             '想走后宫：重点完成旧手镯实际交割，且不要建立有效侍从关系；不需要先通过普通告白，也不要求先刷到30／60／90声望。',
             '想走侍从：第二次拾荒时选“差多少我补”，实际买货后仍有缺口并成功补款，再在共事来访中答应。单纯多付货款或等她自己凑齐不算入线。'
         ], acts=[
             act('common-act1', '第一幕 · 相识与箱子', '23–30', '12–19',
                 ['不是给上层区开的店', '缺口不在喝水那边', '你按东西开价', '自称叔叔的人／警告回访', '她最不愿意开口的事'],
                 '前三次交易是积累普通告白资格最直接的机会；假叔叔事件决定是否留下风险承担记录。',
                 ['有效记录按一次完整交易来访结算，不是每件商品加一次。尽量按正常价格成交；她卖货时不要压价，她买货时不要抬价或威胁。同一来访中有压价／抬价记录，会优先按较差行为结算。',
                  '最稳妥的风险承担方式：收下假叔叔带来的箱子后举报，并完成治安部带走他的流程。只在后续提醒埃莉诺箱子有问题，不等于完成举报。',
                  '若埃莉诺被捕，等原生保释来访：现金500，或治安部声望40免费担保；缺钱可暂缓，明确拒绝救援会结束剧情。办妥后还要完成获释回访，后续才恢复。'],
                 ['c01', 'c02', 'c03', 'c04', 'c05']),
             act('common-act2', '第二幕 · 侦探费与旧手镯', '31–39起', '20–28起',
                 ['她自己带来的货', '第二次，她记住了', '不是拿来卖的', '旧手镯', '她看见的／掮客／回购', '最后一截或共事约定'],
                 '侍从资格在第二次拾荒的实际补款中取得；后宫资格在莉迪亚手镯交还中取得。两者都不能只靠选了对白却没有交割。',
                 ['侍从：按货成交后补到侦探费800，扣的是剩余差额。她已经自己凑满800、未成交、未补款或现金不足，都不会触发共事入口。',
                  '后宫：实际买下莉迪亚出售的那只家传手镯；买入后按持有计数保留，不依赖具体存放在哪里。掮客1000报价时不卖，等莉迪亚回购。选“按原价还”或“让她自己出价”后，还要实际把一只手镯卖给她；500回购也有效。“直接送她”会直接交还，同样有效。',
                  '手镯不买、卖给掮客、提前卖给其他人、或回购来访未交还就离场，都会失去新后宫资格。想留在普通恋爱路线，可在前奏不买手镯或卖给掮客。',
                  '未走侍从时，调查来访会等侦探费累计800，或到普通第42／提前第31天由她补齐；等到日期不等于获得侍从资格。手镯后续也会占用额外来访。'],
                 ['c06', 'c07', 'c08', 'c09', 'c10', 'c11']),
             act('common-act3', '第三幕 · 救母与关系分流', '41–52', '30–41',
                 ['生日', '跑腿钱', '三条路', '母亲来买东西', '事情结束以后，她还是来了', '她没有把这件事算进账里'],
                 '救母选择先决定研究归还、研究保留或革命军路线；安全救母后的共同交接，再决定后宫或普通告白。',
                 ['“替她收着／先问她盘里还有什么”都继续；威胁中的“不收，话带到／叫他出去”也都继续，并不要求交盘给打手。',
                  '“交盘换人”＝母亲安全、研究归还，对应E1／E3／E4或朋友E2；“先保人再公开”（侍从版为“申请保护”）＝母亲安全、研究保留，对应E5／E7／E8或朋友E6；“立即公开”＝母亲死亡，转革命军。当前重制入口没有额外300／400保护费或声望30渠道门槛。',
                  '普通路线要完成母亲来访，再完成埃莉诺的交接来访。具备手镯资格且没有有效侍从关系时，后宫入口会优先出现，接管原本的普通告白；新入口并没有“接受／拒绝后宫”按钮。',
                  '没有后宫接管时，普通告白检查“至少3次有效公平／让利记录＋至少一次风险承担”。保护救母路径会记录威胁；交盘换人则要在此前举报过假叔叔或实际救出过被捕的埃莉诺。满足后选“我也喜欢你。跟你以前住在哪儿没关系。”；不满足或选做朋友，进入E2／E6。'],
                 ['c12', 'c13', 'c14', 'c17', 'c18', 'c19'])]),
    dict(id='regular', name='恋爱线', intro='共同告白之后的三幕恋爱日常，最终走到婚礼。研究归还与研究保留共用这段故事。',
         entry=[
             '先按共同剧情积累至少3次有效公平／让利记录和一次风险承担，安全救母后明确接受告白。仅把好感刷高不够。',
             '不要进入侍从共事或取得会接管后续的后宫入口。新档想稳定走本线，旧手镯前奏不要取得后宫资格。',
             '完成共同告白后，下一次合适的来访开始第一幕；之后每场完成后至少隔一天。买卖结果影响反应，不设母亲认可分数或额外刷好感门槛。'
         ], acts=[
             act('regular-act1', '第一幕 · 母亲搬下来了', '54–56', '43–45',
                 ['母亲搬下来了', '营养果', '关门前'], '索菲娅通过买东西认识店主，埃莉诺在关门前来访。',
                 ['前两场准备日用品、食物；对话后仍要完成或结束当次交易，听完收尾才算该场完成。没成交也有对应收尾，不会因此锁死路线。',
                  '“埃莉诺／反问她／钱花得完”等是当场回应，都会继续，没有母亲的隐藏通过分数。'],
                 ['love_a1_01', 'love_a1_02', 'love_a1_03']),
             act('regular-act2', '第二幕 · 停水与配给', '57–59', '46–48',
                 ['停水', '价签', '家常衣服'], '停水、配给和街坊的生意让她们逐渐把店主当作自己人。',
                 ['三场分别采购日用品、食物、甜食。正常结束每次买卖即可继续；定价只改变交易反应。',
                  '本幕没有要求玩家记录真实涨价百分比，也没有必须选择的“认可”答案。'],
                 ['love_a2_01', 'love_a2_02', 'love_a2_03']),
             act('regular-act3', '第三幕 · 婚礼与回家', '60–63', '49–52',
                 ['旧友', '嫁给我', '名单', '婚礼'], '莉迪亚来访、求婚、商量宾客，最后完成婚礼与结局页。',
                 ['求婚不要求持有元件、项链、戒指或前期回礼；“开口求婚／等她先说／让她先挑货”都继续。',
                  '“就你们几个”只改为小型婚礼，其余答案也继续婚礼。完成最后一场：研究归还为E1，研究保留为E5。'],
                 ['love_a3_01', 'love_a3_02', 'love_a3_03', 'love_a3_04'])]),
    dict(id='harem', name='后宫线', intro='手镯前奏、共同交接之后，按四幕推进三人的关系；新档不再走旧版撮合与声望关卡。',
         entry=[
             '共同线实际买下莉迪亚的家传手镯，不卖给掮客，最终按原价／500回购或直接赠还给她，完成同一只物品的交割。',
             '母亲必须安全获救，母亲来访与埃莉诺交接都已完成；没有有效侍从关系，也没有先进入普通恋爱卷。',
             '资格成立后，莉迪亚带埃莉诺来访《窗口》，新后宫开始。这里的两组选项只改变回应，均入线；不存在图上旧写法的“明确接受／拒绝后宫”。不需要普通告白的3次交易门槛。',
             '上层声望30／60／90只在后续空闲来访送独立经营奖励，不阻挡四幕或终章，也不消耗声望。'
         ], acts=[
             act('harem-act1', '第一幕 · 窗口与旧物', '51–56', '40–45',
                 ['窗口（入口）', '怀表', '血袋', '矿石'], '完成入口后隔2天开始怀表，随后按来访顺序推进。',
                 ['入口及本幕各组选项都会接回主线。收回怀表、先不说血袋结局或不让碰矿石，不会关闭后宫。'],
                 ['d01', 'dv01', 'dv02', 'dv03']),
             act('harem-act2', '第二幕 · 打手与保护', '57–60', '46–49',
                 ['问路', '傍晚（同日续访）', '关门前', '扫玻璃'], '打手打听来访者，三人遭到袭击，再一起善后。',
                 ['《问路》完成后，《傍晚》可在同一天继续来访，先等这次续访完成，再按后续日期推进。',
                  '“隔窗护住她们／让她们从巷子另一头走”均继续，不另检查战斗属性、现金、声望或所谓保护成功分数。'],
                 ['dv04', 'dv05', 'dv06', 'dv07']),
             act('harem-act3', '第三幕 · 两次靠近', '61–64', '50–53',
                 ['搬货', '开价', '开门', '停下'], '埃莉诺先告白；莉迪亚约下休店一天，再得知她的心意。',
                 ['本幕的回答只改变反应；两段关系都随剧情推进，不需要额外先完成普通告白。',
                  '2000信用点是莉迪亚支付给玩家的休店款，玩家不需要预存2000。约定的次日来访会关店、黑幕并结算一次；之后继续下一次来访。'],
                 ['dv08', 'dv09', 'dv10', 'dv11']),
             act('harem-act4', '第四幕 · 祝福与点头', '65–67', '54–56',
                 ['祝福', '点头'], '莉迪亚说出自己的感情，三人确认共同关系，完成结局页。',
                 ['《祝福》的三个回应都继续；终章吻埃莉诺、吻莉迪亚或隔窗揽住两人，都确认同一段三人关系。',
                  '研究归还为E4，研究保留为E8。区别在共同线已经作出的救母选择，不在亲吻对象，也不在声望奖励是否领齐。'],
                 ['dv12', 'dv13'])]),
    dict(id='maid', name='侍从线', intro='序幕的缺口借款和共事约定决定入线，之后四幕走向驻场、晚会邀请与私下尾声。',
         entry=[
             '第二次拾荒《还差一截》选“差多少我补”。选项要求当前持有现金足够覆盖侦探费800的缺口；之后必须实际买下一件有付款的拾荒货。',
             '当次交易结束时，系统重新计算800减去她累计所得，只补仍大于0的实际差额。钱不足、未成交、未补款或她自己已经凑齐800，都不取得共事资格。',
             '补款成功后，等《用我自己的名字》来访，选“点头答应”才正式入线；“婉拒”回共同线，已垫调查款不退。',
             '第三幕之前必须在王尔德购买P2“通行与正装”。P1／P3／P4只是可选经营项目。新侍从告白的三种回答都继续，不套用普通告白的3次交易门槛。'
         ], acts=[
             act('maid-prologue', '序幕 · 借款与共事', '33、39起', '22、28起',
                 ['还差一截', '用我自己的名字'], '先成交并补缺口，再答应按天计工的共事约定。',
                 ['不能靠单纯等到800调查款自动补齐来入线，也不能只点“差多少我补”后不买货就离场。',
                  '如果没有看到共事选项，先回查这次真实补款，而不是去补买P1。想走恋爱或后宫，可在共事来访婉拒。'],
                 ['v2_a0c1', 'v2_a0c2']),
             act('maid-act1', '第一幕 · 数据盘与救母', '41–46', '30–35',
                 ['夹层里的盘', '跑腿钱', '原话', '换了地方（保护路径）', '日用品'], '在《原话》中选择救母方向；不同安全结果都能继续侍从。',
                 ['“交盘换人”保住母亲、交回研究，继续走向E3；“申请保护”保住母亲并保留研究，继续走向E7。两个选项都可用，没有额外声望或保护款门槛。',
                  '“立即公开”会使母亲死亡并结束侍从关系，转入革命军四幕；这是换路线，不是侍从线的普通语气选项。',
                  '交盘换人会略过保护路径的《换了地方》，之后同样完成母亲来访。'],
                 ['v2_a1c1', 'v2_a1c2', 'v2_a1c3', 'v2_a1c4', 'v2_a1c5']),
             act('maid-act2', '第二幕 · 合伙人与告白', '48–50', '37–39',
                 ['按人摆', '交完班'], '她自己讲货、改变称呼，再主动告白。',
                 ['告白的“说我也是／两样都给／叫她名字”都会确认侍从恋人。',
                  '本幕完成后，王尔德才开放P2“通行与正装”：基础3000信用点，下层声望达到30时基础优惠价1500；最终价格还受原生难度与其他折扣影响，以界面报价为准。声望只给折扣，不是购买资格。',
                  '若下一幕一直不来，先确认P2已实际付款，再继续营业等来访；不用先买P1，P3／P4也不拦主线。'],
                 ['v2_a2c1', 'v2_a2c2']),
             act('maid-act3', '第三幕 · 第一次登台', '52–55', '41–44',
                 ['想试一次', '唱快了', '再唱一晚'], 'P2已付款后进入试唱、第一次失利和再次登台。',
                 ['“让她自己换曲／问她学到什么／提一首稳的歌”都会继续第二次登台，没有额外唱功数值检查。',
                  '完成成功返场后，继续营业等第四幕；不是到这里就已经出现最终CG。'],
                 ['v2_a3c1', 'v2_a3c2', 'v2_a3c3']),
             act('maid-act4', '第四幕 · 驻场与嘉宾栏', '61–67', '50–56',
                 ['多搬了一排椅子', '旧约那一页', '嘉宾栏', '柜台灯下'], '正式驻场、结清旧约与续约、上层晚会邀请，最后回到窗口。',
                 ['旧约、邀请的三个回应都接回收束，不需要在结局前额外购买P3／P4，也没有另一笔续约费。',
                  '这里按常规剧情来访推进；不用沿旧版攻略等清空队列后点关门来触发另一个晚会。完成《柜台灯下》：交盘换人为E3，申请保护为E7。'],
                 ['v2_a4c1', 'v2_a4c2', 'v2_a4c3', 'v2_a4c4'])]),
    dict(id='rev', name='革命军线', intro='在救母方向中立即公开研究后进入，按四幕完成远征归来。',
         entry=[
             '共同线《三条路》选“立即公开”，或侍从第一幕《原话》选同名选项。母亲会死亡；侍从关系若已建立也会在此结束。',
             '不要求先成为恋人，不检查普通告白的交易次数，不要求上层／革命军声望或旧版补给次数。',
             '完成每场对话及交易收尾，等下一次来访；部分场景安排在安全的清晨。这里的“门还没开”是剧情标题，不要求玩家额外操作侧门。'
         ], acts=[
             act('rev-act1', '第一幕 · 入队与补给', '46–48', '35–37',
                 ['第一张单子', '药先拿'], '她加入队伍，通过当铺销货和采购药品。',
                 ['交易完成或正常结束并听完收尾即可推进，成交／未成交分别有反应；不需要刷旧版反复补给来开下一幕。'],
                 ['v2_r1c1', 'v2_r1c2']),
             act('rev-act2', '第二幕 · 名单与损失', '51–57', '40–46',
                 ['短了一行', '空着手', '第七行'], '名单缩短、空手来访，逐步写出队伍的损失。',
                 ['本幕回复均继续当前四幕，没有用钱或声望扭转母亲死亡的隐藏分支。'],
                 ['v2_r2c1', 'v2_r2c2', 'v2_r2c3']),
             act('rev-act3', '第三幕 · 等候与空位', '60–62', '49–51',
                 ['门还没开', '不用等'], '她第一次杀人后再来，谈到等候与给她留下的位置。',
                 ['按来访顺序读完两场即可继续，不需要告白、侍从称呼或额外夜间约会。'],
                 ['v2_r3c1', 'v2_r3c2']),
             act('rev-act4', '第四幕 · 远征归来', '64–72', '53–61',
                 ['折价', '带话', '快到了', '门外'], '她远征离站，革命军来客带话，最后她回到窗口。',
                 ['最后一场《门外》全部读完，听到“我好想你”后完成E9并显示CG。未读完终章是未完成状态，不单列成一条新结局。'],
                 ['v2_r4c1', 'v2_r4c2', 'v2_r4c3', 'v2_r4c4'])])
]

endings = [e for e in old_data['endings'] if int(e['id'][1:]) <= 9]
conditions = {
    'E1': '未进入侍从／后宫；至少3次有效公平／让利记录＋至少一次风险承担；交盘换人；接受普通告白；完成恋爱第三幕《婚礼》。',
    'E2': '交盘换人、母亲安全；普通告白条件不足或明确选择做朋友，沿共同后续收束。',
    'E3': '第二次拾荒实际补缺口＋答应共事；交盘换人；购买P2；读完侍从第四幕《柜台灯下》。',
    'E4': '实际买下并交还莉迪亚手镯；无有效侍从关系；交盘换人；完成母亲来访和共同交接；读完后宫第四幕《点头》。',
    'E5': '未进入侍从／后宫；至少3次有效公平／让利记录＋至少一次风险承担；先保人再公开；接受普通告白；完成恋爱第三幕《婚礼》。',
    'E6': '先保人再公开、母亲安全；普通告白条件不足或明确选择做朋友，沿共同后续收束。',
    'E7': '第二次拾荒实际补缺口＋答应共事；申请保护；购买P2；读完侍从第四幕《柜台灯下》。',
    'E8': '实际买下并交还莉迪亚手镯；无有效侍从关系；先保人再公开；完成母亲来访和共同交接；读完后宫第四幕《点头》。',
    'E9': '救母方向选择立即公开；母亲死亡；读完革命军四幕，完成《门外》与“我好想你”。'
}
for e in endings:
    e['condition'] = conditions[e['id']]
    e['note'] = ''
    if e['id'] == 'E9':
        e['title'] = '革命军线·远征归来'

data = dict(version=old_data['version'], updated='2026-11-15', scope='current_rewrite',
            routes={k: {'name': v['name']} for k, v in old_data['routes'].items() if k != 'failure'},
            guides=guides, endings=endings)
axis = dict(x=150, top=20, bottom=75, yTop=40, yBottom=2460, step=5,
            earlyOffset=11, label='普通天 / 提前开局 = −11')
nodes, edges = [], []


def node(key, day, x, w, lines, cls, guide, h=62):
    nodes.append(dict(id=key, day=day, x=x, y=40+(day-20)*44, w=w, h=h,
                      lines=lines, cls=cls, guide=guide))


def edge(a, b, cls='common', label=None):
    e = dict(from_=a, to=b, cls=cls)
    e['from'] = e.pop('from_')
    if label:
        e.update(label=label, labelT=.75)
    edges.append(e)


node('C_ACT1', 23, 230, 440, ['共同第一幕 23–30', '相识 / 三次交易 / 假叔叔'], 'common', 'common-act1')
node('C_TRADE', 27, 230, 440, ['普通告白准备：有效交易 ≥3次', '另需 举报 / 救出她 / 保护记录威胁 之一'], 'decision', 'common-act1')
node('C_ACT2', 31, 230, 440, ['共同第二幕 31–39起', '侦探费 / 旧手镯 / 共事'], 'common', 'common-act2', 50)
node('GAP', 33, 230, 440, ['侍从前提：差多少我补 → 实际买货', '仍有差额并成功垫款，才取得共事资格'], 'decision', 'maid-prologue', 56)
node('BRACELET', 36, 230, 440, ['后宫前提：买下手镯 → 不卖掮客', '最终实际回购 / 赠还给莉迪亚'], 'decision', 'common-act2', 56)
node('WORK', 39, 230, 440, ['共事来访：点头答应 → 侍从', '婉拒 / 未实际补款 → 继续共同剧情'], 'decision', 'maid-prologue', 56)
node('C_ACT3', 41, 230, 440, ['共同第三幕 41–52', '数据盘 / 威胁 / 救母 / 关系分流'], 'common', 'common-act3')
node('DIR', 44, 230, 440, ['交盘换人：研究归还；先保人再公开：研究保留', '立即公开：母亲死亡 → 革命军'], 'decision', 'common-act3', 64)
node('HANDOFF', 50, 230, 440, ['母亲来访 → 埃莉诺交接完成', '手镯资格有效且无侍从关系 → 后宫优先'], 'common', 'common-act3', 56)
node('LOVE', 52, 230, 440, ['无后宫接管 → 普通告白', '有效交易 ≥3次＋风险承担 → 可接受告白', '不满足 / 做朋友 → E2或E6'], 'decision', 'common-act3', 66)
for a,b in [('C_ACT1','C_TRADE'),('C_TRADE','C_ACT2'),('C_ACT2','GAP'),('GAP','BRACELET'),('BRACELET','WORK'),('WORK','C_ACT3'),('C_ACT3','DIR'),('DIR','HANDOFF'),('HANDOFF','LOVE')]:
    edge(a,b)
node('E2', 59, 230, 210, ['E2 · 朋友', '研究归还'], 'bad', 'E2', 48)
node('E6', 59, 460, 210, ['E6 · 朋友', '研究保留'], 'bad', 'E6', 48)
edge('LOVE','E2','bad'); edge('LOVE','E6','bad')

for key,day,title,guide in [
    ('L1',54,['恋爱第一幕 54–56','母亲搬下来了 / 营养果 / 关门前'],'regular-act1'),
    ('L2',57,['恋爱第二幕 57–59','停水 / 价签 / 家常衣服'],'regular-act2'),
    ('L3',60,['恋爱第三幕 60–63','旧友 / 嫁给我 / 名单 / 婚礼'],'regular-act3')]:
    node(key,day,780,480,title,'regular',guide)
edge('LOVE','L1','regular','条件满足并接受告白');edge('L1','L2','regular');edge('L2','L3','regular')
for key,x,label in [('E1',780,'★ E1 · 研究归还'),('E5',1035,'E5 · 研究保留')]:
    node(key,63,x,225,[label,'婚礼完成'],'perfect' if key=='E1' else 'regular',key,50)
    edge('L3',key,'perfect' if key=='E1' else 'regular')

for key,day,title,guide in [
    ('H1',51,['后宫第一幕 51–56','窗口 / 怀表 / 血袋 / 矿石'],'harem-act1'),
    ('H2',57,['后宫第二幕 57–60','问路 / 傍晚 / 关门前 / 扫玻璃'],'harem-act2'),
    ('H3',61,['后宫第三幕 61–64','搬货 / 开价 / 开门 / 停下'],'harem-act3'),
    ('H4',65,['后宫第四幕 65–67','祝福 / 点头'],'harem-act4')]:
    node(key,day,1340,480,title,'harem',guide)
edge('HANDOFF','H1','harem','手镯交割＋无有效侍从');edge('H1','H2','harem');edge('H2','H3','harem');edge('H3','H4','harem')
for key,x,label in [('E4',1340,'★ E4 · 研究归还'),('E8',1595,'E8 · 研究保留')]:
    node(key,67,x,225,[label,'点头 / 三人关系确认'],'perfect' if key=='E4' else 'harem',key,50)
    edge('H4',key,'perfect' if key=='E4' else 'harem')

node('M0',39,1900,480,['侍从序幕 33 / 39起','实际补款 → 点头答应共事'],'maid','maid-prologue',56)
node('M1',41,1900,480,['侍从第一幕 41–46','数据盘 / 跑腿钱 / 原话 / 救母'],'maid','maid-act1',62)
node('M_DIR',44,1900,480,['交盘换人 → E3；申请保护 → E7','立即公开 → 结束侍从，转革命军'],'decision','maid-act1',56)
node('M2',48,1900,480,['侍从第二幕 48–50','按人摆 / 交完班 / 告白'],'maid','maid-act2',56)
node('P2',50,1900,480,['王尔德：实际购买P2「通行与正装」','基础3000；下层声望30基础优惠1500'],'decision','maid-act2',56)
node('M3',52,1900,480,['侍从第三幕 52–55','想试一次 / 唱快了 / 再唱一晚'],'maid','maid-act3',62)
node('M4',61,1900,480,['侍从第四幕 61–67','驻场 / 旧约 / 嘉宾栏 / 柜台灯下'],'maid','maid-act4',62)
edge('WORK','M0','maid','答应共事');
for a,b in [('M0','M1'),('M1','M_DIR'),('M_DIR','M2'),('M2','P2'),('P2','M3'),('M3','M4')]:edge(a,b,'maid')
for key,x,label in [('E3',1900,'★ E3 · 交盘换人'),('E7',2155,'E7 · 申请保护')]:
    node(key,67,x,225,[label,'柜台灯下完成'],'perfect' if key=='E3' else 'maid',key,50)
    edge('M4',key,'perfect' if key=='E3' else 'maid')

node('R0',44,2460,480,['革命军入口：立即公开','母亲死亡；不要求先确认恋爱'],'rev','guide-rev',56)
for key,day,title,guide in [
    ('R1',46,['革命军第一幕 46–48','第一张单子 / 药先拿'],'rev-act1'),
    ('R2',51,['革命军第二幕 51–57','短了一行 / 空着手 / 第七行'],'rev-act2'),
    ('R3',60,['革命军第三幕 60–62','门还没开 / 不用等'],'rev-act3'),
    ('R4',64,['革命军第四幕 64–72','折价 / 带话 / 快到了 / 门外'],'rev-act4')]:
    node(key,day,2460,480,title,'rev',guide)
edge('DIR','R0','rev','立即公开');edge('M_DIR','R0','rev','立即公开')
for a,b in [('R0','R1'),('R1','R2'),('R2','R3'),('R3','R4')]:edge(a,b,'rev')
node('E9',72,2460,480,['★ E9 · 远征归来','读完「我好想你」后完成'],'perfect','E9',56);edge('R4','E9','perfect')
chart = dict(nodes=nodes, edges=edges, axis=axis, lanes=[
    dict(x=x,y=20,w=w,h=2460,cls=cls,label=label)
    for x,w,cls,label in [(180,550,'common','共同剧情 / 入线条件'),(760,540,'regular','恋爱线'),
                          (1320,540,'harem','后宫线'),(1880,540,'maid','侍从线'),(2440,540,'rev','革命军线')]])

for name, obj in [('DATA',data),('CHART',chart)]:
    start = html.index(f'const {name} = ') + len(f'const {name} = ')
    _, length = json.JSONDecoder().raw_decode(html[start:])
    html = html[:start] + json.dumps(obj,ensure_ascii=False,indent=2) + html[start+length:]

html = html.replace('<b>14</b> 个结局','<b>9</b> 个现行结局').replace('十四种结局','九种现行结局')
html = html.replace('上层区的富家千金，正在下层区的当铺里等你。','上层区的富家千金，正在当铺窗口外等你。')
html = html.replace('埃莉诺在当铺柜台后','埃莉诺与当铺窗口')
html = html.replace('<a href="#map">流程图</a>','<a href="#map">流程图</a>\n    <a href="#guide">入线条件与分幕攻略</a>')
html = html.replace('每条路线只有一个 ★ 完美结局。选错选项不等于坏档，其他结局同样是完整的收束。',
                    '按当前重制新档整理。先核对入线条件，再按幕推进；点路线卡查看具体操作。')
html = html.replace('<article class="route-card regular">','<article class="route-card regular"><a class="card-link" href="#guide-regular" data-route="regular" aria-label="查看恋爱线条件与分幕攻略"></a>')
html = html.replace('<article class="route-card harem">','<article class="route-card harem"><a class="card-link" href="#guide-harem" data-route="harem" aria-label="查看后宫线条件与分幕攻略"></a>')
html = html.replace('<article class="route-card maid">','<article class="route-card maid"><a class="card-link" href="#guide-maid" data-route="maid" aria-label="查看侍从线条件与分幕攻略"></a>')
html = html.replace('<article class="route-card rev">','<article class="route-card rev"><a class="card-link" href="#guide-rev" data-route="rev" aria-label="查看革命军线条件与分幕攻略"></a>')
replacements = {
    '普通恋爱确认，不进入侍从或双恋':'有效交易≥3次＋风险承担；接受普通告白',
    '双恋入口、保护事件、三人确认':'手镯实际交还＋安全救母＋共同交接',
    'C07 只补实际缺口入线；P2 开门第三幕，驻场与上层晚会邀请收束。':'拾荒成交后补足实际缺口，答应共事；P2开启第三幕，驻场与晚会邀请收束。',
    '完成或续约侍从线；双恋不开放':'真实补款＋答应共事；王尔德购买P2',
    '选择未保护公开，母亲死后她加入革命军；v2 四幕写补给/销赃、队伍损失、第一次杀人和远征归来。最后一句是她的“我好想你”。':'选择立即公开，母亲死后她加入革命军。四幕写补给、队伍损失、第一次杀人和远征归来。',
    '完成 v2 280–290；E9/E10；旧 R01–R14 保留旧档':'救母方向选“立即公开”，读完第四幕',
    '纵轴按及时交接的排期示例。提前开局模式下，目标日期整体提前 11 天。':'常规节点统一按幕；金色为四个完美结局。点击节点查看条件与操作。',
    '<b>研究归还 / 研究保留</b> 代表两种世界结果：前者母亲安全留在下层区，后者母女团聚且研究保留；它们不是路线分类。★ 为完美结局。':'本表只列当前重制新档的E1–E9。<b>研究归还／研究保留</b>由前期救母选择决定；★为完美结局。',
    '四个：<b>E1</b> 恋爱线、<b>E4</b> 后宫线、<b>E3</b> 侍从线、<b>E9</b> 革命军线。其余结局不是完美结局，但同样是完整的收束，不会中途断掉。':'四个：<b>E1</b> 恋爱线、<b>E4</b> 后宫线、<b>E3</b> 侍从线、<b>E9</b> 革命军线。E1／E3／E4需要在救母时选择“交盘换人”；E9需要“立即公开”。',
    '重制后宫线的 30／60／90 上层声望达标后，各有一次独立的莉迪亚奖励来访，主线照常推进。旧版后宫存档仍沿用原有声望排期。':'后宫线30／60／90上层声望只给独立奖励，不拦四幕；侍从P2下层声望30只给优惠，不是入线或购买门槛。普通恋爱看有效交易与风险承担，不看高好感替代。',
    '不能。有效的侍从契约不开放双恋，朋友档也不会收到双恋入口。想走后宫线，就不要先建立侍从关系。':'不能。想走后宫就不要答应建立侍从共事。新后宫资格在手镯前奏决定，安全救母与共同交接完成后优先接管后续；不能先进入普通恋爱卷，再临时改成新后宫。'
}
for before, after in replacements.items():
    if before not in html:
        raise ValueError('Missing replacement: '+before[:60])
    html=html.replace(before,after)
note_start=html.index('  <div class="chart-note">')
note_end=html.index('</div>',note_start)+len('</div>')
html=html[:note_start]+'''  <div class="chart-note"><b>先看判定，再看日期：</b>普通告白的交易记录、手镯交割、侍从实际补款和P2付款是主要门槛。点击图中节点可跳到下面对应的分幕攻略。日期只是及时推进示例；入线失败不会靠等天数自动修复。研究归还对应E1／E3／E4，研究保留对应E5／E7／E8，朋友为E2／E6，立即公开转E9。</div>'''+html[note_end:]
html=html.replace('<main>','<main>')
insert=html.index('\n<section id="endings"')
html=html[:insert]+'''
<section id="guide" class="guide-section" aria-labelledby="guide-title">
  <div class="section-head"><div><div class="eyebrow">Walkthrough</div><h2 id="guide-title">入线条件与分幕攻略</h2></div><p>先读“必须做到”，再按幕看剧情顺序和推进条件。这里只写当前新档流程。</p></div>
  <div class="guide-filter" role="group" aria-label="筛选路线攻略">
    <button type="button" data-guide-filter="all" aria-pressed="true">全部</button>
    <button type="button" data-guide-filter="common" aria-pressed="false">共同剧情</button>
    <button type="button" data-guide-filter="regular" aria-pressed="false">恋爱线</button>
    <button type="button" data-guide-filter="harem" aria-pressed="false">后宫线</button>
    <button type="button" data-guide-filter="maid" aria-pressed="false">侍从线</button>
    <button type="button" data-guide-filter="rev" aria-pressed="false">革命军线</button>
  </div>
  <div id="route-guides"></div>
</section>
'''+html[insert:]
style='''
[id]{scroll-margin-top:100px}
.route-card{position:relative}.card-link{position:absolute;inset:0;z-index:1;border-radius:18px}
.card-link:focus-visible{outline:3px solid var(--gold);outline-offset:-4px}
.guide-section{max-width:1280px;margin:0 auto;padding:clamp(24px,4vw,44px) clamp(18px,4vw,48px)}
.guide-filter{display:flex;gap:8px;flex-wrap:wrap;margin:18px 0 24px}
.guide-filter button{font:inherit;font-size:13px;padding:9px 15px;color:var(--muted);background:var(--panel);border:1px solid var(--line);border-radius:9px;cursor:pointer}
.guide-filter button[aria-pressed="true"]{color:var(--gold);border-color:var(--gold);background:#282113}
.guide-route{border:1px solid var(--line);border-top:3px solid var(--gold);border-radius:16px;padding:24px;margin-bottom:28px;background:var(--panel)}
.guide-route[data-route="regular"]{border-top-color:var(--regular)}.guide-route[data-route="harem"]{border-top-color:var(--harem)}
.guide-route[data-route="rev"]{border-top-color:var(--rev)}
.guide-route h3{margin:0;font-size:24px}.guide-intro{color:var(--muted);line-height:1.8;font-size:14px}
.entry-box{border:1px solid rgba(217,178,111,.35);background:rgba(217,178,111,.04);border-radius:12px;padding:16px 20px}
.entry-box h4{color:var(--gold);margin:0 0 8px;font-size:15px}
.entry-box ul,.act-checks{padding-left:21px;margin:0;line-height:1.9;font-size:14px}
.entry-box li+li,.act-checks li+li{margin-top:7px}
.act-list{list-style:none;padding:0;margin:24px 0 0}.guide-act{padding:22px 0;border-top:1px solid var(--line)}
.guide-act:last-child{padding-bottom:0}.act-heading{display:flex;align-items:baseline;justify-content:space-between;gap:8px 20px;flex-wrap:wrap}
.act-heading h4{font-size:18px;margin:0}.act-date{color:var(--gold);font-size:12px}
.scene-list{display:flex;flex-wrap:wrap;gap:7px;list-style:none;padding:0;margin:13px 0}
.scene-list li{font-size:12px;line-height:1.5;border:1px solid var(--line);border-radius:7px;background:var(--panel-2);padding:5px 9px}
.act-summary{color:var(--muted);font-size:14px;line-height:1.8;margin:12px 0}
.guide-back{display:inline-block;color:var(--gold);font-size:12px;margin-top:20px}.chart-node{cursor:pointer}
.chart-node:focus-visible rect{stroke-width:4}.guide-act:target,.guide-route:target{outline:2px solid var(--gold);outline-offset:6px}
@media(max-width:600px){.guide-route{padding:18px 15px}.entry-box{padding:14px}.entry-box ul,.act-checks{font-size:13px}.guide-route h3{font-size:22px}}
'''
html=html.replace('</style>',style+'</style>')
js='''
function escapeText(value){return String(value).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));}
function showGuide(route){
  document.querySelectorAll('.guide-route').forEach(el=>{el.hidden=route!=='all'&&el.dataset.route!==route;});
  document.querySelectorAll('[data-guide-filter]').forEach(el=>el.setAttribute('aria-pressed',String(el.dataset.guideFilter===route)));
}
function followGuide(id){
  const target=document.getElementById(id);if(!target)return;
  const route=target.closest('.guide-route');if(route)showGuide(route.dataset.route);
  location.hash=id;target.scrollIntoView({behavior:'smooth',block:'start'});
}
function renderGuides(){
  $('#route-guides').innerHTML=DATA.guides.map(g=>`<article class="guide-route" id="guide-${g.id}" data-route="${g.id}">
    <h3>${escapeText(g.name)}</h3><p class="guide-intro">${escapeText(g.intro)}</p>
    <div class="entry-box"><h4>必须做到</h4><ul>${g.entry.map(t=>`<li>${escapeText(t)}</li>`).join('')}</ul></div>
    <ol class="act-list">${g.acts.map(a=>`<li class="guide-act" id="${a.id}"><div class="act-heading"><h4>${escapeText(a.title)}</h4><span class="act-date">普通 ${escapeText(a.day)} 天 · 提前 ${escapeText(a.early)} 天</span></div>
      <ol class="scene-list" aria-label="剧情顺序">${a.scenes.map(t=>`<li>${escapeText(t)}</li>`).join('')}</ol>
      <p class="act-summary">${escapeText(a.summary)}</p><ul class="act-checks">${a.checks.map(t=>`<li>${escapeText(t)}</li>`).join('')}</ul></li>`).join('')}</ol>
    <a class="guide-back" href="#map">↑ 回到流程图</a></article>`).join('');
  document.querySelectorAll('[data-guide-filter]').forEach(el=>el.addEventListener('click',()=>showGuide(el.dataset.guideFilter)));
  document.querySelectorAll('[data-route].card-link').forEach(el=>el.addEventListener('click',e=>{e.preventDefault();followGuide('guide-'+el.dataset.route);}));
}
let suppressNodeClickUntil=0;
'''
html=html.replace('function renderChart(){',js+'\nfunction renderChart(){')
html=html.replace('    const group=svgEl("g");','''    const group=svgEl("g",{class:"chart-node",tabindex:"0",role:"link","aria-label":n.lines.join('，')+'，查看攻略',"data-guide":n.guide});
    const title=svgEl('title');title.textContent=n.lines.join('，')+'：点击查看条件与分幕攻略';group.appendChild(title);
    group.addEventListener('click',()=>{if(Date.now()>suppressNodeClickUntil)followGuide(n.guide);});
    group.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();followGuide(n.guide);}});''')
html=html.replace('<tr>\n    <td class="eid">${e.id}', '<tr id="${e.id}">\n    <td class="eid">${e.id}')
html=html.replace('  renderChart(); renderEndingTable(); applyZoom();','  renderGuides(); renderChart(); renderEndingTable(); applyZoom();')
html=html.replace('    drag={id:e.pointerId,x:e.clientX,left:chart.scrollLeft};\n    chart.classList.add(\'dragging\');\n    chart.setPointerCapture(e.pointerId);',
                  "    drag={id:e.pointerId,x:e.clientX,y:e.clientY,left:chart.scrollLeft,moved:false};")
html=html.replace('    chart.scrollLeft=drag.left-(e.clientX-drag.x);', '''    if(!drag.moved&&Math.hypot(e.clientX-drag.x,e.clientY-drag.y)<6)return;
    if(!drag.moved){drag.moved=true;chart.classList.add('dragging');chart.setPointerCapture(e.pointerId);}
    chart.scrollLeft=drag.left-(e.clientX-drag.x);''')
html=html.replace("    chart.classList.remove('dragging');", "    if(drag.moved)suppressNodeClickUntil=Date.now()+250;\n    chart.classList.remove('dragging');")
html=html.replace("  chart.addEventListener('pointercancel',endDrag);", "  chart.addEventListener('pointercancel',endDrag);\n  if(location.hash)followGuide(decodeURIComponent(location.hash.slice(1))); ")
page.write_text(html,encoding='utf-8')
print(f'Updated {len(guides)} guides, {sum(len(g["acts"]) for g in guides)} acts, {len(nodes)} chart nodes, {len(endings)} current endings.')
