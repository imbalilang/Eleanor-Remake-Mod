#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Convert the maid v2 writer drafts into Content/maid_v2.json.

Source of truth: tools/NovelWorkFlow_Eleanor/Novel_Output/volume_maid_v2_Chapter_{0..4}_Final.txt
plus the matching _Branches.txt.  This is a one-time design-to-content converter;
runtime builds read Content/maid_v2.json through tools/build_maid_v2_content.py.
"""
from __future__ import annotations

import json
import re
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "tools" / "NovelWorkFlow_Eleanor" / "Novel_Output"
OUT = ROOT / "Content" / "maid_v2.json"

SPEAKERS = {
    "旁白": "Narrator",
    "店主": "Player",
    "埃莉诺": "Eleanor",
    "索菲娅": "Sophia",
    "黑市打手": "Thug",
}

# id, act, chapter code, portrait, client, intent
SCENE_META = [
    ("v2_a0c1", 0, 8, "eleanor_scav", "Eleanor", "SELL"),
    ("v2_a0c2", 0, 12, "eleanor_scav", "Eleanor", "DIALOGUE"),
    ("v2_a1c1", 1, 13, "eleanor_scav", "Eleanor", "DIALOGUE"),
    ("v2_a1c2", 1, 14, "maleBM5", "Thug", "DIALOGUE"),
    ("v2_a1c3", 1, 15, "eleanor_scav", "Eleanor", "DIALOGUE"),
    ("v2_a1c4", 1, 16, "eleanor_scav", "Eleanor", "DIALOGUE"),
    ("v2_a1c5", 1, 17, "er_portrait_mother_female_beauty", "Sophia", "BUY"),
    ("v2_a2c1", 2, 18, "eleanor_scav", "Eleanor", "DIALOGUE"),
    ("v2_a2c2", 2, 19, "eleanor1", "Eleanor", "DIALOGUE"),
    ("v2_a3c1", 3, 20, "eleanor1", "Eleanor", "DIALOGUE"),
    ("v2_a3c2", 3, 21, "er_portrait_eleanor_gala_female_beauty", "Eleanor", "DIALOGUE"),
    ("v2_a3c3", 3, 22, "er_portrait_eleanor_gala_female_beauty", "Eleanor", "DIALOGUE"),
    ("v2_a4c1", 4, 25, "er_portrait_eleanor_gala_female_beauty", "Eleanor", "DIALOGUE"),
    ("v2_a4c2", 4, 26, "er_portrait_eleanor_gala_female_beauty", "Eleanor", "DIALOGUE"),
    ("v2_a4c3", 4, 27, "er_portrait_eleanor_gala_female_beauty", "Eleanor", "DIALOGUE"),
    ("v2_a4c4", 4, 28, "er_portrait_eleanor_maid_female_beauty", "Eleanor", "DIALOGUE"),
]
NEXT_CODE = {
    "v2_a0c1": 9, "v2_a0c2": 13,
    "v2_a1c1": 14, "v2_a1c2": 15, "v2_a1c3": 16, "v2_a1c4": 17, "v2_a1c5": 18,
    "v2_a2c1": 19, "v2_a2c2": 20,
    "v2_a3c1": 21, "v2_a3c2": 22, "v2_a3c3": 0,  # 0 = complete core, Act 4 is return-phase
    "v2_a4c1": 26, "v2_a4c2": 27, "v2_a4c3": 28, "v2_a4c4": 0,
}
TRADE_SCENE = {0: "v2_a0c1", 1: "v2_a1c5"}
WHOLESALE = {"v2_a0c1": 30}
BUDGET = {"v2_a1c5": 220}

CHOICE_RE = re.compile(r"^\[选择\|([^|]+)\|(.+)\|主线=(\d+)\]$")
OPTION_RE = re.compile(r"^\[选项\|(.*)\]$")
DIALOGUE_RE = re.compile(r"^\[([^|\]]+)\|对白\|(.*)\]$")
NARRATOR_RE = re.compile(r"^\[旁白\|(.*)\]$")
EXPR_RE = re.compile(r"^\[([^|\]]+)\|表情\|([^\]]+)\]$")
RESULT_RE = re.compile(r"^\[结果\|([^|]+)\|(.*)\]$")
MERGE_RE = re.compile(r"^\[汇合\|([^\]]+)\]$")
CHAPTER_RE = re.compile(r"^\[章\|([^\]]+)\]\s*$", re.M)


def parse_lines(text: str, allow_choices: bool = False):
    """Parse tagged writer text into items.  Choices are only accepted in chapters."""
    items = []
    lines = []
    pending_expr = {}
    choice = None
    options = []

    def flush_lines():
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
            spe, key = m.group(1), m.group(2)
            pending_expr[spe] = key
            continue
        m = CHAPTER_RE.match(s)
        if m:
            continue
        if allow_choices:
            m = CHOICE_RE.match(s)
            if m:
                flush_lines()
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
                    raise ValueError("choice without options: " + choice["id"])
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
            speaker = SPEAKERS.get(spe, spe)
            line = {"speaker": speaker, "text": txt}
            if pending_expr.get(spe):
                line["expr"] = pending_expr.pop(spe)
            lines.append(line)
            continue
        m = NARRATOR_RE.match(s)
        if m:
            lines.append({"speaker": "Narrator", "text": m.group(1)})
            continue
        # Ignore declarations/section markers here; they are parsed separately.
        if s.startswith("[交易声明") or s.startswith("[交易后续") or s.startswith("[/交易后续"):
            continue
        if s.startswith("[通告") or s.startswith("[新闻") or s.startswith("[时间"):
            continue
        raise ValueError("unparsed line: " + s)
    if choice is not None:
        raise ValueError("unterminated choice: " + choice["id"])
    flush_lines()
    return items


def parse_branches(text: str):
    """Return (choice_branches, trade_declarations, trade_outcomes)."""
    choice_branches = OrderedDict()
    declarations = []
    trade_outcomes = OrderedDict()

    for m in re.finditer(r"\[选项后续\|([^|]+)\|(\d+)\]\s*(.*?)\s*\[/选项后续\]", text, re.S):
        cid, index, inner = m.group(1), int(m.group(2)), m.group(3)
        lines = []
        result = None
        merge = False
        for item in parse_lines(inner, allow_choices=False):
            if item["type"] == "lines":
                lines.extend(item["lines"])
            elif item["type"] == "result":
                result = item["text"]
            elif item["type"] == "merge":
                merge = True
        choice_branches[(cid, index)] = {"lines": lines, "result": result, "merge": merge}

    for m in re.finditer(r"\[交易声明\|([^|]+)\|([^|]+)\|([^|]+)\|预算=([^|]+)\|客户=([^\]]+)\]", text):
        declarations.append({"id": m.group(1), "intent": m.group(2), "type": m.group(3),
                             "budget": m.group(4), "client": m.group(5)})

    for m in re.finditer(r"\[交易后续\|([^|]+)\|([^\]]+)\]\s*(.*?)\s*\[/交易后续\]", text, re.S):
        tid, outcome, inner = m.group(1), m.group(2).strip(), m.group(3)
        lines = []
        for item in parse_lines(inner, allow_choices=False):
            if item["type"] == "lines":
                lines.extend(item["lines"])
        trade_outcomes.setdefault(tid, OrderedDict())[outcome] = lines
    return choice_branches, declarations, trade_outcomes


def chapter_bodies(text: str):
    matches = list(CHAPTER_RE.finditer(text))
    for i, m in enumerate(matches):
        start = m.end()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        yield m.group(1).strip(), text[start:end]


def block(block_id: str, lines):
    return block_id, {"lines": [
        {"speaker": l["speaker"], "text": l["text"], "en": l["text"], **({"expr": l["expr"]} if l.get("expr") else {})}
        for l in lines
    ]}


def merge_existing_english(scenes):
    """Keep previous en text when the writer output is reconverted."""
    if not OUT.exists():
        return
    old = json.loads(OUT.read_text(encoding="utf-8"))
    old_scenes = {s.get("id"): s for s in old.get("scenes", [])}
    for scene in scenes:
        old_scene = old_scenes.get(scene["id"])
        if not old_scene:
            continue
        if old_scene.get("title_en") and old_scene["title_en"] != old_scene.get("title_zh"):
            scene["title_en"] = old_scene["title_en"]
        old_blocks = old_scene.get("blocks", {})
        for block_id, block in scene["blocks"].items():
            old_block = old_blocks.get(block_id)
            if not old_block:
                continue
            for i, line in enumerate(block["lines"]):
                old_lines = old_block.get("lines", [])
                if i < len(old_lines) and old_lines[i].get("en") and old_lines[i]["en"] != old_lines[i].get("text"):
                    line["en"] = old_lines[i]["en"]
        old_choices = {}
        for seg in old_scene.get("segments", []):
            if seg.get("type") == "choice":
                old_choices[seg.get("id")] = seg
        for seg in scene["segments"]:
            if seg.get("type") != "choice":
                continue
            old_seg = old_choices.get(seg.get("id"))
            if not old_seg:
                continue
            if old_seg.get("prompt_en") and old_seg["prompt_en"] != old_seg.get("prompt"):
                seg["prompt_en"] = old_seg["prompt_en"]
            old_opts = {o.get("id"): o for o in old_seg.get("options", [])}
            for opt in seg["options"]:
                old_opt = old_opts.get(opt.get("id"))
                if old_opt and old_opt.get("label_en") and old_opt["label_en"] != old_opt.get("label"):
                    opt["label_en"] = old_opt["label_en"]


def main():
    final_texts = []
    branch_texts = []
    for n in range(5):
        final_texts.append((SRC / f"volume_maid_v2_Chapter_{n}_Final.txt").read_text(encoding="utf-8"))
        branch_texts.append((SRC / f"volume_maid_v2_Chapter_{n}_Branches.txt").read_text(encoding="utf-8"))

    scenes = []
    meta_index = 0
    for act, (final, branch_text) in enumerate(zip(final_texts, branch_texts)):
        branches, declarations, outcomes = parse_branches(branch_text)
        chapters = list(chapter_bodies(final))
        for title, body in chapters:
            if meta_index >= len(SCENE_META):
                raise ValueError("more chapters than scene metadata")
            scene_id, meta_act, code, portrait, client, intent = SCENE_META[meta_index]
            if meta_act != act:
                raise ValueError(f"act mismatch at {scene_id}: {meta_act} != {act}")
            items = parse_lines(body, allow_choices=True)
            blocks = OrderedDict()
            segments = []
            lines_buffer = []
            seg_no = 0
            choice_no = 0

            def flush_segment():
                nonlocal lines_buffer, seg_no
                if not lines_buffer:
                    return
                block_id = f"{scene_id}_s{seg_no}"
                seg_no += 1
                blocks.update([block(block_id, lines_buffer)])
                segments.append({"type": "lines", "block": block_id})
                lines_buffer = []

            for item in items:
                if item["type"] == "lines":
                    lines_buffer.extend(item["lines"])
                    continue
                if item["type"] == "choice":
                    flush_segment()
                    choice_no += 1
                    cid = item["id"]
                    opts = []
                    for idx, label in enumerate(item["options"], 1):
                        option = {"id": f"{cid}_o{idx}", "label": label, "mainline": idx == item["mainline"]}
                        if idx != item["mainline"]:
                            b = branches.get((cid, idx))
                            if b is None:
                                raise ValueError(f"missing branch {scene_id} {cid} option {idx}")
                            block_id = f"{scene_id}_{cid.lower()}_o{idx}"
                            blocks.update([block(block_id, b["lines"])])
                            option["block"] = block_id
                            if b["result"]:
                                option["result"] = b["result"]
                            if b["merge"]:
                                option["merge"] = True
                        opts.append(option)
                    if item["mainline"] < 1 or item["mainline"] > len(opts):
                        raise ValueError(f"bad mainline {scene_id} {cid}")
                    segments.append({"type": "choice", "id": cid, "prompt": item["context"],
                                     "mainline": item["mainline"], "options": opts})
                    continue
                if item["type"] == "result":
                    # A result at chapter level is a terminal outcome for the chapter.
                    segments.append({"type": "result", "text": item["text"]})
                    continue
                if item["type"] == "merge":
                    continue
            flush_segment()

            scene = {
                "id": scene_id,
                "act": act,
                "code": code,
                "title_zh": title,
                "title_en": title,
                "client": client,
                "portrait": portrait,
                "intent": intent,
                "next_code": NEXT_CODE[scene_id],
                "segments": segments,
                "blocks": blocks,
            }
            if scene_id in WHOLESALE:
                scene["wholesale"] = WHOLESALE[scene_id]
            if scene_id in BUDGET:
                scene["budget"] = BUDGET[scene_id]
                scene["tags"] = ["HOUSEHOLD_GOOD"]

            trade_scene = TRADE_SCENE.get(act)
            if trade_scene == scene_id:
                if not declarations:
                    raise ValueError(f"trade omitted at {scene_id}")
                declaration = declarations[0]
                trade = dict(declaration)
                trade["outcomes"] = OrderedDict()
                for outcome, lines in outcomes.get(declaration["id"], OrderedDict()).items():
                    block_id = f"{scene_id}_trade_{len(trade['outcomes']) + 1}"
                    blocks.update([block(block_id, lines)])
                    trade["outcomes"][outcome] = block_id
                if not trade["outcomes"]:
                    raise ValueError(f"no trade outcomes at {scene_id}")
                scene["trades"] = [trade]

            scenes.append(scene)
            meta_index += 1

    if meta_index != len(SCENE_META):
        raise ValueError(f"scene count mismatch: {meta_index} != {len(SCENE_META)}")

    merge_existing_english(scenes)
    data = {"version": 1, "source": "Novel_Output/volume_maid_v2_Chapter_0..4_Final.txt",
            "scenes": scenes}
    OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    total_blocks = sum(len(s["blocks"]) for s in scenes)
    total_lines = sum(len(b["lines"]) for s in scenes for b in s["blocks"].values())
    total_choices = sum(1 for s in scenes for seg in s["segments"] if seg["type"] == "choice")
    print(f"Wrote {OUT}")
    print(f"scenes={len(scenes)} blocks={total_blocks} lines={total_lines} choices={total_choices}")


if __name__ == "__main__":
    main()
