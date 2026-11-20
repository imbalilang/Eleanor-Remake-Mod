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
    "story_v06.json": ["c17.b004", "c17.b005", "c19.b002", "c19.b003", "c19.b004",
                       "c19.b005", "c20.b002", "c20.b003", "ep.b006"],
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
    out.append("# 共同线（按运行章节；块顺序取自章节代码，不是单周目播放顺序）\n")
    special = {
        5: ["（第5章由原生保释/获释来访承载：c05.b007、b009、b010；主剧情在 c05.n001 求食段）"],
        11: ["（第11章按 DPreludeStep 分三路：c10.n201–n205 / c10.n206–n208 / c10.n209–n214）"],
        12: ["（第12章为侍从线入口：新档 v2_a0c2 / 旧档 m11；共同线无独立章节块）"],
    }
    for ch in sorted(switch):
        meth = switch[ch]
        title = f"第{ch}章（{meth}）" if ch != 201 else "运行章 201 · 失败/背叛分支（CreateRouteD）"
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
    out.append("## 第5章 · 原生保释/获释（不经过 CreateClient 章节方法）")
    out.append("")
    out.extend(special[5])
    out.append("")
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


def render_love(out, data, blocks):
    out.append("# 恋爱线 E1（按场景 JSON 分组；顺序为块表顺序，实际连接见代码）\n")
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


def render_dual(out, data, blocks):
    out.append("# 后宫线 v2（dv01–dv13；旧档 d0x/dr 只在旧存档运行）\n")
    wanted = [f"dv{i:02d}" for i in range(1, 14)]
    for sc in data["scenes"]:
        if sc["id"] not in wanted:
            continue
        out.append(f"## {sc['id']}　{sc.get('title_zh','')}")
        out.append("")
        companion = {"dv01": "埃莉诺", "dv02": "埃莉诺", "dv03": "埃莉诺",
                     "dv05": "埃莉诺", "dv06": "埃莉诺", "dv07": "莉迪亚",
                     "dv13": "莉迪亚"}.get(sc["id"])
        for b in sc.get("blocks", []):
            bid = b["id"]
            role = ("开场" if bid.endswith("_b001") else "后续/共通" if re.search(r"_b0(0[2-9]|\d\d)$", bid) else "正文")
            render_block(out, blocks, bid, role, sec_name=companion)
        for ch in sc.get("choices", []) or []:
            out.append(f"- 选择 `{ch.get('id')}`：{ch.get('label_zh','')}（{ch.get('description_zh','')}）")
        out.append("")


def render_v2(out, data, blocks, title):
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
                out.append(f"- 选择：{seg.get('prompt','')}")
                for opt in seg.get("options", []):
                    main = "主线" if opt.get("mainline") else "分支"
                    out.append(f"  - [{main}] {opt.get('label','')} → `{opt.get('block')}`" if opt.get("block") else f"  - [{main}] {opt.get('label','')}")
                    if opt.get("block"):
                        out.extend(block_lines(blocks, opt["block"], "    "))
                out.append("")
        out.append("")


def render_endings(out, ui):
    out.append("# 结局页与尾声（Localization 里的显示文本）\n")
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
    group(dual, lambda b: re.match(r"^(d0|dr|v05_|wait_)", b), "后宫旧档 d0x / dr / v05 / wait")
    group(rev1, lambda b: b.startswith("rv"), "革命军旧版 R01–R14")


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

    out = [
        "# 当前正文阅读版（按 v1.23 Content 生成）",
        "",
        "生成：`python tools/render_current_reading.py`。文本来自 `Content/*.json`（当前真源），不是旧阅读稿。",
        "用途：文风/节奏/事实复核的工作底稿；不是运行时回放。",
        "",
        "- 共同线按 `src/EleanorStory.cs` 的章节方法分组；块顺序取自代码引用顺序，块内保留 JSON 行序。",
        "- 恋爱/后宫按场景 JSON 分组；v2 路线按生成内容里的 segments/trades 顺序渲染。",
        "- 旧档路线只给块索引；`附录`列出确认不会被任何代码路径播放的块。",
        "- 选择、交易反应、条件块都在正文中标注；一次周目实际只走其中一条。",
        "",
    ]
    render_common(out, story, src, ui)
    render_love(out, love_data, love_blocks)
    render_dual(out, dual_data, dual_blocks)
    render_v2(out, maid2_data, maid2_blocks, "# 侍从线 v2（新档；按场景 segments 顺序）\n")
    render_v2(out, rev2_data, rev2_blocks, "# 革命军线 v2（新档；按场景 segments 顺序）\n")
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
    out.append("- 旧档分支（侍从旧版、后宫 d0x/dr、革命军 R01–R14）不在正文区分，只在索引列出。")
    out.append("- 重跑本脚本会整体重写本文件；修改 Content 后请重新生成，不要手改本文件。")
    out.append("")
    DOC.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"Wrote {DOC} ({len(out)} lines)")


if __name__ == "__main__":
    main()
