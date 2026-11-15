#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Parse the harem-line v2 writer output into integration scenes.

The four ``volume_harem_D_v2_Chapter_*_Final.txt`` files contain 13 chapters
(one client visit each).  This script turns them into ``dv01``-``dv13`` scene
objects with stable block and choice IDs, plus a translation worklist.  The
merge step in ``_integrate_harem_v2.py`` fills the English strings and appends
the scenes to ``Content/dual_romance_v04.json``.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOW = ROOT / "tools" / "NovelWorkFlow_Eleanor"
OUT_SCENES = FLOW / "tmp_harem_v2_scenes.json"
OUT_ITEMS = FLOW / "tmp_harem_v2_items.json"

ACTS = [
    {
        "final": "volume_harem_D_v2_Chapter_1_Final.txt",
        "branches": "volume_harem_D_v2_Chapter_1_Branches.txt",
        "choices": "volume_harem_D_v2_Chapter_1_Choices.json",
    },
    {
        "final": "volume_harem_D_v2_Chapter_2_Final.txt",
        "branches": "volume_harem_D_v2_Chapter_2_Branches.txt",
        "choices": "volume_harem_D_v2_Chapter_2_Choices.json",
    },
    {
        "final": "volume_harem_D_v2_Chapter_3_Final.txt",
        "branches": "volume_harem_D_v2_Chapter_3_Branches.txt",
        "choices": "volume_harem_D_v2_Chapter_3_Choices.json",
    },
    {
        "final": "volume_harem_D_v2_Chapter_4_Final.txt",
        "branches": "volume_harem_D_v2_Chapter_4_Branches.txt",
        "choices": "volume_harem_D_v2_Chapter_4_Choices.json",
    },
]

# id, act index, chapter index inside the act, main speaker, backup speaker,
# whether the first block stages the backup, native trade id.
SCENES = [
    ("dv01", 0, 0, "Lydia", "Eleanor", True, None),
    ("dv02", 0, 1, "Lydia", "Eleanor", True, None),
    ("dv03", 0, 2, "Lydia", "Eleanor", True, "T1"),
    ("dv04", 1, 0, "Thug", None, False, None),
    ("dv05", 1, 1, "Lydia", "Eleanor", True, None),
    ("dv06", 1, 2, "Lydia", "Eleanor", True, None),
    ("dv07", 1, 3, "Eleanor", "Lydia", True, None),
    ("dv08", 2, 0, "Eleanor", None, False, None),
    ("dv09", 2, 1, "Lydia", None, False, None),
    ("dv10", 2, 2, "Lydia", None, False, None),
    ("dv11", 2, 3, "Lydia", None, False, None),
    ("dv12", 3, 0, "Lydia", None, False, None),
    ("dv13", 3, 1, "Eleanor", "Lydia", True, None),
]

ROLE_MAP = {
    "莉迪亚": "Lydia",
    "埃莉诺": "Eleanor",
    "店主": "Player",
    "黑市打手": "Thug",
    "旁白": "Narrator",
}

DESCRIPTIONS = {
    "dv01": [
        "顺着莉迪亚的好奇，把怀表主人的事讲完。",
        "不把别人的旧事当故事讲。",
        "让莉迪亚自己去问那个人。",
    ],
    "dv02": [
        "告诉她血袋后来怎么了。",
        "把结局留在柜台这边。",
    ],
    "dv03": [
        "让她亲手掂一掂那块矿石。",
        "不碰别人留下的押品。",
    ],
    "dv04": [
        "不承认两个姑娘来过。",
        "把矿石要回来，把人请出去。",
        "先问出背后是谁。",
    ],
    "dv06": [
        "自己站在门口。",
        "先让她们离开。",
    ],
    "dv08": [
        "不打断她。",
        "只叫她的名字。",
        "停下手上的事听她说完。",
    ],
    "dv09": [
        "先把界线说清楚。",
        "让她把话说明白。",
        "让她坐下来再谈。",
    ],
    "dv10": [
        "直接回答她。",
        "点一下头。",
        "先让她进门。",
    ],
    "dv11": [
        "把埃莉诺先告白的事说出来。",
        "先让她坐稳。",
        "先问她怎么想。",
    ],
    "dv12": [
        "把喜欢说出口，不替埃莉诺决定。",
        "让她把退和不退说清楚。",
        "先递给她一件旧衣。",
    ],
    "dv13": [
        "把位置留给她们。",
        "先说自己想留下两个人。",
        "让她们坐下来谈。",
    ],
}

TRADE_OUTCOME_EN = {
    "公道": "Fair price",
    "让利": "Discount",
    "高价": "Overcharged",
    "未成交": "No sale",
}


def read(name: str) -> str:
    return (FLOW / "Novel_Output" / name).read_text(encoding="utf-8")


