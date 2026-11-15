#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Convert the revolution v2 writer drafts into Content/revolution_v2.json."""
from __future__ import annotations

import json
import re
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "tools" / "NovelWorkFlow_Eleanor" / "Novel_Output"
OUT = ROOT / "Content" / "revolution_v2.json"

SPEAKERS = {
    "旁白": "Narrator", "店主": "Player", "埃莉诺": "Eleanor",
    "革命军军需官": "RevQuartermaster", "革命军战士": "RevFighter",
    "革命军游击队": "RevGuerrilla",
}
SCENE_META = [
    ("v2_r1c1", 1, 280, "eleanor_rev", "Eleanor", "SELL"),
    ("v2_r1c2", 1, 281, "eleanor_rev", "Eleanor", "BUY"),
    ("v2_r2c1", 2, 282, "eleanor_rev", "Eleanor", "BUY"),
    ("v2_r2c2", 2, 283, "EleanorVestPortrait", "Eleanor", "DIALOGUE"),
    ("v2_r2c3", 2, 284, "EleanorVestPortrait", "Eleanor", "DIALOGUE"),
    ("v2_r3c1", 3, 285, "EleanorVestPortrait", "Eleanor", "DIALOGUE"),
    ("v2_r3c2", 3, 286, "EleanorVestPortrait", "Eleanor", "DIALOGUE"),
    ("v2_r4c1", 4, 287, "maleLower1", "革命军军需官", "SELL"),
    ("v2_r4c2", 4, 288, "maleBM5", "革命军战士", "SELL"),
    ("v2_r4c3", 4, 289, "maleLower1", "革命军游击队", "SELL"),
    ("v2_r4c4", 4, 290, "EleanorVestPortrait", "Eleanor", "DIALOGUE"),
]
NEXT_CODE = {280: 281, 281: 282, 282: 283, 283: 284, 284: 285, 285: 286,
             286: 287, 287: 288, 288: 289, 289: 290, 290: 0}

CHOICE_RE = re.compile(r"^\[选择\|([^|]+)\|(.+)\|主线=(\d+)\]$")
OPTION_RE = re.compile(r"^\[选项\|(.*)\]$")
DIALOGUE_RE = re.compile(r"^\[([^|\]]+)\|对白\|(.*)\]$")
NARRATOR_RE = re.compile(r"^\[旁白\|(.*)\]$")
EXPR_RE = re.compile(r"^\[([^|\]]+)\|表情\|([^\]]+)\]$")
RESULT_RE = re.compile(r"^\[结果\|([^|]+)\|(.*)\]$")
MERGE_RE = re.compile(r"^\[汇合\|([^\]]+)\]$")
CHAPTER_RE = re.compile(r"^\[章\|([^\]]+)\]\s*$", re.M)


def parse_lines(text: str, allow_choices: bool = False):
    items, lines, pending = [], [], {}
    choice = None
    options = []

    def flush():
        nonlocal lines
        if lines:
            items.append({"type": "lines", "lines": lines})
            lines = []

    for raw in text.splitlines():
        s = raw.strip()
        if not s:
            continue
        m = EXPR_RE.match(s)
        if m:
            pending[m.group(1)] = m.group(2)
            continue
        m = CHAPTER_RE.match(s)
        if m:
            continue
        if allow_choices:
            m = CHOICE_RE.match(s)
            if m:
                flush()
                choice = {"type": "choice", "id": m.group(1), "context": m.group(2).strip(),
                          "mainline": int(m.group(3))}
                options = []
                continue
            m = OPTION_RE.match(s)
            if m:
                if choice is None:
                    raise ValueError("option outside choice: " + s)
                options.append(m.group(1).strip())
                continue
            if s == "[/选择]":
                if choice is None:
                    raise ValueError("closing choice without opening")
                if not options:
                    raise ValueError("choice without options")
                choice["options"] = options
                items.append(choice)
                choice = None
                options = []
                continue
        m = RESULT_RE.match(s)
        if m:
            items.append({"type": "result", "id": m.group(1), "text": m.group(2).strip()})
            continue
        m = MERGE_RE.match(s)
        if m:
            items.append({"type": "merge", "id": m.group(1)})
            continue
        m = DIALOGUE_RE.match(s)
        if m:
            spe, txt = m.group(1), m.group(2)
            line = {"speaker": SPEAKERS.get(spe, spe), "text": txt}
            if pending.get(spe):
                line["expr"] = pending.pop(spe)
            lines.append(line)
            continue
        m = NARRATOR_RE.match(s)
        if m:
            lines.append({"speaker": "Narrator", "text": m.group(1)})
            continue
        if s.startswith("[交易声明") or s.startswith("[交易后续") or s.startswith("[/交易后续"):
            continue
        if s.startswith("[通告") or s.startswith("[新闻") or s.startswith("[时间"):
            continue
        raise ValueError("unparsed line: " + s)
    if choice is not None:
        raise ValueError("unterminated choice")
    flush()
    return items


