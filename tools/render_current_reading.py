#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Render a current-text reading edition from Content JSON + runtime chapter mapping.

输出 docs/CURRENT_TEXT_READING.md。只读 Content/src，不改游戏内容。
每个块的文字来自 Content/*.json（当前真源）；共同线按代码创建的运行章节分组，
路线 v2 按数据驱动的 segments 顺序渲染；旧档路线只给块索引。

用法：python tools/render_current_reading.py
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / "docs" / "CURRENT_TEXT_READING.md"


def current_mod_version() -> str:
    """Read the author-facing mod version so this header cannot go stale again."""
    try:
        manifest = (ROOT / "manifest.xml").read_text(encoding="utf-8")
        match = re.search(r"<ModVersion>(.*?)</ModVersion>", manifest)
        return match.group(1).strip() if match else "unknown"
    except OSError:
        return "unknown"

SPEAKER = {
    "narrator": "旁白", "Narrator": "旁白", "player": "玩家", "Player": "玩家",
    "eleanor": "埃莉诺", "Eleanor": "埃莉诺", "lydia": "莉迪亚", "Lydia": "莉迪亚",
    "sophia": "索菲娅", "Sophia": "索菲娅", "mother": "母亲", "Mother": "母亲",
    "uncle": "假叔叔", "Uncle": "假叔叔", "thug": "黑市打手", "Thug": "黑市打手",
    "officer": "治安部警官", "Officer": "治安部警官", "broker": "黑市掮客",
    "Broker": "黑市掮客", "news": "新闻", "News": "新闻",
    "security_notice": "治安部通知", "SecurityNotice": "治安部通知",
    "upperbuyer": "上层区客人", "UpperBuyer": "上层区客人",
    "RevQuartermaster": "革命军军需官", "RevFighter": "革命军战士",
    "RevGuerrilla": "革命军游击队", "Buyer": "买家", "Resident": "抢水的人",
}

DEAD_BLOCKS = {
    "story_v06.json": ["c07.b101", "c07.b106", "c16.b001", "c16.b002", "c16.b003",
                       "c16.b004", "c16.b005", "c17.b004", "c17.b005", "c19.b002",
                       "c19.b003", "c19.b004", "c19.b005", "c20.b002", "c20.b003", "ep.b006"],
    "dual_romance_v04.json": ["common09_add_added", "common09_add_after",
                              "common09_add_high_price", "common10_add_family"],
    "maid_v05.json": ["exit_b002", "m12_b005", "m20_b010", "m20_b011"],
}


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def clean(text: str) -> str:
    return re.sub(r"<i>(.*?)</i>", r"（\1）", text).replace("<i>", "（").replace("</i>", "）")


def lines_of(block, sec_name=None):
    out = []
    for line in block.get("lines", []):
        raw = line.get("speaker", "")
        sp = sec_name if (raw == "SEC" and sec_name) else SPEAKER.get(raw, raw)
        out.append(f"{sp}：{clean(line.get('text', ''))}")
    return out


def block_lines(blocks, bid, indent="  ", sec_name=None):
    b = blocks.get(bid)
    if not b:
        return [indent + f"【缺失块 {bid}】"]
    return [indent + x for x in lines_of(b, sec_name)]


def render_block(out, blocks, bid, label="", bullet=True, sec_name=None):
    if bullet:
        out.append(f"- `{bid}`" + (f"（{label}）" if label else ""))
    else:
        out.append(f"`{bid}`" + (f"（{label}）" if label else ""))
    out.extend(block_lines(blocks, bid, sec_name=sec_name))
    out.append("")


def content_blocks_from_chapters(data):
    blocks = {}
    for ch in data.get("chapters", []):
        for b in ch.get("blocks", []):
            blocks[b["id"]] = b
    return blocks


def content_blocks_from_scenes(data):
    blocks = {}
    for sc in data.get("scenes", []):
        bs = sc.get("blocks")
        if isinstance(bs, dict):
            for bid, b in bs.items():
                blocks[bid] = b
        elif isinstance(bs, list):
            for b in bs:
                blocks[b["id"]] = b
    return blocks


def block_sort_key(bid):
    m = re.match(r"([a-z0-9]+)[._]?(n|b)?(\d+)(.*)", bid)
    if not m:
        return (bid, 0, "")
    kind_rank = {"n": 0, "b": 1, None: 2}.get(m.group(2), 3)
    return (kind_rank, int(m.group(3)), m.group(4) or "")


def render_common(out, story, src, ui):
    """Chapter -> ordered block ids using the constants referenced in each chapter method."""
    # method bodies
    methods = {}
    cur = None
    for line in src.splitlines():
        m = re.match(r"\s*(?:public|private|internal)\s+static\s+(?:StoreClient|Dialogue)\s+(\w+)\(", line)
        if m:
            cur = m.group(1)
            methods.setdefault(cur, [])
            continue
        if cur:
            methods[cur].extend(re.findall(r"StoryV06Content\.(B_[A-Z0-9_]+)", line))
    g = (ROOT / "src" / "StoryV06Content.g.cs").read_text(encoding="utf-8")
    const2block = dict(re.findall(r'public const string (B_[A-Z0-9_]+) = "([^"]+)";', g))
    switch = {}
    for m in re.finditer(r"case\s+(\d+):\s*return\s+(\w+)\(", src):
        switch[int(m.group(1))] = m.group(2)
    # Chapter 5 is a ternary (RescuePending ? null : CreateWarningChapter()).
    if 5 not in switch and "CreateWarningChapter" in methods:
        switch[5] = "CreateWarningChapter"
    out.append("# 共同线（按运行章节；块顺序取自章节代码，不是单周目播放顺序）\n")
    special = {
        5: ["（第5章正常路线是 CreateWarningChapter 的假叔叔警告；仅当 RescuePending 时 CreateClient(5) 返回 null，由原生保释/获释流程接管，不显示本章。）"],
        8: ["（第8章由侍从线 v2_a0c1《还差一截》承载；未走侍从线时跳过，共同线无独立块。）"],
        11: ["（第11章按 DPreludeStep 分三路：c10.n201–n205 / c10.n206–n208 / c10.n209–n214）"],
        12: ["（第12章为侍从线入口：新档 v2_a0c2 / 旧档 m11；共同线无独立章节块）"],
        16: ["（第16章由侍从线 v2_a1c4《换了地方》承载；仅侍从线安全救母路径出现，共同线无独立块。）"],
        17: ["（第17章由侍从线 v2_a1c5《日用品》承载；仅侍从线出现，共同线无独立块。）"],
    }
    for ch in sorted(set(switch) | {8, 16, 17}):
        meth = switch.get(ch)
        if meth:
            title = f"第{ch}章（{meth}）" if ch != 201 else "运行章 201 · 失败/背叛分支（CreateRouteD）"
        else:
            title = f"第{ch}章 · 侍从线分支（MaidV2Story.CreateChapter）"
        out.append(f"## {title}")
        out.append("")
        if ch in special:
            out.extend(special[ch])
            out.append("")
        # choice prompts / labels authored in ui_text for this chapter's method
        seen = set()
        for c in methods.get(meth, []):
            bid = const2block.get(c)
            if not bid or bid in seen or bid not in story:
                continue
            seen.add(bid)
            label = "分支/反应" if re.search(r"\.b\d", bid) else "主线/条件"
            render_block(out, story, bid, label)
        # choice hint: scan method body text from source
        body_lines = []
        in_method = False
        if meth:
            for line in src.splitlines():
                if re.match(r"\s*(?:public|private|internal)\s+static\s+(?:StoreClient|Dialogue)\s+" + meth + r"\(", line):
                    in_method = True
                    continue
                if in_method:
                    if re.match(r"\s*(?:public|private|internal)\s+static\s+", line):
                        break
                    body_lines.append(line)
        body = "\n".join(body_lines)
        keys = []
        for k in re.findall(r'L\.Get\("(er_(?:op55|prelude_c|prompt|choice|love_choice)[a-z0-9_]*)"', body):
            if k not in keys and k in ui:
                keys.append(k)
        if keys:
            out.append("- 本章选择/提示文案（ui_text）：")
            for k in keys:
                out.append(f"  - `{k}`：{ui[k]}")
            out.append("")
        if ch == 11:
            for extra in ("CreatePreludeEleanorChapter", "CreatePreludeBrokerChapter", "CreatePreludeBuybackChapter"):
                out.append(f"### {extra}")
                out.append("")
                for c in methods.get(extra, []):
                    bid = const2block.get(c)
                    if bid and bid in story:
                        render_block(out, story, bid, "prelude 路径")
        out.append("")
    out.append("## 第5章 · 原生保释/获释（RescuePending 路径）")
    out.append("")
    out.append("（当 RescuePending 时，CreateClient(5) 返回 null，由原生埃莉诺保释/获释来访接管；上面的第5章是正常警告路线。以下为本 MOD 在这一路径接管的文本。）")
    out.append("")
    for bid in ("c05.b007", "c05.b009", "c05.b010"):
        if bid in story:
            render_block(out, story, bid, "原生救援路径")
    out.append("## 第12章 · 侍从线入口（新档 v2_a0c2 / 旧档 m11）")
    out.append("")
    out.extend(special[12])
    out.append("")
    out.append("## 尾声 ep（RomanceConfirmed 与朋友分支）")
    out.append("")
    for bid in sorted(story):
        if bid.startswith("ep."):
            render_block(out, story, bid, "尾声")
    out.append("")


def render_love(out, data, blocks, cg_data=None):
    out.append("# 恋爱线 E1（按玩家实际顺序；CG 章节插在对应场景之后，婚礼结尾）\n")
    for sc in data["scenes"]:
        out.append(f"## {sc['id']}　{sc.get('title_zh','')}")
        out.append("")
        for b in sc.get("blocks", []):
            bid = b["id"]
            role = ("开场" if "_pre" in bid else "选择后/共通" if ("_post" in bid or "_common" in bid)
                    else "选项分支" if re.search(r"_c\d", bid) else "交易反应" if re.search(r"_t\d", bid)
                    else "条件/补充" if ("research" in bid or "handoff" in bid) else "正文")
            sec = "索菲娅" if bid.startswith("la3_s4") else "埃莉诺"
            render_block(out, blocks, bid, role, sec_name=sec)
        for ch in sc.get("choices", []) or []:
            out.append(f"- 选择 `{ch.get('id')}`：{ch.get('label_zh','')}（{ch.get('description_zh','')}）")
        out.append("")
        if cg_data:
            render_cg_after(out, "恋爱线 E1", "love", sc["id"], cg_data)


def render_dual(out, data, blocks, cg_data=None):
    out.append("# 后宫线（新档入口《窗口》、dv01–dv13、CG 章节与终章）\n")
    wanted = ["d01", *(f"dv{i:02d}" for i in range(1, 14))]
    scenes = {sc["id"]: sc for sc in data["scenes"]}
    for sid in wanted:
        sc = scenes[sid]
        out.append(f"## {sc['id']}　{sc.get('title_zh','')}")
        out.append("")
        companion = {"d01": "埃莉诺", "dv01": "埃莉诺", "dv02": "埃莉诺", "dv03": "埃莉诺",
                     "dv05": "埃莉诺", "dv06": "埃莉诺", "dv07": "莉迪亚",
                     "dv13": "莉迪亚"}.get(sc["id"])
        for b in sc.get("blocks", []):
            bid = b["id"]
            role = ("开场" if bid.endswith("_b001") else "后续/共通" if re.search(r"_b0(0[2-9]|\d\d)$", bid) else "正文")
            render_block(out, blocks, bid, role, sec_name=companion)
        for ch in sc.get("choices", []) or []:
            out.append(f"- 选择 `{ch.get('id')}`：{ch.get('label_zh','')}（{ch.get('description_zh','')}）")
        out.append("")
        if cg_data:
            render_cg_after(out, "后宫线 E4", "dual", sc["id"], cg_data)


def render_dual_post(out, data, blocks):
    out.append("# 结局后复用日常（后宫线；循环内容，不推进故事）\n")
    scenes = {sc["id"]: sc for sc in data["scenes"]}
    for sid in ("dr_daily_2", "dr_daily_3"):
        sc = scenes[sid]
        out.append(f"## {sc['id']}　{sc.get('title_zh','')}")
        out.append("")
        for b in sc.get("blocks", []):
            bid = b["id"]
            role = "开场" if bid.endswith("_b001") else "后续/共通"
            render_block(out, blocks, bid, role)
        out.append("")


def render_v2(out, data, blocks, title, story=None, extra=None, cg_data=None, route_key=None, route_name=None):
    out.append(title)
    for sc in data["scenes"]:
        out.append(f"## {sc['id']}　{sc.get('title_zh','')}（code {sc.get('code')}）")
        out.append("")
        for tr in sc.get("trades", []) or []:
            out.append(f"- 交易：{tr.get('intent')} / {tr.get('type')} / 预算 {tr.get('budget')} / 客户 {tr.get('client')}")
            for k, bid in (tr.get("outcomes") or {}).items():
                out.append(f"  - {k} → `{bid}`")
            out.append("")
        for seg in sc.get("segments", []) or []:
            t = seg.get("type")
            if t == "lines":
                render_block(out, blocks, seg["block"], "正文")
            elif t == "result":
                out.append(f"- 结果旁白：{clean(seg.get('result',''))}")
                out.append("")
            elif t == "choice":
                out.append("- 在上句选择" if seg.get("attach_to_previous") else f"- 选择：{seg.get('prompt','')}")
                for opt in seg.get("options", []):
                    main = "主线" if opt.get("mainline") else "分支"
                    out.append(f"  - [{main}] {opt.get('label','')} → `{opt.get('block')}`" if opt.get("block") else f"  - [{main}] {opt.get('label','')}")
                    if opt.get("block"):
                        out.extend(block_lines(blocks, opt["block"], "    "))
                out.append("")
        for bid in (extra or {}).get(sc.get("id"), []):
            if story and bid in story:
                render_block(out, story, bid, "交易共同反应")
        if cg_data and route_key and route_name:
            render_cg_after(out, route_name, route_key, sc["id"], cg_data)
        out.append("")


def render_endings(out, ui):
    out.append("# 结局页与尾声文本（Localization 显示文本；参考清单，不属于路线顺序）\n")
    keys = [k for k in ui if k.startswith("er_ending_")]
    for k in keys:
        out.append(f"- `{k}`：{ui[k]}")
    out.append("")


def render_legacy(out, maid, dual, rev1):
    out.append("# 旧档路线块索引（只给索引与行数；正文仍在 Content，仅在旧存档分支播放）\n")
    def group(blocks, pred, title):
        ids = sorted(b for b in blocks if pred(b))
        if not ids:
            return
        out.append(f"## {title}（{len(ids)} 块）")
        out.append("")
        for bid in ids:
            out.append(f"- `{bid}`：{len(blocks[bid].get('lines', []))} 行")
        out.append("")
    group(maid, lambda b: re.match(r"^m\d\d_", b), "侍从旧版 m11–m28")
    group(maid, lambda b: b.startswith(("world_", "wait_", "daily_", "exit_", "tragic_", "choicefix_", "legacy_", "gala_")), "侍从旧版 其他")
    group(dual, lambda b: re.match(r"^(d0[2-9]_|dr|v05_|wait_)", b)
          and not b.startswith(("dr_daily_2_", "dr_daily_3_")), "后宫旧档 d02–d09 / dr / v05 / wait")
    group(rev1, lambda b: b.startswith("rv"), "革命军旧版 R01–R14")


CG_NATIVE_EXISTING = {
    "love_a2_01", "love_a3_04", "dv08", "dv10", "dv13",
    "v2_a0c2", "v2_a4c3", "v2_a4c4", "v2_r3c1", "v2_r4c4",
}
CG_NEW_NATIVE = {"dual_final_shop", "rev_final_second_knock"}

CG_CODE = {
    "love_night_book": "恋爱线；T1Done；第54–56天；睡前",
    "love_trade_wall": "恋爱线；A2T2Done；第60–62天；营业客位",
    "love_night_supper": "恋爱线；Act1Done；第61–62天；睡前",
    "love_day_promenade": "恋爱线；A3T1Done 且未到 A3T2Done；第62天；玩家主动关店",
    "love_night_stay": "恋爱线；A3T2Done；当日为 LastImportantDay；睡前，次晨亮图",
    "love_trade_kiss": "恋爱线；A3T2Done；第64–68天；营业客位",
    "love_night_bathhouse": "恋爱线；A3T3Done；第67–68天；睡前",
    "dual_trade_dice": "后宫线；已完成 dv01、未完成 dv04；第54–56天；营业客位",
    "dual_night_bath": "后宫线；Confirmed 且路线剧情完成；第68–69天；睡前",
    "dual_trade_brooch": "后宫线；Confirmed 且路线剧情完成；第70–72天；营业客位",
    "dual_night_fireworks": "后宫线；Confirmed 且路线剧情完成；第70–71天；睡前",
    "dual_night_hotel": "后宫线；Confirmed 且路线剧情完成；第72天；睡前",
    "maid_stage_soundcheck": "侍从线；CoWork；Progress 17–19；第46–50天；关店时",
    "maid_trade_shopkeeper": "侍从线；Progress≥18；第48–50天；营业客位",
    "maid_night_steps": "侍从线；Act2Done；第51–53天；睡前",
    "maid_trade_shutters": "侍从线；Residency；第56–58天；营业客位",
    "maid_night_costume": "侍从线；Progress≥26；第58–59天；睡前",
    "maid_night_encore": "侍从线；GalaInvited；Progress==27；第61–62天；睡前",
    "rev_trade_sill": "革命军线；未远征、未完成、Date<63；Progress≥281；第49–51天；营业客位",
    "rev_ride": "革命军线；未远征、未完成、Date<63；Progress≥281；第49–51天；收店后",
    "rev_trade_wrist": "革命军线；未远征、未完成、Date<63；Progress≥283；第55–58天；营业客位",
    "rev_night_departure": "革命军线；Progress==286；第62天；睡前",
    "rev_night_jacket": "革命军线；RevComplete；第73天；睡前",
    "rev_night_barracks": "革命军线；RevComplete；第74天；睡前",
    "love_a2_01": "现有《停水》流程；第57天接图",
    "love_a3_04": "现有婚礼结局页；第69天接图",
    "dv08": "现有《搬货》后共同段；接图",
    "dv10": "现有莉迪亚约会；接图",
    "dv13": "现有《点头》；接图",
    "v2_a0c2": "现有雇用签约分支；接图",
    "v2_a4c3": "现有上层舞会；接图",
    "v2_a4c4": "现有侍从终章；接图",
    "v2_r3c1": "现有《门还没开》；接图",
    "v2_r4c4": "现有《门外》；接图",
    "dual_final_shop": "后宫终章检查；《点头》后第六天打烊",
    "rev_final_second_knock": "革命军终章检查；《门外》后第四晨，玩家开门时",
}


def _cg_trigger(text):
    for line in text.splitlines():
        if line.strip().startswith(">"):
            return line.strip()[1:].strip()
    return ""


def load_cg_chapters():
    import build_cg_chapters as cg
    route_of_prefix = {"E1": "love", "E3": "maid", "E4": "dual", "E9": "rev"}
    result = {k: [] for k in route_of_prefix.values()}
    for path in sorted((ROOT / "Content" / "CG章节").glob("E*.md")):
        route = route_of_prefix[path.name[:2]]
        source = path.read_text(encoding="utf-8-sig")
        heads = list(cg.HEAD.finditer(source))
        for index, head in enumerate(heads):
            end = heads[index + 1].start() if index + 1 < len(heads) else len(source)
            text = source[head.end():end].strip()
            scene, title = head.group(2), head.group(1)
            chapter = cg.parse(scene, title, text)
            chapter["trigger"] = _cg_trigger(text)
            chapter["native"] = scene in cg.NATIVE
            chapter["existing"] = scene in CG_NATIVE_EXISTING
            result[route].append(chapter)
    return result


def _cg_slot(scene):
    native = {
        "love_a2_01": "营业中（停水日末客后）",
        "love_a3_04": "婚礼结局页",
        "dv08": "营业中（《搬货》后）",
        "dv10": "店休约会",
        "dv13": "营业中（《点头》）",
        "v2_a0c2": "营业中签约",
        "v2_a4c3": "闭店赴会",
        "v2_a4c4": "营业中终章",
        "v2_r3c1": "开店前",
        "v2_r4c4": "开门前",
    }
    if scene in native:
        return native[scene]
    if "_trade_" in scene:
        return "营业中客位（插在普通散客之间）"
    if scene == "maid_stage_soundcheck":
        return "关店时（收工后）"
    if scene == "love_day_promenade":
        return "营业结束关窗时"
    if scene == "love_night_stay":
        return "睡前；次晨开门前亮图"
    if scene == "rev_ride":
        return "收店后"
    if scene == "dual_final_shop":
        return "打烊后按下睡觉时，外出返店与留店都可触发"
    if scene == "rev_final_second_knock":
        return "开门前"
    if "_night_" in scene:
        return "睡前（收店结算后）"
    return "按触发条件"


def _cg_line(raw):
    who, text = raw.split("　", 1)
    return f"{who}：{text}"


def _cg_meta(out, chapter, native):
    scene = chapter["id"]
    kind = "现有场景接图定稿" if native else "本次新增独立来访"
    out.append(f"- 触发：{chapter['trigger'] or '按代码条件'} ｜ 代码：{CG_CODE.get(scene, '按路线流程')} ｜ 出现：{_cg_slot(scene)} ｜ 类型：{kind}")
    out.append("")


def _cg_blocks(out, chapter, native):
    scene = chapter["id"]
    if chapter["intro"]:
        out.append(f"- `{scene}.s0`（{'本次接图开场' if native else 'CG开场'}）")
        for line in chapter["intro"]:
            out.append("  " + _cg_line(line))
        out.append("")
    for key, label in (("sold", "成交"), ("unsold", "未成交")):
        if chapter[key]:
            out.append(f"- `{scene}.t_{key}`（{label}反应）")
            out.extend("  " + _cg_line(line) for line in chapter[key])
            out.append("")
    if chapter["caption"]:
        out.append(f"- `{scene}.cap`（图上文案）")
        for line in chapter["caption"].splitlines():
            out.append(f"  「{line}」")
        out.append("")


def render_cg_chapter(out, route_name, chapter):
    scene = chapter["id"]
    out.append(f"## {scene}　{chapter['title']}")
    out.append("")
    _cg_meta(out, chapter, False)
    _cg_blocks(out, chapter, False)


def render_cg_native(out, route_name, chapter):
    scene = chapter["id"]
    out.append(f"### {chapter['title']} · 本次接图定稿")
    out.append("")
    _cg_meta(out, chapter, True)
    _cg_blocks(out, chapter, True)


CG_AFTER = {
    "love": {
        "love_a1_03": ["love_night_book"],
        "love_a2_02": ["love_trade_wall"],
        "love_a2_03": ["love_night_supper"],
        "love_a3_01": ["love_day_promenade"],
        "love_a3_02": ["love_night_stay", "love_trade_kiss"],
        "love_a3_03": ["love_night_bathhouse"],
    },
    "dual": {
        "dv01": ["dual_trade_dice"],
        "dv13": ["dual_night_bath", "dual_trade_brooch", "dual_night_fireworks",
                  "dual_night_hotel", "dual_final_shop"],
    },
    "maid": {
        "v2_a1c5": ["maid_stage_soundcheck", "maid_trade_shopkeeper"],
        "v2_a2c2": ["maid_night_steps"],
        "v2_a4c1": ["maid_trade_shutters"],
        "v2_a4c2": ["maid_night_costume"],
        "v2_a4c3": ["maid_night_encore"],
    },
    "rev": {
        "v2_r1c2": ["rev_trade_sill", "rev_ride"],
        "v2_r2c3": ["rev_trade_wrist"],
        "v2_r3c2": ["rev_night_departure"],
        "v2_r4c4": ["rev_night_jacket", "rev_night_barracks", "rev_final_second_knock"],
    },
}
CG_NATIVE_BY_SCENE = {
    "love_a2_01", "love_a3_04", "dv08", "dv10", "dv13",
    "v2_a0c2", "v2_a4c3", "v2_a4c4", "v2_r3c1", "v2_r4c4",
}


def _cg_lookup(cg_data, route_key):
    return {chapter["id"]: chapter for chapter in cg_data[route_key]}


def render_cg_after(out, route_name, route_key, scene_id, cg_data):
    lookup = _cg_lookup(cg_data, route_key)
    if scene_id in CG_NATIVE_BY_SCENE:
        render_cg_native(out, route_name, lookup[scene_id])
    for chapter_id in CG_AFTER.get(route_key, {}).get(scene_id, []):
        render_cg_chapter(out, route_name, lookup[chapter_id])


def main():
    story = content_blocks_from_chapters(load(ROOT / "Content" / "story_v06.json"))
    love_data = None
    love_blocks = {}
    for name in ("love_romance_v01.json", "love_romance_v02.json", "love_romance_v03.json"):
        d = load(ROOT / "Content" / name)
        if love_data is None:
            love_data = {"scenes": []}
        love_data["scenes"].extend(d["scenes"])
        love_blocks.update(content_blocks_from_scenes(d))
    dual_data = load(ROOT / "Content" / "dual_romance_v04.json")
    dual_blocks = content_blocks_from_scenes(dual_data)
    maid = load(ROOT / "Content" / "maid_v05.json")
    maid_blocks = {b["id"]: b for b in maid.get("blocks", [])}
    maid2_data = load(ROOT / "Content" / "maid_v2.json")
    maid2_blocks = content_blocks_from_scenes(maid2_data)
    rev1 = load(ROOT / "Content" / "revolution_v1.json")
    rev1_blocks = {b["id"]: b for b in rev1.get("blocks", [])}
    rev2_data = load(ROOT / "Content" / "revolution_v2.json")
    rev2_blocks = content_blocks_from_scenes(rev2_data)
    ui = load(ROOT / "tools" / "ui_text_zh.json")
    for ui_path in sorted((ROOT / "Content").glob("*_ui.json")):
        raw = load(ui_path)
        for k, v in raw.items():
            if k not in ui and isinstance(v, list) and v:
                ui[k] = v[0]
    src = (ROOT / "src" / "EleanorStory.cs").read_text(encoding="utf-8")
    cg_data = load_cg_chapters()

    out = [
        f"# 当前正文阅读版（按 v{current_mod_version()} Content 生成）",
        "",
        "生成：`python tools/render_current_reading.py`。文本来自 `Content/*.json` 与 `Content/CG章节`（当前真源），不是旧阅读稿。",
        "用途：文风/节奏/事实复核的工作底稿；不是运行时回放。",
        "",
        "- 动作统一用全角括号（）标出；游戏内也使用括号，不再使用斜体。",
        "- 共同线按 `src/EleanorStory.cs` 的章节方法分组；块顺序取自代码引用顺序，块内保留 JSON 行序。",
        "- 恋爱/后宫/v2 路线按玩家实际触发顺序排列；CG 章节插在对应场景之后，每条路线以最终场景/结局收束。",
        "- CG 章节来自 `Content/CG章节`、`src/ChapterScripts.g.cs` 与 `src/ChapterVisits.cs`；现有场景接图以“本次接图定稿”列在同场之后。",
        "- CG 章节只在中文界面启用；交易章插在营业客位，夜邀章在睡前，晨图在次日开门前。后宫结局后的复用日常单列。",
        "- 旧档路线只给块索引；`附录`列出确认不会被任何代码路径播放的块。",
        "- 选择、交易反应、条件块都在正文中标注；一次周目实际只走其中一条。",
        "",
    ]
    render_common(out, story, src, ui)
    render_love(out, love_data, love_blocks, cg_data=cg_data)
    render_dual(out, dual_data, dual_blocks, cg_data=cg_data)
    render_dual_post(out, dual_data, dual_blocks)
    render_v2(out, maid2_data, maid2_blocks, "# 侍从线 v2（新档；按玩家实际顺序，CG 插在对应场景之后）\n",
              story=story, extra={"v2_a0c1": ["c07.b004", "c07.b005", "c07.b006", "c07.b007"]},
              cg_data=cg_data, route_key="maid", route_name="侍从线 E3")
    render_v2(out, rev2_data, rev2_blocks, "# 革命军线 v2（新档；按玩家实际顺序，CG 插在对应场景之后）\n",
              cg_data=cg_data, route_key="rev", route_name="革命军线 E9")
    render_endings(out, ui)
    render_legacy(out, maid_blocks, dual_blocks, rev1_blocks)
    out.append("# 附录：确认不会被代码播放的块\n")
    for file, ids in DEAD_BLOCKS.items():
        out.append(f"## {file}")
        out.append("")
        for bid in ids:
            out.append(f"- `{bid}`")
        out.append("")
    out.append("## 口径与限制\n")
    out.append("- 本文件不模拟周目；标记为“分支/反应/条件”的块不会在一次周目中全部出现。")
    out.append("- 共同线的块顺序是代码引用顺序，不等于运行时的逐句顺序；实际拼接以 `src/EleanorStory.cs` 为准。")
    out.append("- CG 事件按路线、日期和前置触发。独立事件从窗口对白接图，交易按实际成交结果收尾。《一起醒来》在次晨亮图。旧章的接图位置排在同场正文之后。")
    out.append("- 旧档分支（侍从旧版、后宫 d02–d09 与旧 dr 排期、革命军 R01–R14）在索引列出。")
    out.append("- 重跑本脚本会整体重写本文件；修改 Content 后请重新生成，不要手改本文件。")
    out.append("")
    DOC.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"Wrote {DOC} ({len(out)} lines)")


if __name__ == "__main__":
    main()