def parse_segment(raw_lines, main: str, backup: str | None, where: str):
    """Parse dialogue/expression/narration lines inside one segment."""
    out = []
    pending = {}
    for raw in raw_lines:
        line = raw.strip()
        if not line:
            continue
        if line.startswith(("[时间|", "[章|", "[选择|", "[选项|", "[汇合|", "[结果|",
                            "[选项后续|", "[/选项后续]", "[/选择]", "[交易声明|",
                            "[交易后续|", "[/交易后续]")):
            continue
        m = re.match(r"^\[([^|\]]+)\|表情\|([^\]]+)\]$", line)
        if m:
            pending[m.group(1)] = m.group(2).strip()
            continue
        m = re.match(r"^\[([^|\]]+)\|对白\|(.*)\]$", line)
        if m:
            name, text = m.group(1), m.group(2).strip()
            role = ROLE_MAP.get(name)
            if role is None:
                raise ValueError(f"{where}: unknown speaker {name}")
            if role not in ("Player", "Narrator"):
                if role == main:
                    speaker = role
                elif backup is not None and role == backup:
                    speaker = "SEC"
                else:
                    raise ValueError(f"{where}: speaker {role} is neither main nor backup")
            else:
                speaker = role
            row = {"speaker": speaker, "text": text}
            if name in pending:
                if speaker == main:
                    row["expr"] = pending[name]
                pending.pop(name, None)
            out.append(row)
            continue
        m = re.match(r"^\[旁白\|(.*)\]$", line)
        if m:
            out.append({"speaker": "Narrator", "text": m.group(1).strip()})
            continue
        raise ValueError(f"{where}: unparsed line {line}")
    return out


def parse_final(text: str):
    chapters = []
    cur = None
    mode = "pre"
    for raw in text.splitlines():
        line = raw.strip()
        if not line:
            continue
        if line.startswith("[章|"):
            title = line[len("[章|"):-1].strip()
            cur = {"title": title, "pre": [], "post": [], "choice": None,
                   "options": [], "choice_num": None, "mainline": None}
            chapters.append(cur)
            mode = "pre"
            continue
        if cur is None:
            continue
        if line.startswith("[时间|"):
            continue
        if line.startswith("[选择|"):
            m = re.match(r"^\[选择\|C(\d+)\|([^|\]]*)\|主线=(\d+)\]$", line)
            if not m:
                raise ValueError("bad choice header: " + line)
            cur["choice_num"] = int(m.group(1))
            cur["choice"] = m.group(2).strip()
            cur["mainline"] = int(m.group(3))
            mode = "options"
            continue
        if line.startswith("[选项|"):
            cur["options"].append(line[len("[选项|"):-1].strip())
            continue
        if line == "[/选择]":
            mode = "post"
            continue
        target = "pre" if mode == "pre" else "post"
        cur[target].append(line)
    return chapters


def parse_branches(text: str):
    branches = {}
    for m in re.finditer(r"\[选项后续\|(C\d+)\|(\d+)\](.*?)\[/选项后续\]", text, re.DOTALL):
        choice_id, option = m.group(1), int(m.group(2))
        inner = m.group(3)
        merge = "[汇合|" in inner
        result = re.search(r"\[结果\|[^|\]]+\|([^\]]*)\]", inner)
        branches[(choice_id, option)] = {
            "raw": inner.splitlines(),
            "merge": merge,
            "result": result.group(1).strip() if result else None,
        }
    trades = {}
    for m in re.finditer(r"\[交易后续\|(T\d+)\|([^|\]]+)\](.*?)\[/交易后续\]", text, re.DOTALL):
        trades[m.group(2).strip()] = m.group(3).splitlines()
    return branches, trades


def scene_presentation(backup: str | None):
    if backup is None:
        return None
    sprite = "eleanor1" if backup == "Eleanor" else "er_portrait_lydia_female_beauty"
    name = "埃莉诺" if backup == "Eleanor" else "莉迪亚"
    return {"mode": "immediate", "backup1": {"sprite": sprite, "name": name}}