def parse_branches(text: str):
    branches = OrderedDict()
    declarations = []
    outcomes = OrderedDict()
    for m in re.finditer(r"\[选项后续\|([^|]+)\|(\d+)\]\s*(.*?)\s*\[/选项后续\]", text, re.S):
        cid, index, inner = m.group(1), int(m.group(2)), m.group(3)
        lines, result, merge = [], None, False
        for item in parse_lines(inner):
            if item["type"] == "lines":
                lines.extend(item["lines"])
            elif item["type"] == "result":
                result = item["text"]
            elif item["type"] == "merge":
                merge = True
        branches[(cid, index)] = {"lines": lines, "result": result, "merge": merge}
    for m in re.finditer(r"\[交易声明\|([^|]+)\|([^|]+)\|([^|]+)\|预算=([^|]+)\|客户=([^\]]+)\]", text):
        declarations.append({"id": m.group(1), "intent": m.group(2), "type": m.group(3),
                             "budget": m.group(4), "client": m.group(5)})
    for m in re.finditer(r"\[交易后续\|([^|]+)\|([^\]]+)\]\s*(.*?)\s*\[/交易后续\]", text, re.S):
        tid, outcome, inner = m.group(1), m.group(2).strip(), m.group(3)
        lines = []
        for item in parse_lines(inner):
            if item["type"] == "lines":
                lines.extend(item["lines"])
        outcomes.setdefault(tid, OrderedDict())[outcome] = lines
    return branches, declarations, outcomes


def chapter_bodies(text: str):
    matches = list(CHAPTER_RE.finditer(text))
    for i, m in enumerate(matches):
        yield m.group(1).strip(), text[m.end():matches[i + 1].start() if i + 1 < len(matches) else len(text)]


def block(block_id, lines):
    return block_id, {"lines": [
        {"speaker": l["speaker"], "text": l["text"], "en": l["text"], **({"expr": l["expr"]} if l.get("expr") else {})}
        for l in lines
    ]}


def merge_existing_english(scenes):
    if not OUT.exists():
        return
    old = json.loads(OUT.read_text(encoding="utf-8"))
    old_scenes = {s.get("id"): s for s in old.get("scenes", [])}
    for scene in scenes:
        o = old_scenes.get(scene["id"])
        if not o:
            continue
        if o.get("title_en") and o["title_en"] != o.get("title_zh"):
            scene["title_en"] = o["title_en"]
        ob = o.get("blocks", {})
        for bid, b in scene["blocks"].items():
            if bid not in ob:
                continue
            for i, line in enumerate(b["lines"]):
                ol = ob[bid].get("lines", [])
                if i < len(ol) and ol[i].get("en") and ol[i]["en"] != ol[i].get("text"):
                    line["en"] = ol[i]["en"]
        oc = {s.get("id"): s for s in o.get("segments", []) if s.get("type") == "choice"}
        for seg in scene["segments"]:
            if seg.get("type") != "choice":
                continue
            os = oc.get(seg.get("id"))
            if os:
                if os.get("prompt_en") and os["prompt_en"] != os.get("prompt"):
                    seg["prompt_en"] = os["prompt_en"]
                oo = {x.get("id"): x for x in os.get("options", [])}
                for opt in seg["options"]:
                    x = oo.get(opt.get("id"))
                    if x and x.get("label_en") and x["label_en"] != x.get("label"):
                        opt["label_en"] = x["label_en"]


def main():
    finals, branches_text = [], []
    for n in range(1, 5):
        finals.append((SRC / f"volume_revolution_v2_Chapter_{n}_Final.txt").read_text(encoding="utf-8"))
        branches_text.append((SRC / f"volume_revolution_v2_Chapter_{n}_Branches.txt").read_text(encoding="utf-8"))
    scenes = []
    meta_index = 0
    for act, (final, btext) in enumerate(zip(finals, branches_text), 1):
        branches, declarations, outcomes = parse_branches(btext)
        act_scenes = []
        for title, body in chapter_bodies(final):
            if meta_index >= len(SCENE_META):
                raise ValueError("too many chapters")
            sid, meta_act, code, portrait, client, intent = SCENE_META[meta_index]
            if meta_act != act:
                raise ValueError(f"act mismatch at {sid}")
            items = parse_lines(body, allow_choices=True)
            blocks, segments = OrderedDict(), []
            buf, seg_no = [], 0

            def flush_seg():
                nonlocal buf, seg_no
                if not buf:
                    return
                bid = f"{sid}_s{seg_no}"
                seg_no += 1
                blocks.update([block(bid, buf)])
                segments.append({"type": "lines", "block": bid})
                buf = []

            for item in items:
                if item["type"] == "lines":
                    buf.extend(item["lines"])
                elif item["type"] == "choice":
                    flush_seg()
                    opts = []
                    for idx, label in enumerate(item["options"], 1):
                        opt = {"id": f"{item['id']}_o{idx}", "label": label, "mainline": idx == item["mainline"]}
                        if idx != item["mainline"]:
                            b = branches.get((item["id"], idx))
                            if b is None:
                                raise ValueError(f"missing branch {sid} {item['id']} {idx}")
                            bid = f"{sid}_{item['id'].lower()}_o{idx}"
                            blocks.update([block(bid, b["lines"])])
                            opt["block"] = bid
                            if b["result"]:
                                opt["result"] = b["result"]
                            if b["merge"]:
                                opt["merge"] = True
                        opts.append(opt)
                    if not (1 <= item["mainline"] <= len(opts)):
                        raise ValueError(f"bad mainline {sid}")
                    segments.append({"type": "choice", "id": item["id"], "prompt": item["context"],
                                     "mainline": item["mainline"], "options": opts})
                elif item["type"] == "result":
                    segments.append({"type": "result", "text": item["text"]})
            flush_seg()
            scene = {"id": sid, "act": act, "code": code, "title_zh": title, "title_en": title,
                     "client": client, "portrait": portrait, "intent": intent,
                     "next_code": NEXT_CODE[code], "segments": segments, "blocks": blocks}
            act_scenes.append(scene)
            scenes.append(scene)
            meta_index += 1

        trade_scenes = [s for s in act_scenes if s["intent"] in ("SELL", "BUY")]
        if len(trade_scenes) != len(declarations):
            raise ValueError(f"act {act}: {len(trade_scenes)} trade scenes vs {len(declarations)} declarations")
        for scene, decl in zip(trade_scenes, declarations):
            trade = dict(decl)
            trade["outcomes"] = OrderedDict()
            for outcome, lines in outcomes.get(decl["id"], OrderedDict()).items():
                bid = f"{scene['id']}_trade_{len(trade['outcomes']) + 1}"
                scene["blocks"].update([block(bid, lines)])
                trade["outcomes"][outcome] = bid
            if not trade["outcomes"]:
                raise ValueError(f"no outcomes for {scene['id']}")
            scene["trades"] = [trade]
    if meta_index != len(SCENE_META):
        raise ValueError(f"scene count mismatch {meta_index}")
    merge_existing_english(scenes)
    OUT.write_text(json.dumps({"version": 1,
                               "source": "Novel_Output/volume_revolution_v2_Chapter_1..4_Final.txt",
                               "scenes": scenes}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    total_blocks = sum(len(s["blocks"]) for s in scenes)
    total_lines = sum(len(b["lines"]) for s in scenes for b in s["blocks"].values())
    total_choices = sum(1 for s in scenes for x in s["segments"] if x["type"] == "choice")
    print(f"Wrote {OUT}")
    print(f"scenes={len(scenes)} blocks={total_blocks} lines={total_lines} choices={total_choices}")


if __name__ == "__main__":
    main()