def build():
    items = []
    scenes = []
    for scene_id, act_i, chapter_i, main, backup, stage_backup, trade in SCENES:
        act = ACTS[act_i]
        chapters = parse_final(read(act["final"]))
        if chapter_i >= len(chapters):
            raise ValueError(f"{scene_id}: missing chapter {chapter_i}")
        chapter = chapters[chapter_i]
        branch_data, trade_data = parse_branches(read(act["branches"]))
        choices_json = json.loads(read(act["choices"]))
        declared = None
        if chapter["choice_num"] is not None:
            declared = next(c for c in choices_json["choices"]
                            if c["id"] == f"C{chapter['choice_num']}")
            if declared["options"] != chapter["options"]:
                raise ValueError(f"{scene_id}: choice options differ from choices JSON")
            if declared["mainline"] != chapter["mainline"]:
                raise ValueError(f"{scene_id}: mainline differs from choices JSON")

        where = scene_id
        pre = parse_segment(chapter["pre"], main, backup, where + ".pre")
        post = parse_segment(chapter["post"], main, backup, where + ".post")
        blocks = []
        order = []  # (option index, block id) for non-mainline branches

        def add_block(bid, label, lines, presentation=None):
            block = {"id": bid, "label_zh": label, "lines": []}
            if presentation:
                block["presentation"] = presentation
            for idx, row in enumerate(lines, 1):
                key = f"er_dual_{bid}_{idx:02d}"
                block["lines"].append({
                    "speaker": row["speaker"],
                    "text": row["text"],
                    **({"expr": row["expr"]} if row.get("expr") else {}),
                    "_key": key,
                })
                items.append({"key": key, "kind": "line",
                              "speaker": row["speaker"], "zh": row["text"]})
            blocks.append(block)

        title = chapter["title"]
        presentation = scene_presentation(backup) if stage_backup else None
        add_block(scene_id + "_b001", title, pre, presentation)

        if chapter["choice_num"] is not None:
            qid = f"C{chapter['choice_num']}"
            next_num = 2
            for option_index in range(1, len(chapter["options"]) + 1):
                if option_index == chapter["mainline"]:
                    continue
                branch = branch_data.get((qid, option_index))
                if branch is None:
                    raise ValueError(f"{scene_id}: missing branch {qid}.{option_index}")
                bid = f"{scene_id}_b{next_num:03d}"
                add_block(bid, title + "：" + chapter["options"][option_index - 1],
                          parse_segment(branch["raw"], main, backup,
                                        f"{where}.{qid}.{option_index}"))
                order.append((option_index, bid, branch["merge"], branch["result"]))
                next_num += 1
            cont_id = f"{scene_id}_b{next_num:03d}"
            add_block(cont_id, title + "：共通", post)
            next_num += 1
            if trade:
                for outcome in ("公道", "让利", "高价", "未成交"):
                    if outcome not in trade_data:
                        raise ValueError(f"{scene_id}: missing trade outcome {outcome}")
                    bid = f"{scene_id}_b{next_num:03d}"
                    add_block(bid, title + "：" + outcome,
                              parse_segment(trade_data[outcome], main, backup,
                                            f"{where}.{outcome}"))
                    next_num += 1
        else:
            if post:
                raise ValueError(f"{scene_id}: choice-free chapter has post lines")
            cont_id = None

        choice_entries = []
        if chapter["choice_num"] is not None:
            for option_index, label in enumerate(chapter["options"], 1):
                cid = f"{scene_id}_c{chapter['choice_num']}_{option_index}"
                desc = DESCRIPTIONS[scene_id][option_index - 1]
                choice_entries.append({
                    "id": cid,
                    "label_zh": label,
                    "description_zh": desc,
                    "block": cont_id if option_index == chapter["mainline"]
                             else next(b for (i, b, _, _) in order if i == option_index),
                    "merge": (cont_id if option_index == chapter["mainline"]
                              else next(m for (i, _, m, _) in order if i == option_index)),
                })
                items.append({"key": "choice." + cid + ".label", "kind": "choice_label",
                              "speaker": "", "zh": label})
                items.append({"key": "choice." + cid + ".desc", "kind": "choice_desc",
                              "speaker": "", "zh": desc})

        items.append({"key": "title." + scene_id, "kind": "title", "speaker": "", "zh": title})
        scenes.append({
            "id": scene_id,
            "title_zh": title,
            "main": main,
            "backup": backup,
            "presentation": presentation is not None,
            "trade": trade,
            "choice_number": chapter["choice_num"],
            "mainline": chapter["mainline"],
            "blocks": blocks,
            "choices": choice_entries,
            "branch_order": [{"option": i, "block": b, "merge": m, "result": r}
                             for (i, b, m, r) in order],
            "continuation": cont_id,
        })

    OUT_SCENES.write_text(json.dumps({"scenes": scenes}, ensure_ascii=False, indent=2) + "\n",
                          encoding="utf-8")
    OUT_ITEMS.write_text(json.dumps(items, ensure_ascii=False, indent=2) + "\n",
                         encoding="utf-8")
    lines = sum(len(b["lines"]) for s in scenes for b in s["blocks"])
    print(f"scenes={len(scenes)} blocks={sum(len(s['blocks']) for s in scenes)} lines={lines} items={len(items)}")
    print("wrote", OUT_SCENES)
    print("wrote", OUT_ITEMS)


if __name__ == "__main__":
    build()
